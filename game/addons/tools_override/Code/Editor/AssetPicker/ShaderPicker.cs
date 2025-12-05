using Editor.AssetPickers;

namespace Editor;

[AssetPicker( typeof( Shader ) )]
internal class ShaderPicker : SimplePicker
{
	public ShaderPicker( Widget parent, AssetType assetType, PickerOptions options ) : base( parent, assetType, options )
	{
	}
}
