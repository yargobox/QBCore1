using System.Data;
using System.Linq.Expressions;
using QBCore.Extensions.Linq;

namespace QBCore.DataSource.QueryBuilder.Mongo;

internal sealed class QBSoftDelBuilder<TDoc, TDto> : IQBSoftDelBuilder<TDoc, TDto>, IQBMongoSoftDelBuilder<TDoc, TDto>, ICloneable
{
	public QueryBuilderTypes QueryBuilderType => QueryBuilderTypes.SoftDel;
	public bool IsNormalized { get; private set; }

	public IReadOnlyList<QBContainer> Containers => _containers;
	public IReadOnlyList<QBCondition> Connects => _connects ?? BuilderEmptyLists.Conditions;
	public IReadOnlyList<QBCondition> Conditions => _conditions ?? BuilderEmptyLists.Conditions;
	public IReadOnlyList<QBField> Fields => _fields ?? BuilderEmptyLists.Fields;
	public IReadOnlyList<QBParameter> Parameters => _parameters ?? BuilderEmptyLists.Parameters;
	public IReadOnlyList<QBSortOrder> SortOrders => _sortOrders ?? (_sortOrders = new List<QBSortOrder>(3));
	public IReadOnlyList<QBAggregation> Aggregations => _aggregations ?? (_aggregations = new List<QBAggregation>(3));

	private readonly List<QBContainer> _containers = new List<QBContainer>(3);
	private List<QBField>? _fields;
	private List<QBCondition>? _connects;
	private List<QBCondition>? _conditions;
	private List<QBParameter>? _parameters;
	private List<QBSortOrder>? _sortOrders;
	private List<QBAggregation>? _aggregations;

	public Expression<Func<TDoc, object?>>? IdField
	{
		get => _idField;
		set
		{
			if (_idField != null)
				throw new InvalidOperationException($"Incorrect definition of softdel query builder '{typeof(TDto).ToPretty()}': option '{nameof(IdField)}' is already set.");
			_idField = value;
		}
	}
	public Expression<Func<TDoc, object?>>? DateDeleteField
	{
		get => _dateDeleteField;
		set
		{
			if (_dateDeleteField != null)
				throw new InvalidOperationException($"Incorrect definition of softdel query builder '{typeof(TDto).ToPretty()}': option '{nameof(DateDeleteField)}' is already set.");
			_dateDeleteField = value;
		}
	}


	private Expression<Func<TDoc, object?>>? _idField;
	private Expression<Func<TDoc, object?>>? _dateDeleteField;

	public QBSoftDelBuilder()
	{
		_idField =
			typeof(TDoc).GetProperties().Where(x => x.IsDefined(typeof(DeIdAttribute), true)).FirstOrDefault()?.ToMemberExpression<TDoc>()
			?? typeof(TDoc).GetFields().Where(x => x.IsDefined(typeof(DeIdAttribute), true)).FirstOrDefault()?.ToMemberExpression<TDoc>();

		_dateDeleteField =
				typeof(TDoc).GetProperties().Where(x => x.IsDefined(typeof(DeDeletedAttribute), true)).FirstOrDefault()?.ToMemberExpression<TDoc>()
				?? typeof(TDoc).GetFields().Where(x => x.IsDefined(typeof(DeDeletedAttribute), true)).FirstOrDefault()?.ToMemberExpression<TDoc>();
	}
	public QBSoftDelBuilder(QBSoftDelBuilder<TDoc, TDto> other)
	{
		if (!(IsNormalized = other.IsNormalized))
		{
			other.Normalize();
		}

		_containers = new List<QBContainer>(other._containers);
		if (other._fields != null) _fields = new List<QBField>(other._fields);
		if (other._parameters != null) _parameters = new List<QBParameter>(other._parameters);
		if (other._connects != null) _connects = new List<QBCondition>(other._connects);
		if (other._conditions != null) _conditions = new List<QBCondition>(other._conditions);
		if (other._sortOrders != null) _sortOrders = new List<QBSortOrder>(other._sortOrders);
		if (other._aggregations != null) _aggregations = new List<QBAggregation>(other._aggregations);
		_idField = other._idField;
		_dateDeleteField = other._dateDeleteField;
	}
	public object Clone() => new QBSoftDelBuilder<TDoc, TDto>(this);

	public void Normalize()
	{
		if (IsNormalized) return;
		NormalizeInternal();
		IsNormalized = true;
	}

