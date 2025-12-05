namespace Editor.ProjectSettingPages;

partial class InputActionPanel : Widget
{
	private InputAction Action { get; set; }

	private InputCategory Page { get; set; }

	/// <summary>
	/// Called when an input action is changed
	/// </summary>
	public Action Changed { get; set; }

	int Index { get; set; } = -1;

	public InputActionPanel( InputAction action, InputCategory page ) : base( null )
	{
		Page = page;
		Action = action;
		MinimumHeight = 24;
		Cursor = CursorShape.Finger;
	}

	private string GetFriendlyGamepadCode( GamepadCode value )
	{
		return DisplayInfo.ForEnumValues<GamepadCode>()
			.FirstOrDefault( x => x.value.Equals( value ) )
			.info.Name;
	}

	private float DrawTextWithIcon( Rect r, string text, string icon, int iconSize = 14 )
	{
		var textRect = Paint.DrawText( r, text, TextFlag.RightCenter );

		r.Right -= textRect.Width + 4;

		var iconRect = Paint.DrawIcon( r, icon, iconSize, TextFlag.RightCenter );

		return textRect.Width + iconRect.Width + 24;
	}

	protected override void OnPaint()
	{
		if ( Index == -1 )
			Index = Parent?.Children?.ToList()?.IndexOf( this ) ?? -1;

		Paint.ClearPen();

		if ( Index % 2 == 0 )
		{
			Paint.SetBrush( Color.Black.WithAlpha( 0.05f ) );
			Paint.DrawRect( LocalRect );
		}

		Paint.Antialiasing = true;

		var r = LocalRect;

		Paint.ClearPen();
		Paint.ClearBrush();
		Paint.SetPen( Theme.Text.WithAlpha( Paint.HasMouseOver ? 1f : 0.7f ) );

		if ( Paint.HasMouseOver )
		{
			Paint.DrawIcon( r, "edit", 14, TextFlag.LeftCenter );
			r.Left += 20;
		}

		Paint.SetDefaultFont();
		var name = string.IsNullOrEmpty( Action.Title ) ? Action.Name : Action.Title;
		var nameRect = Paint.DrawText( r, name, TextFlag.LeftCenter );
		var width = 0f;

		if ( !string.IsNullOrEmpty( Action.KeyboardCode ) )
		{
			width = DrawTextWithIcon( r, Action.KeyboardCode.ToUpperInvariant(), "keyboard" );
			r.Right -= width - 8;
		}

		if ( Action.GamepadCode != GamepadCode.None )
		{
			width = DrawTextWithIcon( r, GetFriendlyGamepadCode( Action.GamepadCode ), "sports_esports" );
			r.Right -= width - 8;
		}
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		if ( e.LeftMouseButton )
		{
			ShowModal();
		}

		if ( e.RightMouseButton )
		{
			var m = new ContextMenu( this );
			m.AddOption( "Edit", "edit", ShowModal );

			m.AddOption( "Duplicate", "file_copy", () =>
			{
				Page.AddAction( new InputAction( Action ) );
			} );

			m.AddOption( "Delete", "delete", () =>
			{
				Page.RemoveAction( Action );
			} );

			m.OpenAtCursor();
		}
	}

	/// <summary>
	/// Display a modal for editing/creating the input action in a window
	/// </summary>
	void ShowModal()
	{
		var d = new Dialog( Page );
		d.DeleteOnClose = true;
		d.Layout = Layout.Column();
		d.Layout.Margin = 16;
		d.Window.Size = new( 400, 270 );

		var sheet = new ControlSheet();
		sheet.AddObject( Action.GetSerialized() );
		d.Layout.Add( sheet );
		d.Layout.AddStretchCell( 1 );

		var row = d.Layout.AddRow();
		row.Spacing = 8;
		row.AddStretchCell();
		row.Add( new Button( "Delete", "delete" ) { Clicked = () => { Page.RemoveAction( Action ); d.Close(); } } );
		row.Add( new Button.Primary( "Done", "done" ) { MouseClick = () => { Page.UpdateActionList(); d.Close(); Changed?.Invoke(); } } );


		d.Show();
	}
}
