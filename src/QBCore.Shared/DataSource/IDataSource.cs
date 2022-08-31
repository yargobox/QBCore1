using QBCore.DataSource.Options;

namespace QBCore.DataSource;

public interface IDataSource
{
	IDSInfo DSInfo { get; }
}

public interface IDataSource<TKey, TDocument, TCreate, TSelect, TUpdate, TDelete, TRestore> : IDataSource
{
	Task<TKey> InsertAsync(
		TCreate document,
		IReadOnlyDictionary<string, object?>? arguments = null,
		DataSourceInsertOptions? options = null,
		CancellationToken cancellationToken = default(CancellationToken)
	);
	Task<IEnumerable<KeyValuePair<string, object?>>> AggregateAsync(
		IReadOnlyCollection<IDSAggregation> aggregations,
		SoftDel mode = SoftDel.Actual,
		IReadOnlyCollection<DSCondition<TSelect>>? conditions = null,
		IReadOnlyDictionary<string, object?>? arguments = null,
		DataSourceSelectOptions? options = null,
		CancellationToken cancellationToken = default(CancellationToken)
	);
	Task<long> CountAsync(
		SoftDel mode = SoftDel.Actual,
		IReadOnlyList<DSCondition<TSelect>>? conditions = null,
		IReadOnlyDictionary<string, object?>? arguments = null,
		DataSourceCountOptions? options = null,
		CancellationToken cancellationToken = default(CancellationToken)
	);
	Task<TSelect?> SelectAsync(
		TKey id,
		IReadOnlyDictionary<string, object?>? arguments = null,
		DataSourceSelectOptions? options = null,
		CancellationToken cancellationToken = default(CancellationToken)
	);
	Task<IDSAsyncCursor<TSelect>> SelectAsync(
		SoftDel mode = SoftDel.Actual,
		IReadOnlyList<DSCondition<TSelect>>? conditions = null,
		IReadOnlyList<DSSortOrder<TSelect>>? sortOrders = null,
		IReadOnlyDictionary<string, object?>? arguments = null,
		long skip = 0,
		int take = -1,
		DataSourceSelectOptions? options = null,
		CancellationToken cancellationToken = default(CancellationToken)
	);
	Task UpdateAsync(
		TKey id,
		TUpdate document,
		IReadOnlySet<string>? modifiedFieldNames = null,
		IReadOnlyDictionary<string, object?>? arguments = null,
		DataSourceUpdateOptions? options = null,
		CancellationToken cancellationToken = default(CancellationToken)
	);
	Task DeleteAsync(
		TKey id,
		TDelete? document = default(TDelete?),
		IReadOnlyDictionary<string, object?>? arguments = null,
		DataSourceDeleteOptions? options = null,
		CancellationToken cancellationToken = default(CancellationToken)
	);
	Task RestoreAsync(
		TKey id,
		TRestore? document = default(TRestore?),
		IReadOnlyDictionary<string, object?>? arguments = null,
		DataSourceRestoreOptions? options = null,
		CancellationToken cancellationToken = default(CancellationToken)
	);
}