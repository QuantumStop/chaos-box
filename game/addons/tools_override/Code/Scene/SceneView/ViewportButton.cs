namespace Editor;

internal class ViewportButton : Widget
{
	private string Icon;
	private Action OnClick;

	public ViewportButton( string icon, Action onClick ) : base( null )
	{
		Icon = icon;
		OnClick = onClick;

		FixedWidth = Theme.ControlHeight;
		FixedHeight = Theme.ControlHeight;
		Cursor = CursorShape.Finger;
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton )
		{
			Activate();
		}
	}

	public void Activate()
	{
		OnClick();
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		Paint.ClearBrush();
		Paint.SetPen( Paint.HasMouseOver ? Theme.TextLight.Lighten( 0.8f ) : Theme.TextLight );
		Paint.DrawIcon( LocalRect, Icon, HeaderBarStyle.IconSize, TextFlag.Center );
	}
}

internal class ViewportMainCreateButton : Widget
{
	private string Icon;
	private Action OnClick;

	public ViewportMainCreateButton( string icon, string text, Action onClick ) : base( null )
	{
		Icon = icon;
		OnClick = onClick;

		FixedSize = 32;
		Cursor = CursorShape.Finger;

		ToolTip = text;
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton )
		{
			Activate();
		}
	}

	public void Activate()
	{
		OnClick();
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		Paint.ClearPen();
		Paint.ClearBrush();

		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, Theme.ControlRadius );

		Paint.SetPen( Theme.Blue );
		Paint.DrawIcon( LocalRect, Icon, HeaderBarStyle.IconSize, TextFlag.Center );
	}
}
