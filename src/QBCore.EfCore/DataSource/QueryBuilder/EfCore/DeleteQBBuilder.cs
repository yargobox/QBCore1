using System.Linq.Expressions;
using QBCore.Extensions.Internals;

namespace QBCore.DataSource.QueryBuilder.EfCore;

internal sealed class DeleteQBBuilder<TDoc, TDelete> : CommonQBBuilder<TDoc, TDelete>, IEfCoreDeleteQBBuilder<TDoc, TDelete> where TDoc : class
{
	public override QueryBuilderTypes QueryBuilderType => QueryBuilderTypes.Delete;

	public DeleteQBBuilder()
	{
		if (DocInfo.IdField == null) throw new InvalidOperationException($"Document '{typeof(TDoc).ToPretty()}' does not have an id data entry.");
	}
	public DeleteQBBuilder(DeleteQBBuilder<TDoc, TDelete> other) : base(other) { }
	public DeleteQBBuilder(IQBBuilder other) : this()
	{
		if (other is null) throw new ArgumentNullException(nameof(other));

		if (other.DocType != typeof(TDoc))
		{
			throw new InvalidOperationException($"Could not make delete query builder '{typeof(TDoc).ToPretty()}, {typeof(TDelete).ToPretty()}' from '{other.DocType.ToPretty()}, {other.DtoType.ToPretty()}'.");
		}

		var top = other.Containers.FirstOrDefault();
		if (top?.DocumentType != typeof(TDoc) || top.ContainerType != ContainerTypes.Table)
		{
			throw new InvalidOperationException($"Could not make delete query builder '{typeof(TDoc).ToPretty()}, {typeof(TDelete).ToPretty()}' from '{other.DocType.ToPretty()}, {other.DtoType.ToPretty()}'.");
		}

		AutoBuild(top.DBSideName);
	}

	protected override void OnPrepare()
	{
		var top = Containers.FirstOrDefault();
		if (top?.ContainerOperation != ContainerOperations.Delete)
		{
			throw new InvalidOperationException($"Incompatible configuration of delete query builder '{typeof(TDelete).ToPretty()}'.");
		}

		if (Conditions.Count == 0)
		{
			throw EX.QueryBuilder.Make.QueryBuilderMustHaveAtLeastOneCondition(DataLayer.Name, QueryBuilderType.ToString());
		}
	}

	public override QBBuilder<TDoc, TDelete> AutoBuild(string? tableName = null)
	{
		if (Containers.Count > 0)
		{
			throw new InvalidOperationException($"Delete query builder '{typeof(TDelete).ToPretty()}' has already been initialized.");
		}

		Delete(tableName)
			.Condition(DocInfo.IdField!, FO.Equal, "@id");

		return this;
	}
	IEfCoreDeleteQBBuilder<TDoc, TDelete> IEfCoreDeleteQBBuilder<TDoc, TDelete>.AutoBuild(string? tableName)
	{
		AutoBuild(tableName);
		return this;
	}

	public override QBBuilder<TDoc, TDelete> Delete(string? tableName = null)
	{
		return AddContainer(tableName, ContainerTypes.Table, ContainerOperations.Delete);
	}
	IEfCoreDeleteQBBuilder<TDoc, TDelete> IEfCoreDeleteQBBuilder<TDoc, TDelete>.Delete(string? tableName)
	{
		AddContainer(tableName, ContainerTypes.Table, ContainerOperations.Delete);
		return this;
	}

	IEfCoreDeleteQBBuilder<TDoc, TDelete> IEfCoreDeleteQBBuilder<TDoc, TDelete>.Condition(Expression<Func<TDoc, object?>> field, object? constValue, FO operation)
	{
		AddCondition(QBConditionFlags.OnConst, field, constValue, null, operation);
		return this;
	}
	IEfCoreDeleteQBBuilder<TDoc, TDelete> IEfCoreDeleteQBBuilder<TDoc, TDelete>.Condition(Expression<Func<TDoc, object?>> field, FO operation, string paramName)
	{
		AddCondition(QBConditionFlags.OnParam, field, null, paramName, operation);
		return this;
	}

	IEfCoreDeleteQBBuilder<TDoc, TDelete> IEfCoreDeleteQBBuilder<TDoc, TDelete>.Begin()
	{
		Begin();
		return this;
	}
	IEfCoreDeleteQBBuilder<TDoc, TDelete> IEfCoreDeleteQBBuilder<TDoc, TDelete>.End()
	{
		End();
		return this;
	}

	IEfCoreDeleteQBBuilder<TDoc, TDelete> IEfCoreDeleteQBBuilder<TDoc, TDelete>.And()
	{
		And();
		return this;
	}
	IEfCoreDeleteQBBuilder<TDoc, TDelete> IEfCoreDeleteQBBuilder<TDoc, TDelete>.Or()
	{
		Or();
		return this;
	}
}