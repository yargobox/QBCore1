using MongoDB.Bson;
using MongoDB.Driver;
using QBCore.Configuration;
using QBCore.DataSource.Options;

namespace QBCore.DataSource.QueryBuilder.Mongo;

internal sealed class InsertQueryBuilder<TDocument, TCreate> : QueryBuilder<TDocument, TCreate>, IInsertQueryBuilder<TDocument, TCreate>
{
	public override QueryBuilderTypes QueryBuilderType => QueryBuilderTypes.Insert;

	public IQBInsertBuilder<TDocument, TCreate> InsertBuilder => (IQBInsertBuilder<TDocument, TCreate>)Builder;

	public InsertQueryBuilder(QBInsertBuilder<TDocument, TCreate> building, IDataContext dataContext)
		: base(building, dataContext)
	{
	}

	public async Task<TDocument> InsertAsync(
		TDocument document,
		DataSourceInsertOptions? options = null,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		var top = Builder.Containers.First();
		if (top.ContainerOperation != ContainerOperations.Insert)
		{
			throw new NotSupportedException($"Mongo insert query builder does not support an operation like '{top.ContainerOperation.ToString()}'.");
		}

		if (options != null)
		{
			if (options.NativeOptions != null && options.NativeOptions is not InsertOneOptions)
			{
				throw new ArgumentException(nameof(options.NativeOptions));
			}
			if (options.NativeClientSession != null && options.NativeClientSession is not IClientSessionHandle)
			{
				throw new ArgumentException(nameof(options.NativeClientSession));
			}
		}

		var collection = _mongoDbContext.DB.GetCollection<TDocument>(top.DBSideName);

		var insertOneOptions = (InsertOneOptions?)options?.NativeOptions;
		var clientSessionHandle = (IClientSessionHandle?)options?.NativeClientSession;

		var deId = (MongoDataEntry?)InsertBuilder.DocumentInfo.IdField;
		var deCreated = (MongoDataEntry?)InsertBuilder.DocumentInfo.DateCreatedField;
		var deModified = (MongoDataEntry?)InsertBuilder.DocumentInfo.DateModifiedField;

		var customIdGenerator = InsertBuilder.CustomIdGenerator != null ? InsertBuilder.CustomIdGenerator() : null;
		var generateId = customIdGenerator != null && deId?.Setter != null && customIdGenerator.IsEmpty(deId.Getter(document!));
		object id;
		DataSourceIdGeneratorOptions? generatorOptions = null;

		if (generateId && options != null)
		{
			generatorOptions = options.GeneratorOptions ?? new DataSourceIdGeneratorOptions();
			generatorOptions.NativeClientSession ??= options.NativeClientSession;
			//generatorOptions.QueryStringCallback ??= options.QueryStringCallback;
			//generatorOptions.QueryStringAsyncCallback ??= options.QueryStringAsyncCallback;
		}

		for (int i = 0; ; )
		{
			if (generateId)
			{
				id = await customIdGenerator!.GenerateIdAsync(collection, document!, generatorOptions, cancellationToken);
				deId!.Setter!(document!, id);
			}

			if (deCreated?.Flags.HasFlag(DataEntryFlags.ReadOnly) == false && deCreated.Setter != null)
			{
				if (deCreated.DataEntryType != typeof(BsonTimestamp))
				{
					var dateValue = deCreated.Getter(document!);
					var zero = deCreated.DataEntryType.GetDefaultValue();
					if (dateValue == zero)
					{
						dateValue = Convert.ChangeType(DateTimeOffset.Now, deCreated.DataEntryType);
						deCreated.Setter(document!, dateValue);
					}
				}
			}

			if (deModified?.Flags.HasFlag(DataEntryFlags.ReadOnly) == false && deModified.Setter != null)
			{
				if (deModified.DataEntryType != typeof(BsonTimestamp))
				{
					var dateValue = deModified.Getter(document!);
					var zero = deModified.DataEntryType.GetDefaultValue();
					if (dateValue == zero)
					{
						dateValue = Convert.ChangeType(DateTimeOffset.Now, deModified.DataEntryType);
						deModified.Setter(document!, dateValue);
					}
				}
			}

			try
			{
				if (clientSessionHandle == null)
				{
					await collection.InsertOneAsync(document, insertOneOptions, cancellationToken);
				}
				else
				{
					await collection.InsertOneAsync(clientSessionHandle, document, insertOneOptions, cancellationToken);
				}

				return document;
			}
			catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
			{
				if (generateId && ++i < customIdGenerator!.MaxAttempts)
				{
					continue;
				}

				throw;
			}
		}
	}
}