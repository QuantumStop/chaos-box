namespace Editor;

public static class ControlSheetFormatter
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

	public static string GetPropertyToolTip( SerializedProperty property, bool includePropertyNames = false )
	{
		var tooltip = "<strong>";
		tooltip += property.DisplayName.WithColor( "#9CDCFE" );
		tooltip += $" - {property.PropertyType.ToRichText()}";
		tooltip += "</strong>";

		if ( includePropertyNames && property.Name != property.DisplayName )
		{
			var propertyName = property.Name.WithColor( "#cce8ba" );
			tooltip += $"<br/>Property Name: {propertyName}";
		}

		var desc = $"{property.Description}";
		if ( !string.IsNullOrWhiteSpace( desc ) )
		{
			tooltip += $"<br/><br/><font>{desc}</font>";
		}

		//
		// If we have an overridden ToString() method, display the default property value inside the tooltip
		//
		{
			// Bail if it's a button
			if ( property.PropertyType == typeof( void ) )
				return tooltip;

			var defaultValue = property.GetDefault();

			if ( defaultValue == null )
				return tooltip;

			var defaultValueType = defaultValue.GetType();
			var toStringMethod = defaultValueType.GetMethods().FirstOrDefault( x => x.Name == "ToString" );

			if ( toStringMethod == null )
				return tooltip;

			if ( toStringMethod.DeclaringType == defaultValueType )
			{
				tooltip += $"<br/><br/><font>Default Value: {defaultValue}</font>";
			}
		}

		return tooltip;
	}

	public static string ToRichText( this Type type )
	{
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
		var color = type.IsValueType ? "#86C691" : "#4EC9B0";
		var suffix = "";

		if ( type.IsGenericType && name.IndexOf( '`' ) is var index and > 0 )
		{
			name = name[..index];
			suffix = $"&lt;{string.Join( ",", type.GetGenericArguments().Select( ToRichText ) )}&gt;";
		}

		return $"{WithColor( name, color )}{suffix}";
	}

	static string WithColor( this string text, string color )
	{
		return $"<span style=\"color: {color};\">{text}</span>";
	}
}
