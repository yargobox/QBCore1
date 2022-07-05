using System.Reflection;
using QBCore.DataSource.QueryBuilder;
using QBCore.Extensions.ComponentModel;
using QBCore.Extensions.Text;
using QBCore.ObjectFactory;

namespace QBCore.DataSource;

internal sealed class DSDefinition : IDSDefinition
{
	public string Name { get; }

	public Type Key { get; }
	public Type Document { get; }
	public Type CreateDocument { get; }
	public Type SelectDocument { get; }
	public Type UpdateDocument { get; }
	public Type DeleteDocument { get; }
	public Type RestoreDocument { get; }

	public Type DataSourceConcrete { get; }
	public Type DataSourceInterface { get; }
	public Type DataSourceService { get; }

	public DataSourceOptions Options { get; }

	public string DataContextName { get; }

	public IQueryBuilderFactory QBFactory { get; }
	public Func<IServiceProvider, IDataSourceListener>? ListenerFactory { get; }

	public bool IsServiceSingleton { get; }

	public string? ControllerName { get; }
	public bool? IsAutoController { get; }

	internal static readonly string[] ReservedNames = { "area", "controller", "action", "page", "filter", "cell", "id" };
	internal const DataSourceOptions AllDSOperations = DataSourceOptions.CanInsert | DataSourceOptions.CanSelect | DataSourceOptions.CanUpdate | DataSourceOptions.CanDelete | DataSourceOptions.CanRestore;

