using Microsoft.EntityFrameworkCore;
using QBCore.Configuration;
using QBCore.DataSource.Options;
using QBCore.Extensions.Internals;

namespace QBCore.DataSource.QueryBuilder.EfCore;

internal sealed class RestoreQueryBuilder<TDoc, TRestore> : QueryBuilder<TDoc, TRestore>, IRestoreQueryBuilder<TDoc, TRestore> where TDoc : class
{
	public override QueryBuilderTypes QueryBuilderType => QueryBuilderTypes.Restore;

	public RestoreQueryBuilder(RestoreQBBuilder<TDoc, TRestore> building, IDataContext dataContext) : base(building, dataContext)
	{
		building.Normalize();
	}

	public async Task RestoreAsync(object id, TRestore? document = default(TRestore?), DataSourceRestoreOptions? options = null, CancellationToken cancellationToken = default)
	{
		if (id is null) throw EX.QueryBuilder.Make.IdentifierValueNotSpecified(nameof(id));
		if (document is null && typeof(TRestore) != typeof(EmptyDto)) throw EX.QueryBuilder.Make.DocumentNotSpecified(nameof(document));

		var top = Builder.Containers.First();
		if (top.ContainerOperation != ContainerOperations.Update)
		{
			throw EX.QueryBuilder.Make.QueryBuilderOperationNotSupported(Builder.DataLayer.Name, QueryBuilderType.ToString(), top?.ContainerOperation.ToString());
		}

		var dbContext = _dataContext.AsDbContext();
		var logger = dbContext as IEfCoreDbContextLogger;

		var deId = (EfCoreDEInfo?)Builder.DocInfo.IdField
			?? throw EX.QueryBuilder.Make.DocumentDoesNotHaveIdDataEntry(Builder.DocInfo.DocumentType.ToPretty());
		if (deId.Setter == null)
			throw EX.QueryBuilder.Make.DataEntryDoesNotHaveSetter(Builder.DocInfo.DocumentType.ToPretty(), deId.Name);
		var deDeleted = (EfCoreDEInfo?)Builder.DocInfo.DateDeletedField
			?? throw EX.QueryBuilder.Make.DocumentDoesNotHaveDeletedDataEntry(Builder.DocInfo.DocumentType.ToPretty());
		if (deDeleted.Flags.HasFlag(DataEntryFlags.ReadOnly))
			throw new InvalidOperationException($"Document '{Builder.DocInfo.DocumentType.ToPretty()}' has a readonly date deletion field!");

		if (Builder.Conditions.Count != 1)
		{
			throw new NotSupportedException($"EF update query builder must have one single equality condition for the id data entry.");
		}
		var cond = Builder.Conditions.Single();
		if (cond.Alias != top.Alias || cond.Operation != FO.Equal || !cond.IsOnParam || cond.Field.Name != deId.Name)
		{
			throw new NotSupportedException($"EF update query builder does not support custom conditions.");
		}

		object? dateDel = null;

		if (document is not null)
		{
			var getDateDelFromDto = Builder.DtoInfo?.DateDeletedField?.Getter ?? Builder.DtoInfo?.DataEntries.GetValueOrDefault(deDeleted.Name)?.Getter;
			if (getDateDelFromDto != null)
			{
				dateDel = getDateDelFromDto(document);
			}
		}

		if (!deDeleted.IsNullable && dateDel is null)
		{
			dateDel = Convert.ChangeType(DateTimeOffset.UtcNow, deDeleted.UnderlyingType);
		}

		if (options != null)
		{
			if (options.QueryStringCallback != null)
			{
				if (logger == null)
				{
					throw new NotSupportedException($"Database context '{dbContext.GetType().Name}' should have supported the {nameof(IEfCoreDbContextLogger)} interface for logging query strings.");
				}

				logger.QueryStringCallback += options.QueryStringCallback;
			}
			else if (options.QueryStringCallbackAsync != null)
			{
				throw new NotSupportedException($"{nameof(DataSourceRestoreOptions)}.{nameof(DataSourceRestoreOptions.QueryStringCallbackAsync)} is not supported.");
			}
		}

		try
		{
			int? restoredCount;
			if (dateDel is null)
			{
				restoredCount = await dbContext.Database.SqlQuery<int?>($"WITH restored AS (UPDATE \"{top.DBSideName}\" SET \"{deDeleted.DBSideName}\" = NULL WHERE \"{deId.DBSideName}\" = {id} RETURNING *) SELECT count(*) FROM restored;")
					.SingleOrDefaultAsync();
			}
			else
			{
				restoredCount = await dbContext.Database.SqlQuery<int?>($"WITH restored AS (UPDATE \"{top.DBSideName}\" SET \"{deDeleted.DBSideName}\" = {dateDel} WHERE \"{deId.DBSideName}\" = {id} RETURNING *) SELECT count(*) FROM restored;")
					.SingleOrDefaultAsync();
			}

			if ((restoredCount ?? 0) <= 0)
			{
				throw EX.QueryBuilder.Make.OperationFailedNoSuchRecord(QueryBuilderType.ToString(), id.ToString(), Builder.DocInfo.DocumentType.ToPretty());
			}
		}
		finally
		{
			if (options != null && options.QueryStringCallback != null && logger != null)
			{
				logger.QueryStringCallback -= options.QueryStringCallback;
			}
		}
	}
}