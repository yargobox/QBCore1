using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using Dapper;
using Npgsql;
using QBCore.Configuration;
using QBCore.DataSource.Options;
using QBCore.DataSource.QueryBuilder;
using QBCore.Extensions.Internals;

namespace QBCore.DataSource.QueryBuilder.PgSql;

internal sealed partial class SelectQueryBuilder<TDoc, TSelect> : QueryBuilder<TDoc, TSelect>, ISelectQueryBuilder<TDoc, TSelect> where TDoc : class
{
	public override QueryBuilderTypes QueryBuilderType => QueryBuilderTypes.Select;

	public SelectQueryBuilder(SelectQBBuilder<TDoc, TSelect> builder, IDataContext dataContext)
		: base(builder, dataContext as IPgSqlDataContext ?? throw new ArgumentException(nameof(dataContext)))
	{
		builder.Normalize();
	}

	public IQueryable<TDoc> AsQueryable(DataSourceQueryableOptions? options = null)
	{
		throw new NotImplementedException();
	}

	public Task<long> CountAsync(DataSourceCountOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		throw new NotImplementedException();

/* 		if (options != null)
		{
			if (options.Skip < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(options.Skip));
			}
			if (options.NativeSelectQuery != null && options.NativeSelectQuery is not string)
			{
				throw new ArgumentException(nameof(options.NativeSelectQuery));
			}
		}

		var dbContext = _dataContext.AsDbContext();
		var logger = dbContext as IEfDbContextLogger;

		if (options != null)
		{
			if (options.QueryStringCallback != null)
			{
				if (logger == null)
				{
					throw new NotSupportedException($"Database context '{dbContext.GetType().Name}' should have supported the {nameof(IEfDbContextLogger)} interface for logging query strings.");
				}

				logger.QueryStringCallback += options.QueryStringCallback;
			}
			else if (options.QueryStringCallbackAsync != null)
			{
				throw new NotSupportedException($"{nameof(DataSourceCountOptions)}.{nameof(DataSourceCountOptions.QueryStringCallbackAsync)} is not supported.");
			}
		}

		try
		{
			return await dbContext.Set<TDoc>().LongCountAsync();
		}
		finally
		{
			if (options != null && options.QueryStringCallback != null && logger != null)
			{
				logger.QueryStringCallback -= options.QueryStringCallback;
			}
		} */
	}

	public async Task<IDSAsyncCursor<TSelect>> SelectAsync(long skip = 0L, int take = -1, DataSourceSelectOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

		var top = Builder.Containers.FirstOrDefault();
		if (top?.ContainerOperation != ContainerOperations.Select && top?.ContainerOperation != ContainerOperations.Exec)
		{
			throw EX.QueryBuilder.Make.QueryBuilderOperationNotSupported(Builder.DataLayer.Name, QueryBuilderType.ToString(), top?.ContainerOperation.ToString());
		}

		NpgsqlConnection? connection = null;
		NpgsqlTransaction? transaction = null;
		if (options != null)
		{
			if (options.Connection != null)
			{
				connection = (options.Connection as NpgsqlConnection) ?? throw new ArgumentException(nameof(options.Connection));
			}
			if (options.Transaction != null)
			{
				transaction = (options.Transaction as NpgsqlTransaction) ?? throw new ArgumentException(nameof(options.Transaction));
				
				if (transaction.Connection != connection)
				{
					throw  EX.QueryBuilder.Make.SpecifiedTransactionOpenedForDifferentConnection();
				}
			}
		}

		if (top.ContainerOperation == ContainerOperations.Select)
		{
			string queryString;
			var sb = new StringBuilder();
			var command = new NpgsqlCommand();

			BuildSelectQuery(sb, skip, take >= 0 && options?.ObtainLastPageMarker == true ? take + 1 : take, command.Parameters);

			queryString = sb.ToString();

			if (options != null)
			{
				if (options.QueryStringCallbackAsync != null)
				{
					await options.QueryStringCallbackAsync(queryString).ConfigureAwait(false);
				}
				else if (options.QueryStringCallback != null)
				{
					options.QueryStringCallback(queryString);
				}
			}

			IAsyncEnumerable<TSelect>? cursor = null;
			try
			{
				command.CommandText = queryString;
				command.CommandType = CommandType.Text;

				connection ??= await DataContext.AsNpgsqlDataSource().OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
				command.Connection = connection;
				command.Transaction = transaction;

				cursor = GetAsyncEnumerable<TSelect>(command, options?.Connection == null, cancellationToken);

				if (options?.ObtainLastPageMarker == true)
				{
					return new DSAsyncCursorWithLastPageMarker<TSelect>(cursor, take, cancellationToken);
				}
				else
				{
					return new DSAsyncCursor<TSelect>(cursor, cancellationToken);
				}
			}
			catch
			{
				cursor = null;
				throw;
			}
			finally
			{
				if (cursor == null)
				{
					await command.DisposeAsync().ConfigureAwait(false);

					if (connection != null && options?.Connection == null)
					{
						await connection.DisposeAsync().ConfigureAwait(false);
					}
				}
			}
		}
		else/*  if (top.ContainerOperation == ContainerOperations.Exec) */
		{
			throw new NotImplementedException();
		}
	}

