using System.Reflection;

namespace QBCore.Extensions.Reflection;

public static class ExtensionsForReflection
{
	public static string ToPretty(this Type type, int recursionLevel = 8, bool expandNullable = false)
	{
		if (type.IsArray)
		{
			return $"{ToPretty(type.GetElementType()!, recursionLevel, expandNullable)}[]";
		}

		if (type.IsGenericType)
		{
			// find generic type name
			var genericTypeName = type.GetGenericTypeDefinition().Name;
			var index = genericTypeName.IndexOf('`');
			if (index != -1)
			{
				genericTypeName = genericTypeName.Substring(0, index);
			}

			// retrieve generic type aguments
			var argNames = new List<string>();
			var genericTypeArgs = type.GetGenericArguments();
			foreach (var genericTypeArg in genericTypeArgs)
			{
				argNames.Add(
					recursionLevel > 0
						? ToPretty(genericTypeArg, recursionLevel - 1, expandNullable)
						: "?");
			}

			// if type is nullable and want compact notation '?'
			if (!expandNullable && Nullable.GetUnderlyingType(type) != null)
			{
				return $"{argNames[0]}?";
			}

			// compose common generic type format "T<T1, T2, ...>"
			return $"{genericTypeName}<{string.Join(", ", argNames)}>";
		}

		return type.Name;
	}

	public static Type? GetSubclassOf<T>(this Type @this)
		=> GetSubclassOf(@this, typeof(T));

	public static Type? GetSubclassOf(this Type @this, Type test)
	{
		Type? type = @this;
		while (type != null)
		{
			if (test.IsGenericTypeDefinition)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == test)
				{
					return type;
				}
			}
			else if (type == test)
			{
				return type;
			}

			type = type.BaseType;
		}
		return null;
	}

	public static Type? GetInterfaceOf(this Type @this, Type test)
	{
		if (test.IsGenericTypeDefinition)
		{
			return @this.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == test);
		}
		else
		{
			return @this.GetInterfaces().FirstOrDefault(i => i == test);
		}
	}

	public static Type GetUnderlyingSystemType(this Type type)
	{
		if (type.IsGenericType)
		{
			return type.GetGenericTypeDefinition() == typeof(Nullable<>) ?
				type.GenericTypeArguments[0].UnderlyingSystemType :
				type.UnderlyingSystemType;
		}
		else
		{
			return Nullable.GetUnderlyingType(type)?.UnderlyingSystemType ?? type.UnderlyingSystemType;
		}
	}

	public static bool IsNullable(this PropertyInfo propertyInfo)
	{
		var propertyType = propertyInfo.PropertyType;

		if (propertyType.IsValueType)
		{
			return Nullable.GetUnderlyingType(propertyType) != null;
		}
		else if (propertyType.IsGenericType)
		{
			return propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		var classNullableContextAttribute = propertyInfo?.DeclaringType?.CustomAttributes
			.FirstOrDefault(c => c.AttributeType.Name == "NullableContextAttribute");

		var classNullableContext = classNullableContextAttribute
			?.ConstructorArguments
			.First(ca => ca.ArgumentType.Name == "Byte")
			.Value;

		// EDIT: This logic is not correct for nullable generic types
		var propertyNullableContext = propertyInfo?.CustomAttributes
			.FirstOrDefault(c => c.AttributeType.Name == "NullableAttribute")
			?.ConstructorArguments
			.First(ca => ca.ArgumentType.Name == "Byte")
			.Value;

		// If the property does not have the nullable attribute then it's 
		// nullability is determined by the declaring class 
		propertyNullableContext ??= classNullableContext;

		// If NullableContextAttribute on class is not set and the property
		// does not have the NullableAttribute, then the proeprty is non nullable
		if (propertyNullableContext == null)
		{
			return true;
		}

		// nullableContext == 0 means context is null oblivious (Ex. Pre C#8)
		// nullableContext == 1 means not nullable
		// nullableContext == 2 means nullable
		switch ((byte)propertyNullableContext)
		{
			case 1:// NonNullableContextValue
				return false;
			case 2:// NullableContextValue
				return true;
			default:
				throw new NotSupportedException();
		}
	}
	public static bool IsNullable(this FieldInfo fieldInfo)
	{
		var propertyType = fieldInfo.FieldType;

		if (propertyType.IsValueType)
		{
			return Nullable.GetUnderlyingType(propertyType) != null;
		}
		else if (propertyType.IsGenericType)
		{
			return propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		var classNullableContextAttribute = fieldInfo?.DeclaringType?.CustomAttributes
			.FirstOrDefault(c => c.AttributeType.Name == "NullableContextAttribute");

		var classNullableContext = classNullableContextAttribute
			?.ConstructorArguments
			.First(ca => ca.ArgumentType.Name == "Byte")
			.Value;

		// EDIT: This logic is not correct for nullable generic types
		var propertyNullableContext = fieldInfo?.CustomAttributes
			.FirstOrDefault(c => c.AttributeType.Name == "NullableAttribute")
			?.ConstructorArguments
			.First(ca => ca.ArgumentType.Name == "Byte")
			.Value;

		// If the property does not have the nullable attribute then it's 
		// nullability is determined by the declaring class 
		propertyNullableContext ??= classNullableContext;

		// If NullableContextAttribute on class is not set and the property
		// does not have the NullableAttribute, then the proeprty is non nullable
		if (propertyNullableContext == null)
		{
			return true;
		}

		// nullableContext == 0 means context is null oblivious (Ex. Pre C#8)
		// nullableContext == 1 means not nullable
		// nullableContext == 2 means nullable
		switch ((byte)propertyNullableContext)
		{
			case 1:// NonNullableContextValue
				return false;
			case 2:// NullableContextValue
				return true;
			default:
				throw new NotSupportedException();
		}
	}
}