using QBCore.Configuration;
using QBCore.DataSource.Options;

namespace QBCore.DataSource.QueryBuilder;

public interface IQueryBuilder
{
	QueryBuilderTypes QueryBuilderType { get; }
	Type DocumentType { get; }
	DSDocumentInfo DocumentInfo { get; }
	Type ProjectionType { get; }
	DSDocumentInfo? ProjectionInfo { get; }
	Type DatabaseContextInterfaceType { get; }
	IDataContext DataContext { get; }
}

public interface IQueryBuilder<TDocument, TProjection> : IQueryBuilder
{
	QBBuilder<TDocument, TProjection> Builder { get; }
}

public interface IInsertQueryBuilder<TDocument, TCreate> : IQueryBuilder<TDocument, TCreate>
{
	Task<TDocument> InsertAsync(TDocument document, DataSourceInsertOptions? options = null, CancellationToken cancellationToken = default(CancellationToken));
}

public interface ISelectQueryBuilder<TDocument, TSelect> : IQueryBuilder<TDocument, TSelect>
{
	IQueryable<TDocument> AsQueryable(DataSourceQueryableOptions? options = null);
	Task<long> CountAsync(DataSourceCountOptions? options = null, CancellationToken cancellationToken = default(CancellationToken));
	Task<IDSAsyncCursor<TSelect>> SelectAsync(long skip = 0L, int take = -1, DataSourceSelectOptions? options = null, CancellationToken cancellationToken = default(CancellationToken));
}

public interface IUpdateQueryBuilder<TDocument, TUpdate> : IQueryBuilder<TDocument, TUpdate>
{
	Task<TDocument> UpdateAsync(object id, TDocument document, IReadOnlySet<string>? modifiedFieldNames = null, DataSourceUpdateOptions? options = null, CancellationToken cancellationToken = default(CancellationToken));
}

public interface IDeleteQueryBuilder<TDocument, TDelete> : IQueryBuilder<TDocument, TDelete>
{
	Task DeleteAsync(object id, TDelete? document, DataSourceDeleteOptions? options = null, CancellationToken cancellationToken = default(CancellationToken));
}

public interface IRestoreQueryBuilder<TDocument, TRestore> : IQueryBuilder<TDocument, TRestore>
{
	Task RestoreAsync(object id, TRestore? document, DataSourceRestoreOptions? options = null, CancellationToken cancellationToken = default(CancellationToken));
}