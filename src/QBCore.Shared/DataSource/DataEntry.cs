using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using QBCore.Extensions.Linq.Expressions;

namespace QBCore.DataSource;

[Flags]
public enum DataEntryFlags
{
	None = 0,
	IdField = 1,
	ReadOnly = 2,
	DateCreatedField = 4,
	DateModifiedField = 8,
	DateUpdatedField = 0x10,
	DateDeletedField = 0x20,
	ForeignId = 0x40
}


[DebuggerDisplay("{ToString(false)}")]
public abstract class DataEntry : IComparable<DataEntry>, IEquatable<DataEntry>
{
	public readonly string Name;
	public readonly DSDocumentInfo Document;
	public readonly DataEntryFlags Flags;
	public readonly Type DataEntryType;
	public readonly Type UnderlyingType;
	public readonly bool IsNullable;
	public readonly int Order;
	public Func<object, object?> Getter => _getter ?? (_getter = MakeGetter());
	protected Func<object, object?>? _getter;
	public Action<object, object?>? Setter => _setter ?? (_setter = MakeSetter());
	protected Action<object, object?>? _setter;

	protected DataEntry(DSDocumentInfo document, MemberInfo memberInfo, DataEntryFlags flags)
	{
		if (document == null)
		{
			throw new ArgumentNullException(nameof(document));
		}
		if (memberInfo == null)
		{
			throw new ArgumentNullException(nameof(memberInfo));
		}

		Name = memberInfo.Name;
		Flags = flags;
		Document = document;

		if (memberInfo is PropertyInfo propertyInfo)
		{
			DataEntryType = propertyInfo.PropertyType;
			UnderlyingType = propertyInfo.PropertyType.GetUnderlyingSystemType();
			IsNullable = propertyInfo.IsNullable();
		}
		else if (memberInfo is FieldInfo fieldInfo)
		{
			DataEntryType = fieldInfo.FieldType;
			UnderlyingType = fieldInfo.FieldType.GetUnderlyingSystemType();
			IsNullable = fieldInfo.IsNullable();
		}
		else
		{
			throw new ArgumentException(nameof(memberInfo));
		}

		Order = memberInfo.GetCustomAttribute<DeOrderAttribute>()?.Order ?? 0;
	}

	public override string ToString() => Name;

	public virtual string ToString(bool shortDocumentName)
		=> shortDocumentName
			? string.Concat(Document.DocumentType.Name, ".", Name)
			: string.Concat(Document.DocumentType.FullName, ".", Name);

	public override int GetHashCode()
		=> Document.DocumentType.GetHashCode() ^ Name.GetHashCode();

	public override bool Equals(object? obj)
		=> obj == null ? false : Equals(obj as DataEntry);

	public bool Equals(DataEntry? other)
		=> other == null
			? false
			: Document.DocumentType == other.Document.DocumentType && Name == other.Name;

	public int CompareTo(DataEntry? other)
	{
		var result = Comparer<Type>.Default.Compare(Document.DocumentType, other?.Document.DocumentType);
		return result != 0 ? result : Name.CompareTo(other!.Name);
	}

