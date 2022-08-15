using System.Reflection;
using QBCore.ObjectFactory;

namespace QBCore.DataSource;

public static class DataSourceDocuments
{
	private static Func<Type, bool> _documentExclusionSelector = _ => false;

	public static Func<Type, bool> DocumentExclusionSelector
	{
		get => _documentExclusionSelector;
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			Interlocked.Exchange(ref _documentExclusionSelector, value);
		}
	}

	public static DSDocumentInfo GetOrRegister(Type documentType, IDataLayerInfo dataLayer)
		=> GetOrRegister(StaticFactory.Documents, documentType, dataLayer);

	public static DSDocumentInfo GetOrRegister(this IFactoryObjectDictionary<Type, LazyObject<DSDocumentInfo>> @this, Type documentType, IDataLayerInfo dataLayer)
	{
		var registry = (IFactoryObjectRegistry<Type, LazyObject<DSDocumentInfo>>)@this;

		var doc = registry.GetValueOrDefault(documentType);
		if (doc == null)
		{
			foreach (var selectedType in GetReferencingTypes(documentType, dataLayer.IsDocumentType, true))
			{
				// a new var for each type to do not mess up with types in the lambda expression below
				var type = selectedType;
				if (doc == null)
					doc = registry.TryGetOrRegisterObject(type, x => new LazyObject<DSDocumentInfo>(() => dataLayer.CreateDocumentInfo(type)));
				else
					registry.TryGetOrRegisterObject(type, x => new LazyObject<DSDocumentInfo>(() => dataLayer.CreateDocumentInfo(type)));
			}

			if (doc == null)
			{
				throw new InvalidOperationException($"Could not register as a datasource document type '{documentType.ToPretty()}'.");
			}
		}
		return doc.Value;
	}

	public static IEnumerable<Type> GetReferencingTypes(Type documentType, Func<Type, bool> documentTypesSelector, bool includeThisOne = false)
	{
		if (documentType == null)
		{
			throw new ArgumentNullException(nameof(documentType));
		}
		if (documentTypesSelector == null)
		{
			throw new ArgumentNullException(nameof(documentTypesSelector));
		}

		if (!documentTypesSelector(documentType))
		{
			return Enumerable.Empty<Type>();
		}

		var pool = new Dictionary<Type, bool>();
		if (includeThisOne)
		{
			pool.Add(documentType, true);
		}

		GetReferencingTypes(documentType, documentTypesSelector, pool);

		return pool.Where(x => x.Value).Select(x => x.Key);
	}
	private static void GetReferencingTypes(Type documentType, Func<Type, bool> documentTypesSelector, Dictionary<Type, bool> pool)
	{
		bool isDocumentType;
		Type type;

		foreach (var candidate in DSDocumentInfo.GetDataEntryCandidates(documentType))
		{
			if (candidate.memberInfo is PropertyInfo propertyInfo)
			{
				type = propertyInfo.PropertyType;
			}
			else if (candidate.memberInfo is FieldInfo fieldInfo)
			{
				type = fieldInfo.FieldType;
			}
			else
			{
				continue;
			}

			isDocumentType = documentTypesSelector(type);
			if (pool.TryAdd(type, isDocumentType))
			{
				if (isDocumentType)
				{
					GetReferencingTypes(type, documentTypesSelector, pool);
				}
				else
				{
					foreach (var genericEnumerable in type.GetInterfacesOf(typeof(IEnumerable<>)))
					{
						type = genericEnumerable.GetGenericArguments()[0];
						isDocumentType = documentTypesSelector(type);
						if (pool.TryAdd(type, isDocumentType) && isDocumentType)
						{
							GetReferencingTypes(type, documentTypesSelector, pool);
						}
					}
				}
			}
		}
	}
}