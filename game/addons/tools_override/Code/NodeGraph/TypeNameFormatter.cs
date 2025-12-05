using Facepunch.ActionGraphs;

namespace Editor.NodeEditor;

public static class TypeNameFormatter
{
	private static Dictionary<Type, string> SystemTypeAliases { get; } = new()
	{
		{ typeof(bool), "bool" },
		{ typeof(byte), "byte" },
		{ typeof(char), "char" },
		{ typeof(decimal), "decimal" },
		{ typeof(double), "double" },
		{ typeof(float), "float" },
		{ typeof(int), "int" },
		{ typeof(long), "long" },
		{ typeof(object), "object" },
		{ typeof(sbyte), "sbyte" },
		{ typeof(short), "short" },
		{ typeof(string), "string" },
		{ typeof(uint), "uint" },
		{ typeof(ulong), "ulong" },
		{ typeof(ushort), "ushort" },
		{ typeof(void), "void" }
	};

	public static string ToRichText( this Type type )
	{
		if ( Either.TryUnwrap( type, out var types ) )
		{
			return string.Join( " | ", types.Select( ToRichText ) );
		}

		if ( Nullable.GetUnderlyingType( type ) is { } underlyingType )
		{
			return $"{underlyingType.ToRichText()}?";
		}

		if ( type.IsArray && type.GetElementType() is { } elemType )
		{
			return $"{elemType.ToRichText()}[]";
		}

		if ( type.IsGenericParameter )
		{
			return WithColor( type.Name, "#B8D7A3" );
		}

		if ( SystemTypeAliases.TryGetValue( type, out var systemType ) )
		{
			return WithColor( systemType, "#569CD6" );
		}

		var name = type.Name;
		var color = type == typeof( Signal ) ? "#FFFFFF" : type.IsValueType ? "#86C691" : "#4EC9B0";
		var suffix = "";

		if ( type.IsGenericType && name.IndexOf( '`' ) is var index and > 0 )
		{
			name = name[..index];
			suffix = $"&lt;{string.Join( ",", type.GetGenericArguments().Select( ToRichText ) )}&gt;";
		}

		return $"{WithColor( name, color )}{suffix}";
	}

	public static string WithColor( this string value, string color )
	{
		return $"<span style=\"color: {color};\">{value}</span>";
	}
}

