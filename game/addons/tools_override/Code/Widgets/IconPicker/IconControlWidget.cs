namespace Editor;

/// <summary>
/// Icon picker for Icon strings
/// </summary>
[CustomEditor( typeof( string ), WithAllAttributes = new[] { typeof( IconNameAttribute ) } )]
sealed class IconControlWidget : ControlWidget
{
	public IconControlWidget( SerializedProperty property ) : base( property )
	{
		Cursor = CursorShape.Finger;
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.SetBrushAndPen( Theme.TextControl, Theme.TextControl );
		var icon = SerializedProperty.GetValue<string>();
		var contentRect = LocalRect.Shrink( 4f );
		var iconRect = LocalRect.Shrink( 4f );
		iconRect.Width = iconRect.Height;
		contentRect.Left += iconRect.Width + 6;

		if ( string.IsNullOrEmpty( icon ) )
		{
			var col = Theme.TextControl.WithAlpha( 0.5f );
			Paint.SetBrushAndPen( Color.Transparent, col );
			Paint.DrawRect( iconRect, 1f );
			icon = "Icon";
		}
		else
		{
			Paint.DrawIcon( iconRect, icon, 16f );
		}

		Paint.DrawText( contentRect, icon, TextFlag.LeftCenter );
	}
	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		if ( e.LeftMouseButton )
		{
			string icon = SerializedProperty.GetValue<string>();
			IconPickerWidget.OpenPopup( this, icon, x => { SerializedProperty.SetValue( x ); Update(); } );
		}
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		base.OnContextMenu( e );

		var menu = new Menu( this );
		menu.AddOption( "Clear", "clear", Clear );

		menu.OpenAtCursor();

		e.Accepted = true;
	}

	void Clear()
	{
		SerializedProperty.Parent.NoteStartEdit( SerializedProperty );
		SerializedProperty.SetValue( "" );
		SerializedProperty.Parent.NoteFinishEdit( SerializedProperty );
		Update();
	}

}
