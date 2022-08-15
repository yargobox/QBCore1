namespace QBCore.DataSource.QueryBuilder.Mongo;

using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using QBCore.DataSource.Options;

/// <summary>
/// An optimistic approach to ID generation is based on the last known ID value by the process.
/// If an identifier with such a value plus a step already exists (insertion fails with the DuplicateKey status),
/// then the generator will request the database to obtain the last known identifier value from the collection.
/// </summary>
/// <typeparam name="TDocument">IMongoCollection document type</typeparam>
/// <remarks>
/// If TDocument serves more than one collection, you must use the
/// OptimisticSequentialIdGenerator<TDocument, TIdNamespace> class.
/// </remarks>
public class OptimisticSequentialIdGenerator<TDocument> : OptimisticSequentialIdGenerator<TDocument, TDocument>
{
	public OptimisticSequentialIdGenerator(int startAt = 1, int step = 1, int maxAttempts = 10)
		: base(startAt, step, maxAttempts) { }
}

/// <summary>
/// An optimistic approach to ID generation is based on the last known ID value by the process.
/// If an identifier with such a value plus a step already exists (insertion fails with the DuplicateKey status),
/// then the generator will request the database to obtain the last known identifier value from the collection.
/// </summary>
/// <typeparam name="TDocument">IMongoCollection document type</typeparam>
/// <typeparam name="TIdNamespace">Use this type to make different generic generator types with the same TDocument but for different collections.
/// Otherwise, the internal static variable containing the last known Id value will be the same for different collections of TDocument, resulting in a collision.
/// </typeparam>
public class OptimisticSequentialIdGenerator<TDocument, TIdNamespace> : IDSIdGenerator
{
	public int MaxAttempts { get; }
	public readonly int StartAt;
	public readonly int Step;
	
	private static readonly Func<object, object>? _getDocumentId = BsonClassMap.LookupClassMap(typeof(TDocument)).IdMemberMap?.Getter;

	private static int _lastKnownId = int.MinValue;

	public OptimisticSequentialIdGenerator(int startAt = 1, int step = 1, int maxAttempts = 10)
	{
		if (startAt == int.MinValue) throw new ArgumentOutOfRangeException(nameof(startAt), nameof(startAt) + " cannot be equal to the lowest 'int' value.");
		if (step == 0) throw new ArgumentException(nameof(step) + " cannot be zero.", nameof(step));
		if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

		StartAt = startAt;
		Step = step;
		MaxAttempts = maxAttempts;
	}

	public bool IsEmpty(object? id) => id == null ? false : IsEmpty((int)id);

	public object GenerateId(object container, object document, DataSourceIdGeneratorOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (container == null)
		{
			throw new ArgumentNullException(nameof(container));
		}
		if (container is not IMongoCollection<TDocument> collection)
		{
			throw new ArgumentException(nameof(container));
		}
		if (document == null)
		{
			throw new ArgumentNullException(nameof(document));
		}
		if (document is not TDocument typedDocument)
		{
			throw new ArgumentException(nameof(document));
		}

		if (_lastKnownId == int.MinValue || (_getDocumentId != null && (int?)_getDocumentId(document!) == _lastKnownId + Step))
		{
			if (options != null)
			{
				if (options.NativeOptions != null && options.NativeOptions is not AggregateOptions)
				{
					throw new ArgumentException(nameof(options.NativeOptions));
				}
				if (options.NativeClientSession != null && options.NativeClientSession is not IClientSessionHandle)
				{
					throw new ArgumentException(nameof(options.NativeClientSession));
				}
			}

			var aggregateOptions = (AggregateOptions?)options?.NativeOptions;
			var clientSessionHandle = (IClientSessionHandle?)options?.NativeClientSession;
			var query = Step > 0 ? SequentialIdGeneratorBase.MaxIDQuery : SequentialIdGeneratorBase.MinIDQuery;

			if (options?.QueryStringCallback != null)
			{
				options.QueryStringCallback(Step > 0 ? SequentialIdGeneratorBase.MaxIDQueryString : SequentialIdGeneratorBase.MinIDQueryString);
			}

			var lastId =
				(
					clientSessionHandle != null
						? collection.Aggregate<SequentialIdGeneratorBase.DocumentId>(clientSessionHandle, query, aggregateOptions, cancellationToken)
						: collection.Aggregate<SequentialIdGeneratorBase.DocumentId>(query, aggregateOptions, cancellationToken)
				)
				.FirstOrDefault(cancellationToken)?.Id;

			if (lastId == null)
			{
				if (Interlocked.CompareExchange(ref _lastKnownId, StartAt, int.MinValue) == int.MinValue)
				{
					return StartAt;
				}
			}
			else
			{
				if (Interlocked.CompareExchange(ref _lastKnownId, lastId.Value + Step, int.MinValue) == int.MinValue)
				{
					return lastId.Value + Step;
				}
			}
		}

		int lastKnownId, newId;
		do
		{
			lastKnownId = _lastKnownId;
			newId = lastKnownId + Step;
			if (newId == int.MinValue) throw new OverflowException();
		}
		while (Interlocked.CompareExchange(ref _lastKnownId, newId, lastKnownId) != lastKnownId);

		return newId;
	}

