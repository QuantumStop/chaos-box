namespace Editor;

[CustomEditor( typeof( Sprite.Frame ) )]
public class SpriteFrameControlWidget : ControlWidget
{
	public SpriteFrameControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Row();

		if ( property.TryGetAsObject( out var serializedObj ) )
		{
			Rebuild( serializedObj );
		}
		else
		{
			var newFrame = new Sprite.Frame();
			property.SetValue( newFrame );
			if ( property.TryGetAsObject( out var newSerializedObj ) )
			{
				Rebuild( newSerializedObj );
			}
		}
	}

	void Rebuild( SerializedObject serializedObj )
	{
		Layout.Clear( true );

		var prop = serializedObj.GetProperty( nameof( Sprite.Frame.Texture ) );
		var textureControl = ControlSheetRow.CreateEditor( prop );
		Layout.Add( textureControl );
	}

	protected override void PaintUnder()
	{
		// Do nothing...
	}
}
