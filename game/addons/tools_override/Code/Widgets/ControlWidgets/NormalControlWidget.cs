namespace Editor;

[CustomEditor( typeof( Vector3 ), WithAllAttributes = new[] { typeof( NormalAttribute ) } )]
public sealed class NormalControlWidget : ControlWidget
{
	public override bool SupportsMultiEdit => true;

	readonly IconButton Button;
	readonly Layout Body;

	public NormalControlWidget( SerializedProperty property ) : base( property )
	{
		HorizontalSizeMode = SizeMode.CanGrow | SizeMode.Expand;

		Layout = Layout.Row();
		Layout.Spacing = 2;

		Body = Layout.AddRow();
		Body.Add( new VectorControlWidget( SerializedProperty ) );

		Layout.AddStretchCell();

		var buttonCell = Layout.AddColumn();
		buttonCell.Alignment = TextFlag.RightTop;

		Button = buttonCell.Add( new IconButton.WithCornerIcon( "north_east" )
		{
			OnClick = ShowMenu,
			Background = Color.Transparent,
			Foreground = Theme.Blue,
			IconSize = 16,
			CornerIconSize = 16,
			CornerIconOffset = 2,
			ToolTip = "Choose normal"
		} );
		Button.Enabled = SerializedProperty.IsEditable;
		Button.FixedSize = Theme.RowHeight;
	}

	void SetNormal( Vector3 v )
	{
		SerializedProperty.SetValue( v.Normal );
	}

	enum Cardinal { Up, Down, Left, Right, Forward, Backward }
	static Vector3 ToVector( Cardinal c ) => c switch
	{
		Cardinal.Up => new Vector3( 0, 0, 1 ),
		Cardinal.Down => new Vector3( 0, 0, -1 ),
		Cardinal.Left => new Vector3( 0, -1, 0 ),
		Cardinal.Right => new Vector3( 0, 1, 0 ),
		Cardinal.Forward => new Vector3( 1, 0, 0 ),
		Cardinal.Backward => new Vector3( -1, 0, 0 ),
		_ => new Vector3( 1, 0, 0 )
	};

	static Option AddOption( ContextMenu menu, string name, Action onClick, bool enabled = true )
	{
		var o = menu.AddOption( name, null, onClick );
		o.Enabled = enabled;
		return o;
	}

	void ShowMenu()
	{
		var menu = new ContextMenu();
		AddOption( menu, "Forward (+X)", () => SetNormal( ToVector( Cardinal.Forward ) ) );
		AddOption( menu, "Backward (-X)", () => SetNormal( ToVector( Cardinal.Backward ) ) );
		AddOption( menu, "Right (+Y)", () => SetNormal( ToVector( Cardinal.Right ) ) );
		AddOption( menu, "Left (-Y)", () => SetNormal( ToVector( Cardinal.Left ) ) );
		AddOption( menu, "Up (+Z)", () => SetNormal( ToVector( Cardinal.Up ) ) );
		AddOption( menu, "Down (-Z)", () => SetNormal( ToVector( Cardinal.Down ) ) );

		menu.OpenNextTo( Button, WidgetAnchor.BottomEnd with { AdjustSize = true, ConstrainToScreen = true } );
	}

	protected override void PaintUnder()
	{
		// nothing
	}
}
