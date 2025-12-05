using Editor.Inspectors;

namespace Editor.SoundEditor;

public class Properties : Widget
{
	private readonly SoundFileCompileSettings Settings;

	public Properties( Widget parent ) : base( parent )
	{
		Name = "Properties";
		WindowTitle = "Properties";
		SetWindowIcon( "edit" );

		MinimumWidth = 400;

		Layout = Layout.Column();
		Layout.Margin = 4;

		Settings = new SoundFileCompileSettings( this );
		Settings.SetSizeMode( SizeMode.Flexible, SizeMode.CanGrow );

		var scroll = new ScrollArea( this );
		scroll.Canvas = new Widget( scroll );
		scroll.Canvas.Layout = Layout.Column();
		scroll.Canvas.Layout.Add( Settings );
		scroll.Canvas.Layout.AddStretchCell();

		Layout.Add( scroll );
	}

	public void SetAsset( Asset asset )
	{
		if ( asset == null )
			return;

		Settings.SetAsset( asset );
	}
}
