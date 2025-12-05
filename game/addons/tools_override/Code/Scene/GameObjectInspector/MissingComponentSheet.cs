
namespace Editor;

[CustomEditor( typeof( MissingComponent ) )]
public class MissingComponentSheet : ComponentEditorWidget
{
	TextEdit jsonBox;

	public MissingComponentSheet( SerializedObject obj ) : base( obj )
	{
		var c = obj.Targets.OfType<MissingComponent>().FirstOrDefault();

		jsonBox = new TextEdit( this );
		jsonBox.ReadOnly = true;
		jsonBox.PlainText = c.GetJson().ToJsonString( new JsonSerializerOptions() { WriteIndented = true } );

		SetSizeMode( SizeMode.Default, SizeMode.Default );

		Layout = Layout.Column();
		Layout.Margin = 16;
		Layout.Spacing = 2;
		Layout.Add( jsonBox );
	}
}