	private void NormalizeInternal()
	{
		var containers = _containers ?? BuilderEmptyLists.Containers;
		var connects = _connects ?? BuilderEmptyLists.Conditions;
		var conditions = _conditions ?? BuilderEmptyLists.Conditions;

		var rootIndex = containers.FindIndex(x => x.ContainerOperation == ContainerOperations.Select);
		if (rootIndex < 0)
		{
			throw new InvalidOperationException($"Incompatible configuration of softdel query builder '{typeof(TDto).ToPretty()}'.");
		}

		if (containers.Any(x => x.ContainerType != ContainerTypes.Table && x.ContainerType != ContainerTypes.View))
		{
			throw new InvalidOperationException($"Incompatible configuration of softdel query builder '{typeof(TDto).ToPretty()}'.");
		}

		QBContainer? top, bottom, temp;

		// Move a root container to the beginning
		//
		top = containers[rootIndex];
		if (rootIndex != 0)
		{
			containers.RemoveAt(rootIndex);
			containers.Insert(0, top);
		}

		// The root container cannot have connect conditions (depends on others)
		//
		if (connects.Any(x => x.Alias == top.Alias))
		{
			throw new InvalidOperationException($"Incorrect definition of softdel query builder '{typeof(TDto).ToPretty()}'.");
		}

		// Check for connect conditions.
		// Move all containers that do not have connect conditions down (cross joins).
		// If some of them are referenced by others, then the next sort will raise them to the desired position up.
		// Respect the initial order of these containers.
		//
		for (int i = 1, j = 0; i < containers.Count - j; i++)
		{
			temp = containers[i];

			if (temp.ContainerOperation == ContainerOperations.LeftJoin || temp.ContainerOperation == ContainerOperations.Join)
			{
				if (!connects.Any(x => x.IsConnectOnField && x.Alias == temp.Alias))
				{
					throw new InvalidOperationException($"Incorrect definition of softdel query builder '{typeof(TDto).ToPretty()}': JOIN (LEFT JOIN) has to have at least one connect condition on a field.");
				}
			}
			else if (temp.ContainerOperation == ContainerOperations.CrossJoin)
			{
				if (connects.Any(x => x.IsConnectOnField && x.Alias == temp.Alias))
				{
					throw new InvalidOperationException($"Incorrect definition of softdel query builder '{typeof(TDto).ToPretty()}': CROSS JOIN cannot have connection conditions on fields.");
				}

				containers.RemoveAt(i);
				containers.Add(temp);
				i--;
				j++;
			}
		}

		// Sort containers based on the connect condition, avoid an infinite loop.
		// Start with the penultimate container and go up to the second one.
		//
		for (int i = containers.Count - 2; i > 0; i--)
		{
			// remember a container at this position
			temp = containers[i];

		L_RESCAN:
			top = containers[i];
			foreach (var topDependOn in connects
				.Where(x => x.IsOnField && x.Alias == top.Alias)
				.Select(x => x.RefAlias))
			{
				for (int j = i + 1; j < containers.Count; j++)
				{
					bottom = containers[j];
					if (bottom.Alias == topDependOn)
					{
						// The signal for an infinite loop is the return of the container to its previous position
						if (bottom == temp)
						{
							throw new InvalidOperationException($"Incorrect definition of softdel query builder '{typeof(TDto).ToPretty()}'.");
						}

						containers.Swap(i, j);
						goto L_RESCAN;
					}
				}
			}
		}
	}

	private QBSoftDelBuilder<TDoc, TDto> AddContainer(
		Type documentType,
		string alias,
		string dbSideName,
		ContainerTypes containerType,
		ContainerOperations containerOperation)
	{
		if ((containerOperation & ContainerOperations.MainMask) != ContainerOperations.None &&
			_containers.Any(x => x.ContainerOperation.HasFlag(ContainerOperations.MainMask)))
		{
			throw new InvalidOperationException($"Incorrect definition of softdel query builder '{typeof(TDto).ToPretty()}': another initial container has already been added before.");
		}
		if (alias.Contains('.'))
		{
			throw new InvalidOperationException($"Incorrect definition of softdel query builder '{typeof(TDto).ToPretty()}': container alias '{alias}' cannot contain a period character '.'.");
		}
		if (_containers.Any(x => x.Alias == alias))
		{
			throw new InvalidOperationException($"Incorrect definition of softdel query builder '{typeof(TDto).ToPretty()}': initial container '{alias}' has already been added before.");
		}

		IsNormalized = false;
		_containers.Add(new QBContainer(
			DocumentType: documentType,
			Alias: alias,
			DBSideName: dbSideName,
			ContainerType: containerType,
			ContainerOperation: containerOperation
		));

		return this;
	}

	private QBSoftDelBuilder<TDoc, TDto> AddParameter(string name, Type underlyingType, bool isNullable, System.Data.ParameterDirection direction)
	{
		if (_parameters == null)
		{
			_parameters = new List<QBParameter>(8);
		}

		var param = _parameters.FirstOrDefault(x => x.Name == name);
		if (param != null)
		{
			if (param.UnderlyingType != underlyingType || param.IsNullable != isNullable || param.Direction != direction)
			{
				throw new InvalidOperationException($"Incorrect parameter definition of softdel query builder '{typeof(TDto).ToPretty()}': parameter '{name}' has already been added before with different properties");
			}
		}
		else
		{
			IsNormalized = false;
			_parameters.Add(new QBParameter(name, underlyingType, isNullable, direction));
		}

		return this;
	}
}