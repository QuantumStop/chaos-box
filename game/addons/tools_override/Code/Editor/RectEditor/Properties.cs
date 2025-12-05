
namespace Editor.RectEditor;

public class Properties : Widget
{
	private readonly ControlSheet ControlSheet;

	public SerializedObject SerializedObject
	{
		set
		{
			ControlSheet.Clear( true );

			if ( value is not null )
				ControlSheet.AddObject( value );
		}
	}

	public Properties( Widget parent ) : base( parent )
	{
		Name = "Properties";
		WindowTitle = "Properties";
		SetWindowIcon( "edit" );

		MinimumWidth = 200;

		Layout = Layout.Column();
		Layout.Margin = 5;
		Layout.Spacing = 5;

		ControlSheet = new ControlSheet();
		Layout.Add( ControlSheet );

		Layout.AddStretchCell();
	}
}
