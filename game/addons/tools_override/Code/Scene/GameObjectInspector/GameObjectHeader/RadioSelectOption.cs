namespace Editor;

class RadioSelectOption<T> : Widget
{
	public T Value { get; }
	public bool IsSelected { get; set; }
	public Action OnSelected { get; set; }
	private string Text { get; init; }
	private string Icon { get; init; }

	public RadioSelectOption( string text, string icon, T value )
	{
		Text = text;
		Icon = icon;
		Value = value;
		Layout = Layout.Row();
		HorizontalSizeMode = SizeMode.Expand | SizeMode.CanGrow;
		MinimumHeight = 16f;
		MaximumHeight = 16f;
		MinimumWidth = 100f;
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		IsSelected = true;
		OnSelected?.Invoke();

		base.OnMouseClick( e );
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		var radioColor = IsSelected ? Theme.Blue : Color.White;
		if ( !Enabled ) radioColor = Color.Lerp( radioColor, Color.Black, 0.6f );
		var radioRect = new Rect( 2f, 2f, Height - 4f, Height - 4f );

		if ( IsSelected || !(IsUnderMouse && Enabled) )
			Paint.SetPen( radioColor.WithAlpha( 0.3f ), 1 );
		else if ( IsUnderMouse && Enabled )
			Paint.SetPen( radioColor.WithAlpha( 0.5f ), 1 );

		Paint.SetBrush( radioColor.WithAlpha( 0.2f ) );
		Paint.DrawCircle( radioRect );
		Paint.ClearPen();

		if ( IsSelected )
		{
			Paint.SetBrush( radioColor.WithAlpha( 0.6f ) );
			Paint.DrawCircle( radioRect.Center, new( 8f, 8f ) );
		}
		else if ( IsUnderMouse && Enabled )
		{
			Paint.SetBrush( radioColor.WithAlpha( 0.3f ) );
			Paint.DrawCircle( radioRect.Center, new( 8f, 8f ) );
		}

		Paint.SetDefaultFont();
		var penCol = Theme.TextControl.WithAlpha( 0.8f );
		if ( !Enabled ) penCol = Color.Lerp( penCol, Color.Black, 0.6f );
		Paint.SetPen( penCol );

		var iconRect = Paint.DrawIcon( new( Height + 8f, 0f, Height, Height ), Icon, 16, TextFlag.LeftCenter );
		var textRect = new Rect( iconRect.Right + 6, 0f, Width - 8f, Height - 2f );
		if ( iconRect.Width == 0 ) textRect = new( Height + 8f, 0f, Width - 8f, Height - 2f );
		Paint.DrawText( textRect, Text, TextFlag.LeftCenter );
	}
}