	private void BuildSelectQuery(StringBuilder sb, long skip, int take, NpgsqlParameterCollection commandParams)
	{
		var top = Builder.Containers.First();
		var topDbo = ExtensionsForSql.ParseDbObjectName(top.DBSideName);
		var topAlias = Builder.Containers.Count > 1 || topDbo.Object != top.Alias ? top.Alias : string.Empty;
		bool next;
		var dataEntries = Builder.DtoInfo?.DataEntries ?? Builder.DocInfo.DataEntries;

		Func<string?, DEPath, string> getDBSideName = topAlias.Length > 0
			? GetQuotedDBSideName
			: (alias, fieldPath) => GetQuotedDBSideName(alias == top.Alias ? null : alias, fieldPath);

		foreach (var container in Builder.Containers)
		{
			if (container.ContainerOperation == ContainerOperations.Select)
			{
				sb.Append("SELECT").AppendLine();

				next = false;
				foreach (var de in dataEntries.Values.Cast<SqlDEInfo>())
				{
					if (next) sb.AppendLine(","); next = true;

					//!!!
					var fld = Builder.Fields.FirstOrDefault(x => x.Field.Path == de.Name);
					if (fld == null)
					{
						sb.Append('\t');
						
						if (topAlias.Length > 0)
						{
							sb.Append(topAlias).Append('.');
						}
						sb.Append('"').Append(de.DBSideName).Append('"');
					}
					else if (!fld.IncludeOrExclude && fld.OptionalExclusion)
					{
						sb.Append("NULL");//!!!
					}
					else
					{
						throw new NotSupportedException("Field inclusion is not supported.");//!!!
					}
					
					if (de.Name != de.DBSideName)
					{
						sb.Append(" AS \"").Append(de.Name).Append('"');
					}
				}

				sb.AppendLine().Append("FROM ").AppendContainer(topDbo);

				if (topAlias.Length > 0)
				{
					sb.Append(" AS ").Append(container.Alias);
				}

				sb.AppendLine();
			}
			else if (container.ContainerOperation == ContainerOperations.Join || container.ContainerOperation == ContainerOperations.LeftJoin)
			{
				sb
					.AppendLine()
					.Append(container.ContainerOperation == ContainerOperations.Join ? "JOIN " : "LEFT JOIN ")
						.AppendContainer(container).Append(" AS ").Append(container.Alias).Append(" ON ");

				BuildConditionTree(sb, Builder.Connects, getDBSideName, Builder.Parameters, commandParams);

				sb.AppendLine();
			}
			else if (container.ContainerOperation == ContainerOperations.CrossJoin)
			{
				sb
					.AppendLine()
					.Append("CROSS JOIN ").AppendContainer(container).Append(" AS ").Append(container.Alias).AppendLine();
			}
			else
			{
				throw EX.QueryBuilder.Make.QueryBuilderOperationNotSupported(Builder.DataLayer.Name, QueryBuilderType.ToString(), container.ContainerOperation.ToString());
			}
		}

		if (Builder.Conditions.Count > 0)
		{
			sb.Append("WHERE ");

			BuildConditionTree(sb, Builder.Conditions, getDBSideName, Builder.Parameters, commandParams);

			sb.AppendLine();
		}

		next = false;
		foreach (var sort in Builder.SortOrders)
		{
			if (next)
			{
				sb.Append(", ");
			}
			else
			{
				next = true;
				sb.Append("ORDER BY ");
			}

			if (sort.Alias.Length == 0)
			{
				//!!!
				var de = (SqlDEInfo?)dataEntries.GetValueOrDefault(sort.Field.Path)
					?? throw new InvalidOperationException("");
				var fld = Builder.Fields.FirstOrDefault(x => x.Field.Path == de.Name);
				if (fld == null)
				{
					if (topAlias.Length > 0)
					{
						sb.Append(topAlias).Append('.');
					}
					sb.Append('"').Append(de.DBSideName).Append('"');
				}
				else if (!fld.IncludeOrExclude && fld.OptionalExclusion)
				{
					sb.Append("NULL");//!!!
				}
				else
				{
					throw new NotSupportedException("Field inclusion is not supported.");//!!!
				}
			}
			else
			{
				sb.Append(getDBSideName(sort.Alias, sort.Field));
			}

			if (sort.SortOrder == SO.Descending)
			{
				sb.Append(" DESC");
			}
			else if (sort.SortOrder != SO.Ascending)
			{
				throw new NotSupportedException($"{Builder.DataLayer.Name} does not support the sort order operation '{sort.SortOrder.ToString()}'.");
			}
		}
		if (next) sb.AppendLine();

		if (take >= 0)
		{
			sb.Append("LIMIT ").Append(take).AppendLine();
		}
		if (skip > 0)
		{
			sb.Append("OFFSET ").Append(skip).AppendLine();
		}
	}
}