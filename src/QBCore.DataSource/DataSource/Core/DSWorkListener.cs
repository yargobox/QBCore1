using QBCore.DataSource.Options;
using QBCore.ObjectFactory;

namespace QBCore.DataSource.Core;

public sealed class DSWorkListener<TKey, TDoc, TCreate, TSelect, TUpdate, TDelete, TRestore> : DataSourceListener<TKey, TDoc, TCreate, TSelect, TUpdate, TDelete, TRestore>
{
	IDataSource<TKey, TDoc, TCreate, TSelect, TUpdate, TDelete, TRestore> _dataSource = null!;

	public DSWorkListener(OKeyName keyName)
	{
		KeyName = keyName;
	}

	public override OKeyName KeyName { get; }

	public override async ValueTask OnAttachAsync(IDataSource dataSource)
	{
		_dataSource = (IDataSource<TKey, TDoc, TCreate, TSelect, TUpdate, TDelete, TRestore>)dataSource;
		await ValueTask.CompletedTask;
	}
	public override async ValueTask OnDetachAsync(IDataSource dataSource)
	{
		_dataSource = null!;
		await ValueTask.CompletedTask;
	}

	protected override bool OnAboutInsert(TCreate document, DataSourceInsertOptions? options, CancellationToken cancellationToken)
	{
		return base.OnAboutInsert(document, options, cancellationToken);
	}
}