	public async Task<object> GenerateIdAsync(object container, object document, DataSourceIdGeneratorOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (container == null)
		{
			throw new ArgumentNullException(nameof(container));
		}
		if (container is not IMongoCollection<TDocument> collection)
		{
			throw new ArgumentException(nameof(container));
		}
		if (document == null)
		{
			throw new ArgumentNullException(nameof(document));
		}
		if (document is not TDocument typedDocument)
		{
			throw new ArgumentException(nameof(document));
		}

		if (_lastKnownId == int.MinValue || (_getDocumentId != null && (int?)_getDocumentId(document!) == _lastKnownId + Step))
		{
			if (options != null)
			{
				if (options.NativeOptions != null && options.NativeOptions is not AggregateOptions)
				{
					throw new ArgumentException(nameof(options.NativeOptions));
				}
				if (options.NativeClientSession != null && options.NativeClientSession is not IClientSessionHandle)
				{
					throw new ArgumentException(nameof(options.NativeClientSession));
				}
			}

			var aggregateOptions = (AggregateOptions?)options?.NativeOptions;
			var clientSessionHandle = (IClientSessionHandle?)options?.NativeClientSession;
			var query = Step > 0 ? SequentialIdGeneratorBase.MaxIDQuery : SequentialIdGeneratorBase.MinIDQuery;

			if (options?.QueryStringAsyncCallback != null)
			{
				await options.QueryStringAsyncCallback(Step > 0 ? SequentialIdGeneratorBase.MaxIDQueryString : SequentialIdGeneratorBase.MinIDQueryString).ConfigureAwait(false);
			}
			else if (options?.QueryStringCallback != null)
			{
				options.QueryStringCallback(Step > 0 ? SequentialIdGeneratorBase.MaxIDQueryString : SequentialIdGeneratorBase.MinIDQueryString);
			}

			var lastId = (await
				(
					clientSessionHandle != null
						? await collection.AggregateAsync<SequentialIdGeneratorBase.DocumentId>(query, aggregateOptions, cancellationToken).ConfigureAwait(false)
						: await collection.AggregateAsync<SequentialIdGeneratorBase.DocumentId>(clientSessionHandle, query, aggregateOptions, cancellationToken).ConfigureAwait(false)
				)
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false))?.Id;

			if (lastId == null)
			{
				if (Interlocked.CompareExchange(ref _lastKnownId, StartAt, int.MinValue) == int.MinValue)
				{
					return StartAt;
				}
			}
			else
			{
				if (Interlocked.CompareExchange(ref _lastKnownId, lastId.Value + Step, int.MinValue) == int.MinValue)
				{
					return lastId.Value + Step;
				}
			}
		}

		int lastKnownId, newId;
		do
		{
			lastKnownId = _lastKnownId;
			newId = lastKnownId + Step;
			if (newId == int.MinValue) throw new OverflowException();
		}
		while (Interlocked.CompareExchange(ref _lastKnownId, newId, lastKnownId) != lastKnownId);

		return newId;
	}
}