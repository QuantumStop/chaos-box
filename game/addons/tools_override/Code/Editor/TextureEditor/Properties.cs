

using Sandbox.Diagnostics;

namespace Editor.TextureEditor;

public class Properties : ScrollArea
{
	public Properties( Widget parent ) : base( parent )
	{
		Name = "Properties";
		WindowTitle = "Properties";
		SetWindowIcon( "edit" );

		MinimumWidth = 300;
		Canvas = new Widget();
		Canvas.Layout = Layout.Column();
	}

	public void SetTextureFile( TextureFile textureFile )
	{
		Canvas.Layout.Clear( true );

		if ( textureFile is not null )
		{
			var so = textureFile.GetSerialized();
			Assert.NotNull( so );
			var sheet = ControlSheet.Create( so );
			Canvas.Layout.Add( sheet );

			so.OnPropertyChanged += OnEdited;
		}

		Canvas.Layout.AddStretchCell();
	}

	private void OnEdited( SerializedProperty property )
	{
		ChildValuesChanged( this );
	}
}