	public DSDefinition(Type dataSourceConcrete)
	{
		if (!dataSourceConcrete.IsClass || dataSourceConcrete.IsAbstract || dataSourceConcrete.IsGenericType || dataSourceConcrete.IsGenericTypeDefinition
				|| dataSourceConcrete.GetSubclassOf(typeof(DataSource<,,,,,,,>)) == null || Nullable.GetUnderlyingType(dataSourceConcrete) != null)
		{
			throw new InvalidOperationException($"Invalid datasource type {dataSourceConcrete.ToPretty()}.");
		}
		DataSourceConcrete = dataSourceConcrete;

		// Get document types from a generic interface IDataSource<,,,,,,,>
		//
		var types = DataSourceConcrete.GetDataSourceTypes();
		Key = types.TKey;
		Document = types.TDocument;
		CreateDocument = types.TCreate;
		SelectDocument = types.TSelect;
		UpdateDocument = types.TUpdate;
		DeleteDocument = types.TDelete;
		RestoreDocument = types.TRestore;
		DataSourceInterface = DataSourceConcrete.GetInterfaceOf(typeof(IDataSource<,,,,,,>))!;

		// Our building
		//
		var building = new DSBuilder(DataSourceConcrete);

		// Load fields from [DataSource]
		//
		var dataSourceAttr = DataSourceConcrete.GetCustomAttribute<DataSourceAttribute>(false);
		if (dataSourceAttr != null)
		{
			building.Name = dataSourceAttr.Name;
			building.Options = dataSourceAttr.Options ?? DataSourceOptions.None;
			building.DataContextName = dataSourceAttr.DataContextName;
			building.Listener = dataSourceAttr.Listener;
			building.IsServiceSingleton = dataSourceAttr.IsServiceSingleton;
			building.ServiceInterface = dataSourceAttr.ServiceInterface;
			building.QBFactory = dataSourceAttr.QBFactory;
		}

		// Load fields from [DsApiController]
		//
		var controllerAttr = DataSourceConcrete.GetCustomAttribute<DsApiControllerAttribute>(false);
		if (controllerAttr != null)
		{
			building.ControllerName = controllerAttr.Name;
			building.IsAutoController = controllerAttr.IsAutoController;
		}

		// Find a builder and build if any
		//
		var builder = FactoryHelper.FindBuilder<IDSBuilder>(dataSourceAttr?.Builder ?? DataSourceConcrete, dataSourceAttr?.BuilderMethod);
		if (builder != null)
		{
			builder(building);
		}

		// Name
		//
		if (building.Name != null)
		{
			var name = building.Name.Trim();
			
			if (string.IsNullOrWhiteSpace(name) || name.Contains('/') || name.Contains('*'))
			{
				throw new ArgumentException($"{nameof(IDSBuilder)}.{nameof(IDSBuilder.Name)}");
			}

			if (name.Contains("[DS]", StringComparison.OrdinalIgnoreCase))
			{
				Name = name.Replace("[DS]", MakeDSNameFromType(DataSourceConcrete), StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				Name = name;
			}
		}
		else
		{
			Name = MakeDSNameFromType(DataSourceConcrete);
		}
		Name = string.Intern(Name);
		if (ReservedNames.Contains(Name, StringComparer.OrdinalIgnoreCase))
		{
			throw new ArgumentException("These names are reserved and cannot be used as names for a datasource, CDS, or controller: " + string.Join(", ", ReservedNames));
		}

		// Determine or validate supported datasource operations
		//
		Options = building.Options;
		if ((Options & AllDSOperations) == DataSourceOptions.None)
		{
			if (CreateDocument != typeof(NotSupported)) Options |= DataSourceOptions.CanInsert;
			if (SelectDocument != typeof(NotSupported)) Options |= DataSourceOptions.CanSelect;
			if (UpdateDocument != typeof(NotSupported)) Options |= DataSourceOptions.CanUpdate;
			if (DeleteDocument != typeof(NotSupported)) Options |= DataSourceOptions.CanDelete;
			if (RestoreDocument != typeof(NotSupported)) Options |= DataSourceOptions.CanRestore;
		}
		else if (
			(Options.HasFlag(DataSourceOptions.CanInsert) && CreateDocument == typeof(NotSupported)) ||
			(Options.HasFlag(DataSourceOptions.CanSelect) && SelectDocument == typeof(NotSupported)) ||
			(Options.HasFlag(DataSourceOptions.CanUpdate) && UpdateDocument == typeof(NotSupported)) ||
			(Options.HasFlag(DataSourceOptions.CanDelete) && DeleteDocument == typeof(NotSupported)) ||
			(Options.HasFlag(DataSourceOptions.CanRestore) && RestoreDocument == typeof(NotSupported)))
		{
			throw new InvalidOperationException($"Datasource {DataSourceConcrete.ToPretty()} operation cannot be set on type '{nameof(NotSupported)}'.");
		}

		// Validate options
		//
		if ((Options & AllDSOperations) == DataSourceOptions.None)
		{
			throw new InvalidOperationException($"Datasource {DataSourceConcrete.ToPretty()} must have at least one supported operation.");
		}

		if ((Options.HasFlag(DataSourceOptions.RefreshAfterInsert) && !Options.HasFlag(DataSourceOptions.CanInsert))
			|| (Options.HasFlag(DataSourceOptions.RefreshAfterUpdate) && !Options.HasFlag(DataSourceOptions.CanUpdate))
			|| (Options.HasFlag(DataSourceOptions.RefreshAfterDelete) && !Options.HasFlag(DataSourceOptions.CanDelete))
			|| (Options.HasFlag(DataSourceOptions.RefreshAfterRestore) && !Options.HasFlag(DataSourceOptions.CanRestore)))
		{
			throw new InvalidOperationException($"Datasource {DataSourceConcrete.ToPretty()} cannot be refreshed after the operation if the operation itself is not supported.");
		}

		if (Options.HasFlag(DataSourceOptions.CompositeId | DataSourceOptions.CompoundId)
			|| Options.HasFlag(DataSourceOptions.SingleRecord | DataSourceOptions.FewRecords))
		{
			throw new InvalidOperationException($"Datasource {DataSourceConcrete.ToPretty()} is configured inproperly.");
		}

		// DataContextName
		//
		DataContextName = building.DataContextName ?? "default";
		if (string.IsNullOrWhiteSpace(DataContextName))
		{
			throw new ArgumentNullException(nameof(DataContextName));
		}
		DataContextName = string.Intern(DataContextName);

		// ControllerName
		//
		if (building.ControllerName != null || building.IsAutoController != null)
		{
			var controllerName = building.ControllerName?.Trim() ?? "[DS]";
			
			if (string.IsNullOrWhiteSpace(controllerName) || controllerName.Contains('/') || controllerName.Contains('*'))
			{
				throw new ArgumentException(nameof(ControllerName));
			}

			if (controllerName.Contains("[DS:guessPlural]", StringComparison.OrdinalIgnoreCase))
			{
				ControllerName = controllerName.Replace("[DS:guessPlural]", GuessPluralName(Name), StringComparison.OrdinalIgnoreCase);
			}
			else if (controllerName.Contains("[DS]", StringComparison.OrdinalIgnoreCase))
			{
				ControllerName = controllerName.Replace("[DS]", Name, StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				ControllerName = controllerName;
			}

			ControllerName = string.Intern(ControllerName);

			if (ReservedNames.Contains(ControllerName, StringComparer.OrdinalIgnoreCase))
			{
				throw new ArgumentException("These names are reserved and cannot be used as names for a datasource, CDS, or controller: " + string.Join(", ", ReservedNames));
			}

			IsAutoController = building.IsAutoController ?? true;
		}

		// DataSourceService
		//
		if (building.ServiceInterface != null)
		{
			if (building.ServiceInterface == typeof(NotSupported))
			{
				DataSourceService = DataSourceConcrete;
			}
			else if (building.ServiceInterface.GetInterfaces().Contains(DataSourceInterface))
			{
				DataSourceService = building.ServiceInterface;
			}
			else
			{
				throw new InvalidOperationException($"Invalid datasource servive interface {building.ServiceInterface.ToPretty()}.");
			}
		}
		else
		{
			DataSourceService = TryFindDataSourceServiceInterfaceType() ?? DataSourceConcrete;
		}

		if (building.QBFactory == null)
		{
			throw new InvalidOperationException($"Datasource {DataSourceConcrete.ToPretty()} must have a query builder factory.");
		}
		if (!typeof(IQueryBuilderFactory).IsAssignableFrom(building.QBFactory))
		{
			throw new InvalidOperationException($"Invalid query builder factory {building.QBFactory.ToPretty()}.");
		}

		var ctor = building.QBFactory
			.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => IsSupportedQBFactoryCtorParams(x.GetParameters()))
			.FirstOrDefault()
			?? throw new InvalidOperationException($"Invalid query builder factory {building.QBFactory.ToPretty()}.");

		QBFactory = (IQueryBuilderFactory) ctor.Invoke(new object?[]
		{
			DataSourceConcrete,
			Options,
			new QBBuilderMethodRefs
			{
				InsertBuilder = building.InsertBuilder,
				SelectBuilder = building.SelectBuilder,
				UpdateBuilder = building.UpdateBuilder,
				DeleteBuilder = building.DeleteBuilder,
				SoftDelBuilder = building.SoftDelBuilder,
				RestoreBuilder = building.RestoreBuilder
			},
#if DEBUG
			false
#else
			true
#endif
		});

		// Listener
		//
		if (building.Listener != null)
		{
			ListenerFactory = MakeListenerFactory(building.Listener);
		}
	}

	private static string MakeDSNameFromType(Type dataSourceConcrete)
	{
		var fromClassName = dataSourceConcrete.Name.ReplaceEnding("Service").ReplaceEnding("DS").ReplaceEnding("Ds");
		if (string.IsNullOrEmpty(fromClassName))
		{
			fromClassName = dataSourceConcrete.Name;
		}
		return fromClassName;
	}

	private static bool IsSupportedQBFactoryCtorParams(ParameterInfo[] paramInfos)
		=> paramInfos.Length == 4
			&& paramInfos[0].ParameterType == typeof(Type)
			&& paramInfos[1].ParameterType == typeof(DataSourceOptions)
			&& paramInfos[2].ParameterType == typeof(QBBuilderMethodRefs)
			&& paramInfos[3].ParameterType == typeof(bool);

	private Type? TryFindDataSourceServiceInterfaceType()
	{
		return DataSourceConcrete
			.GetInterfaces()
			.Where(x => x.GetInterfaces().Contains(DataSourceInterface))
			.FirstOrDefault();
	}

	private Func<IServiceProvider, IDataSourceListener> MakeListenerFactory(Type listener)
	{
		if (!listener.IsClass || listener.IsAbstract || listener.IsGenericType || listener.IsGenericTypeDefinition ||
			!typeof(IDataSourceListener).IsAssignableFrom(listener) || Nullable.GetUnderlyingType(listener) != null)
		{
			throw new InvalidOperationException($"Invalid datasource listener type {listener.ToPretty()}.");
		}

		var type = listener.GetSubclassOf(typeof(DataSourceListener<,,,,,,>));
		if (type == null)
		{
			throw new InvalidOperationException($"Invalid datasource listener type {listener.ToPretty()}.");
		}

		var genericArgs = type.GetGenericArguments();
		if (Key != genericArgs[0]
			|| Document != genericArgs[1]
			|| CreateDocument != genericArgs[2]
			|| SelectDocument != genericArgs[3]
			|| UpdateDocument != genericArgs[4]
			|| DeleteDocument != genericArgs[5]
			|| RestoreDocument != genericArgs[6])
		{
			throw new InvalidOperationException($"Incompatible datasource listener type {listener.ToPretty()}.");
		}

		var ctors = listener.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
		if (ctors.Length != 1)
		{
			throw new InvalidOperationException($"Datasource listener {listener.ToPretty()} must have a single public constructor.");
		}

		var ctor = ctors[0];
		var parameters = ctor
			.GetParameters()
			.Select(x => (
				x.ParameterType,
				IsNullable: Nullable.GetUnderlyingType(x.ParameterType) != null
			))
			.ToArray();

		return IDataSourceListener (IServiceProvider provider) =>
		{
			var args = parameters
				.Select(x => x.IsNullable ? provider.GetService(x.ParameterType) : provider.GetRequiredInstance(x.ParameterType))
				.ToArray();

			return (IDataSourceListener)ctor.Invoke(args);
		};
	}

	private static readonly string[] _pluralEndingsType1 = { "s", "ss", "sh", "ch", "x", "z" };
	private static readonly char[] _pluralEndingsType2 = { 'a', 'e', 'i', 'o', 'u' };
	private static string GuessPluralName(string name)
	{
		if (name.Length <= 2)
		{
			return name;
		}
		if (_pluralEndingsType1.Any(x => name.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
		{
			return name + "es";
		}
		if (name.EndsWith('f'))
		{
			return name.Substring(0, name.Length - 1) + "ves";
		}
		if (name.EndsWith("fe"))
		{
			return name.Substring(0, name.Length - 2) + "ves";
		}
		if (name.EndsWith('y'))
		{
			var lower = new String(name[name.Length - 2], 1).ToLower()[0];

			return _pluralEndingsType2.Any(x => x == lower)
				? name.Substring(0, name.Length - 1) + 's'
				: name.Substring(0, name.Length - 1) + "ies";
		}
		if (name.EndsWith("is"))
		{
			return name.Substring(0, name.Length - 2) + "es";
		}

		return char.IsDigit(name[name.Length - 2]) ? name : name + 's';
	}
}