using Sandbox.Resources;

namespace Editor;

/// <summary>
/// A regular ResourceGeneratorControlWidget but we show a texture preview on the side.
/// </summary>
[CanEdit( typeof( Sandbox.Resources.TextureGenerator ) )]
public class TextureGeneratorControlWidget : ResourceGeneratorControlWidget
{
	TextureWidget TextureWidget;

	public TextureGeneratorControlWidget( ResourceGenerator generator, SerializedProperty property ) : base( generator, property )
	{

	}

	protected override void BuildContent()
	{
		TextureWidget = new TextureWidget();
		TextureWidget.FixedSize = Theme.RowHeight;
		TextureWidget.Padding = 1;

		Layout.Add( TextureWidget );

		base.BuildContent();
	}

	protected override void OnResourceChanged( Resource resource )
	{
		if ( resource is Texture texture )
		{
			TextureWidget.Texture = texture;
		}
	}
}