	protected virtual Func<object, object?> MakeGetter(MemberInfo? memberInfo = null)
	{
		if (memberInfo == null)
		{
			memberInfo = (MemberInfo?)Document.DocumentType.GetProperty(Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty)
									?? Document.DocumentType.GetField(Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		if (memberInfo is PropertyInfo propertyInfo)
		{
			return propertyInfo.MakeCommonGetter().Compile();
		}
		else if (memberInfo is FieldInfo fieldInfo)
		{
			return fieldInfo.MakeCommonGetter().Compile();
		}

		throw new ArgumentException(nameof(memberInfo));
	}

	protected virtual Action<object, object?>? MakeSetter(MemberInfo? memberInfo = null)
	{
		if (memberInfo == null)
		{
			memberInfo = (MemberInfo?)Document.DocumentType.GetProperty(Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty)
									?? Document.DocumentType.GetField(Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		if (memberInfo is PropertyInfo propertyInfo)
		{
			return propertyInfo.MakeCommonSetter()?.Compile();
		}
		else if (memberInfo is FieldInfo fieldInfo)
		{
			return fieldInfo.MakeCommonSetter()?.Compile();
		}

		throw new ArgumentException(nameof(memberInfo));
	}

	public static DataEntry GetDataEntry(LambdaExpression memberSelector, IDataLayerInfo dataLayer)
	{
		return GetDataEntryOrDefault(memberSelector, dataLayer)
			?? throw new InvalidOperationException($"The document type '{memberSelector.Parameters[0].Type.ToPretty()}' does not have the specified data entry '{memberSelector.GetMemberName()}'.");
	}

	public static DataEntry? GetDataEntryOrDefault(LambdaExpression memberSelector, IDataLayerInfo dataLayer)
	{
		if (memberSelector == null)
		{
			throw new ArgumentNullException(nameof(memberSelector));
		}
		if (memberSelector.Parameters.Count != 1)
		{
			throw new ArgumentException("Only a single parameter lambda expression is allowed.", nameof(memberSelector));
		}

		var memberInfos = memberSelector.GetPropertyOrFieldPath(x => x.Member, true);
		if (memberInfos.Length != 1)
		{
			throw new ArgumentException($"The lambda expression must point to a single property or field in the document that is a valid data entry.", nameof(memberSelector));
		}

		var documentType = memberInfos[0].GetPropertyOrFieldType();
		if (documentType != memberSelector.Parameters[0].Type)
		{
			throw new ArgumentException($"The lambda expression parameter must be of type '{documentType.ToPretty()}' or vice versa.", nameof(memberSelector));
		}

		return DataSourceDocuments.GetOrRegister(documentType, dataLayer).Value.DataEntries.GetValueOrDefault(memberInfos[0].Name);
	}

	public static DataEntry GetDataEntry(MemberInfo memberInfo, IDataLayerInfo dataLayer)
	{
		return GetDataEntryOrDefault(memberInfo, dataLayer)
			?? throw new InvalidOperationException($"The document type '{memberInfo.GetPropertyOrFieldDeclaringType().ToPretty()}' does not have the specified data entry '{memberInfo.Name}'.");
	}

	public static DataEntry? GetDataEntryOrDefault(MemberInfo memberInfo, IDataLayerInfo dataLayer)
	{
		var documentType = memberInfo.GetPropertyOrFieldDeclaringType();
		var documentInfo = DataSourceDocuments.GetOrRegister(documentType, dataLayer);
		return documentInfo.Value.DataEntries.GetValueOrDefault(memberInfo.Name);
	}

	public static DataEntry GetDataEntry<TDocument>(string propertyOrFieldName, IDataLayerInfo dataLayer)
	{
		return GetDataEntry(typeof(TDocument), propertyOrFieldName, dataLayer);
	}

	public static DataEntry? GetDataEntryOrDefault<TDocument>(string propertyOrFieldName, IDataLayerInfo dataLayer)
	{
		return GetDataEntryOrDefault(typeof(TDocument), propertyOrFieldName, dataLayer);
	}

	public static DataEntry GetDataEntry(Type documentType, string propertyOrFieldName, IDataLayerInfo dataLayer)
	{
		return GetDataEntryOrDefault(documentType, propertyOrFieldName, dataLayer)
			?? throw new InvalidOperationException($"The document type '{documentType.ToPretty()}' does not have the specified data entry '{propertyOrFieldName}'.");
	}

	public static DataEntry? GetDataEntryOrDefault(Type documentType, string propertyOrFieldName, IDataLayerInfo dataLayer)
	{
		if (dataLayer == null)
		{
			throw new ArgumentNullException(nameof(dataLayer));
		}
		if (documentType == null)
		{
			throw new ArgumentNullException(nameof(documentType));
		}
		if (propertyOrFieldName == null)
		{
			throw new ArgumentNullException(nameof(propertyOrFieldName));
		}

		var documentInfo = DataSourceDocuments.GetOrRegister(documentType, dataLayer);
		return documentInfo.Value.DataEntries.GetValueOrDefault(propertyOrFieldName);
	}
}


[DebuggerDisplay("{ToString()}")]
public readonly struct DEDefinition<TDocument> : IEquatable<DEDefinition<TDocument>>
{
	public readonly string? Name;
	public readonly DataEntry? DataEntry;

	public DEDefinition(string propertyOrFieldName)
	{
		if (propertyOrFieldName == null)
		{
			throw new ArgumentNullException(nameof(propertyOrFieldName));
		}

		Name = propertyOrFieldName;
		DataEntry = null;
	}

	public DEDefinition(DataEntry dataEntry)
	{
		if (dataEntry == null)
		{
			throw new ArgumentNullException(nameof(dataEntry));
		}

		Name = null;
		DataEntry = dataEntry;
	}

	public readonly DataEntry ToDataEntry(IDataLayerInfo dataLayer)
	{
		if (DataEntry != null)
		{
			return DataEntry;
		}

		return DataEntry.GetDataEntry<TDocument>(Name!, dataLayer);
	}

	public readonly override string ToString()
	{
		return Name ?? DataEntry!.Name;
	}

	public readonly string ToString(bool shortDocumentName)
	{
		if (shortDocumentName)
		{
			return string.Concat(typeof(TDocument).Name, ".", (Name ?? DataEntry!.Name));
		}
		else
		{
			return string.Concat(typeof(TDocument).FullName, ".", (Name ?? DataEntry!.Name));
		}
	}

	public readonly override int GetHashCode()
	{
		return typeof(TDocument).GetHashCode() ^ (Name ?? DataEntry!.Name).GetHashCode();
	}

	public readonly override bool Equals(object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is DEDefinition<TDocument> value2)
		{
			return Equals(value2);
		}
		return false;
	}

	public readonly bool Equals(DEDefinition<TDocument> other)
		=> (Name ?? DataEntry!.Name) == (other.Name ?? other.DataEntry!.Name);

	public static implicit operator DEDefinition<TDocument>(string propertyOrFieldName)
		=> new DEDefinition<TDocument>(propertyOrFieldName);

	public static implicit operator DEDefinition<TDocument>(DataEntry dataEntry)
		=> new DEDefinition<TDocument>(dataEntry);

	public static bool operator ==(DEDefinition<TDocument> a, DEDefinition<TDocument> b)
		=> a.Equals(b);

	public static bool operator !=(DEDefinition<TDocument> a, DEDefinition<TDocument> b)
		=> !a.Equals(b);
}


[DebuggerDisplay("{ToString()}")]
public readonly struct DEDefinition<TDocument, TField> : IEquatable<DEDefinition<TDocument, TField>>, IEquatable<DEDefinition<TDocument>>
{
	public readonly string? Name;
	public readonly DataEntry? DataEntry;

	public DEDefinition(string propertyOrFieldName)
	{
		if (propertyOrFieldName == null)
		{
			throw new ArgumentNullException(nameof(propertyOrFieldName));
		}

		Name = propertyOrFieldName;
		DataEntry = null;
	}

	public DEDefinition(DataEntry dataEntry)
	{
		if (dataEntry == null)
		{
			throw new ArgumentNullException(nameof(dataEntry));
		}
		if (dataEntry.DataEntryType != typeof(TField))
		{
			throw new ArgumentException($"DataEntry '{dataEntry.ToString(true)}' is of type '{dataEntry.DataEntryType.ToPretty()}' which does not match the expected '{typeof(TField).ToPretty()}'.", nameof(dataEntry));
		}

		Name = null;
		DataEntry = dataEntry;
	}

	public readonly DataEntry ToDataEntry(IDataLayerInfo dataLayer)
	{
		if (DataEntry != null)
		{
			return DataEntry;
		}

		var dataEntry = DataEntry.GetDataEntry<TDocument>(Name!, dataLayer);
		if (dataEntry.DataEntryType != typeof(TField))
		{
			throw new InvalidCastException($"DataEntry '{dataEntry.ToString(true)}' is of type '{dataEntry.DataEntryType.ToPretty()}' which does not match the expected '{typeof(TField).ToPretty()}'.");
		}
		return dataEntry;
	}

	public readonly override string ToString()
	{
		return Name ?? DataEntry!.Name;
	}

	public readonly string ToString(bool shortDocumentName)
	{
		if (shortDocumentName)
		{
			return string.Concat(typeof(TDocument).Name, ".", (Name ?? DataEntry!.Name));
		}
		else
		{
			return string.Concat(typeof(TDocument).FullName, ".", (Name ?? DataEntry!.Name));
		}
	}

	public readonly override int GetHashCode()
	{
		return typeof(TDocument).GetHashCode() ^ (Name ?? DataEntry!.Name).GetHashCode();
	}

	public readonly override bool Equals(object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is DEDefinition<TDocument, TField> a)
		{
			return Equals(a);
		}
		if (obj is DEDefinition<TDocument> b)
		{
			return Equals(b);
		}
		return false;
	}

	public readonly bool Equals(DEDefinition<TDocument> other)
		=> (Name ?? DataEntry!.Name) == (other.Name ?? other.DataEntry!.Name);

	public readonly bool Equals(DEDefinition<TDocument, TField> other)
		=> (Name ?? DataEntry!.Name) == (other.Name ?? other.DataEntry!.Name);

	public static bool operator ==(DEDefinition<TDocument, TField> a, DEDefinition<TDocument, TField> b)
		=> a.Equals(b);

	public static bool operator !=(DEDefinition<TDocument, TField> a, DEDefinition<TDocument, TField> b)
		=> !a.Equals(b);

	public static implicit operator DEDefinition<TDocument, TField>(DEDefinition<TDocument> definition)
		=> definition.Name != null
			? new DEDefinition<TDocument, TField>(definition.Name)
			: new DEDefinition<TDocument, TField>(definition.DataEntry!);

	public static implicit operator DEDefinition<TDocument, TField>(string propertyOrFieldName)
		=> new DEDefinition<TDocument, TField>(propertyOrFieldName);

	public static implicit operator DEDefinition<TDocument, TField>(DataEntry dataEntry)
		=> new DEDefinition<TDocument, TField>(dataEntry);
}