using Sandbox.Audio;

namespace Editor;

/// <summary>
/// Dropdown selection for font names
/// </summary>
[CustomEditor( typeof( string ), WithAllAttributes = new[] { typeof( FontNameAttribute ) } )]
sealed class FontControlWidget : DropdownControlWidget<string>
{
	public FontControlWidget( SerializedProperty property ) : base( property )
	{
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		return EditorUtility.FontFamilies.Order().Cast<object>();
	}
}
