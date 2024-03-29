using Microsoft.EntityFrameworkCore;

using QBCore.Configuration;
using QBCore.DataSource.Options;
using QBCore.Extensions.Internals;

namespace QBCore.DataSource.QueryBuilder.EfCore;

internal sealed class UpdateQueryBuilder<TDoc, TUpdate> : QueryBuilder<TDoc, TUpdate>, IUpdateQueryBuilder<TDoc, TUpdate> where TDoc : class
{
	public override QueryBuilderTypes QueryBuilderType => QueryBuilderTypes.Update;

	public UpdateQueryBuilder(UpdateQBBuilder<TDoc, TUpdate> building, IDataContext dataContext) : base(building, dataContext)
	{
		building.Normalize();
	}

	public async Task<TDoc?> UpdateAsync(object id, TUpdate document, IReadOnlySet<string>? validFieldNames = null, DataSourceUpdateOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (id is null) throw EX.QueryBuilder.Make.IdentifierValueNotSpecified(nameof(id));
		if (document is null) throw EX.QueryBuilder.Make.DocumentNotSpecified(nameof(document));
		if (options?.FetchResultDocument == true) throw new NotSupportedException($"EF update query builder does not support fetching the result document.");

		var top = Builder.Containers.First();
		if (top.ContainerOperation != ContainerOperations.Update)
		{
			throw EX.QueryBuilder.Make.QueryBuilderOperationNotSupported(Builder.DataLayer.Name, QueryBuilderType.ToString(), top?.ContainerOperation.ToString());
		}

		var dbContext = _dataContext.AsDbContext();

		var deId = (EfCoreDEInfo?)Builder.DocInfo.IdField
			?? throw EX.QueryBuilder.Make.DocumentDoesNotHaveIdDataEntry(Builder.DocInfo.DocumentType.ToPretty());
		if (deId.Setter == null)
			throw EX.QueryBuilder.Make.DataEntryDoesNotHaveSetter(Builder.DocInfo.DocumentType.ToPretty(), deId.Name);
		var deUpdated = (EfCoreDEInfo?)Builder.DocInfo.DateUpdatedField;
		var deModified = (EfCoreDEInfo?)Builder.DocInfo.DateModifiedField;

	/* 		if (Builder.Conditions.Count != 1)
			{
				throw new NotSupportedException($"EF update query builder must have one single equality condition for the id data entry.");
			}
			var cond = Builder.Conditions.Single();
			if (cond.Alias != top.Alias || cond.Operation != FO.Equal || !cond.IsOnParam || cond.Field.Name != deId.Name)
			{
				throw new NotSupportedException($"EF update query builder does not support custom conditions.");
			} */

		var update = Activator.CreateInstance<TDoc>();
		deId.Setter(update, id);
		dbContext.Attach<TDoc>(update);

		try
		{
			object? value;
			bool isSetValue, isUpdatedSet = false, isModifiedSet = false, hasFieldToUpdate = false;
			var dataEntries = Builder.DocInfo.DataEntries.Values.Cast<EfCoreDEInfo>();
			EfCoreDEInfo? deProjInfo;

			foreach (var deDocInfo in dataEntries.Where(x => x.Property != null && x.Name != deId.Name && (validFieldNames == null || validFieldNames.Contains(x.Name))))
			{
				deProjInfo = (EfCoreDEInfo?)(Builder.DtoInfo?.DataEntries ?? Builder.DocInfo.DataEntries).GetValueOrDefault(deDocInfo.Name);
				if (deProjInfo == null)
				{
					continue;
				}

				isSetValue = true;
				value = deProjInfo.Getter(document);

				if (deDocInfo == deUpdated)
				{
					isUpdatedSet = isSetValue = value is not null && value != deDocInfo.UnderlyingType.GetDefaultValue();
				}
				else if (deDocInfo == deModified)
				{
					isModifiedSet = isSetValue = value is not null && value != deDocInfo.UnderlyingType.GetDefaultValue();
				}

				if (isSetValue)
				{
					if (deDocInfo.Setter == null)
						throw EX.QueryBuilder.Make.DataEntryDoesNotHaveSetter(Builder.DocInfo.DocumentType.ToPretty(), deDocInfo.Name);

					deDocInfo.Setter(update, value);
					hasFieldToUpdate = true;
				}
			}

			if (!hasFieldToUpdate)
			{
				return default(TDoc?);
			}

			if (deUpdated != null && !isUpdatedSet)
			{
				if (deUpdated.Setter == null)
					throw EX.QueryBuilder.Make.DataEntryDoesNotHaveSetter(Builder.DocInfo.DocumentType.ToPretty(), deUpdated.Name);

				deUpdated.Setter(update, Convert.ChangeType(DateTime.UtcNow, deUpdated.UnderlyingType));
			}
			if (deModified != null && !isModifiedSet)
			{
				if (deModified.Setter == null)
					throw EX.QueryBuilder.Make.DataEntryDoesNotHaveSetter(Builder.DocInfo.DocumentType.ToPretty(), deModified.Name);

				deModified.Setter(update, Convert.ChangeType(DateTime.UtcNow, deModified.UnderlyingType));
			}

			AttachQueryStringCallback(options, dbContext);

			try
			{
				await dbContext.SaveChangesAsync(cancellationToken);

				return default(TDoc?);
			}
			catch (DbUpdateException ex)
			{
				throw EX.QueryBuilder.Make.OperationFailedNoSuchRecord(QueryBuilderType.ToString(), id.ToString(), Builder.DocInfo.DocumentType.ToPretty(), ex);
			}
			finally
			{
				DetachQueryStringCallback(options, dbContext);
			}
		}
		finally
		{
			dbContext.Entry<TDoc>(update).State = EntityState.Detached;
		}
	}

	private static void AttachQueryStringCallback(DataSourceOperationOptions? options, DbContext dbContext)
	{
		if (options?.QueryStringCallback != null)
		{
			var logger = (dbContext as IEfCoreDbContextLogger)
				?? throw new NotSupportedException($"Database context '{dbContext.GetType().Name}' should have supported the {nameof(IEfCoreDbContextLogger)} interface for logging query strings.");

			logger.QueryStringCallback += options.QueryStringCallback;
		}
		else if (options?.QueryStringCallbackAsync != null)
		{
			throw new NotSupportedException($"{nameof(DataSourceOperationOptions)}.{nameof(DataSourceOperationOptions.QueryStringCallbackAsync)} is not supported.");
		}
	}

	private static void DetachQueryStringCallback(DataSourceOperationOptions? options, DbContext dbContext)
	{
		if (options?.QueryStringCallback != null)
		{
			var logger = (dbContext as IEfCoreDbContextLogger)
				?? throw new NotSupportedException($"Database context '{dbContext.GetType().Name}' should have supported the {nameof(IEfCoreDbContextLogger)} interface for logging query strings.");

			logger.QueryStringCallback -= options.QueryStringCallback;
		}
	}
}