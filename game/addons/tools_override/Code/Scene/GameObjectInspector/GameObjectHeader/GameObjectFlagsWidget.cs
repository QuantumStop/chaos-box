
namespace Editor;

class GameObjectFlagsWidget : Widget
{
	SerializedObject Target;
	SerializedProperty Flags;
	public GameObjectFlagsWidget( SerializedObject targetObject )
	{
		Target = targetObject;
		Cursor = CursorShape.Finger;
		MinimumWidth = Theme.RowHeight;
		HorizontalSizeMode = SizeMode.CanShrink;
		Flags = Target.GetProperty( nameof( GameObject.Flags ) );
		ToolTip = "Advanced Flags";
	}

	protected override Vector2 SizeHint() => Theme.RowHeight;
	protected override Vector2 MinimumSizeHint() => Theme.RowHeight;

	protected override void OnMousePress( MouseEvent e )
	{
		if ( ReadOnly ) return;
		Open();
		e.Accepted = true;
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		var rect = LocalRect.Shrink( 2 );
		var icon = "expand_more";

		if ( IsUnderMouse )
		{
			Paint.SetPen( Theme.Text.WithAlpha( 1.0f ) );
			Paint.DrawIcon( rect, icon, 13 );
		}
		else
		{
			Paint.SetPen( Theme.Text.WithAlpha( 0.7f ) );
			Paint.DrawIcon( rect, icon, 13 );
		}
	}
	void Open()
	{

		var flags = Flags.GetValue<GameObjectFlags>();

		var menu = new ContextMenu( this );

		{


			{
				bool has = flags.Contains( GameObjectFlags.EditorOnly );
				var o = menu.AddOption( "Editor Only", null, () => SetFlagBit( GameObjectFlags.EditorOnly, !has ) );
				o.Checkable = true;
				o.Checked = has;
			}

			{
				bool has = flags.Contains( GameObjectFlags.Absolute );
				var o = menu.AddOption( "Position Absolute", null, () => SetFlagBit( GameObjectFlags.Absolute, !has ) );
				o.Checkable = true;
				o.Checked = has;
			}
		}

		menu.OpenAtCursor( false );
	}

	private void SetFlagBit( GameObjectFlags f, bool v )
	{
		var flags = Flags.GetValue<GameObjectFlags>();
		Flags.SetValue( flags.WithFlag( f, v ) );
	}
}
