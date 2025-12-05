namespace Editor;

/// <summary>
/// A widget that acts like a window. It can be dragged around its parent.
/// </summary>
public class WidgetWindow : Widget
{
	public string Icon { get; set; } = null;
	public float HeaderHeight => Theme.RowHeight;

	public bool IsGrabbable { get; set; } = true;
	public bool IsBeingDragged => grabbed;
	public bool IsFloating { get; private set; }

	public Rect InnerRect => new Rect( 0, HeaderHeight, Width, Height - HeaderHeight );

	public WidgetWindow( Widget parent = null, string windowTitle = "Widget Window" ) : base( parent )
	{
		ContentMargins = new( 0, HeaderHeight, 0, 0 );
		WindowTitle = windowTitle;
		Size = 200;
	}

	/// <summary>
	/// When you call this you are releasing this from its parent and making it a floating window.
	/// </summary>
	public void Float()
	{
		NoSystemBackground = true;
		TranslucentBackground = true;
		WindowFlags = WindowFlags.Tool | WindowFlags.FramelessWindowHint;
		Show();
		IsFloating = true;
	}

	bool grabbed;
	Vector2 grabPosition;

	protected override void OnPaint()
	{
		var headerRect = new Rect( LocalRect.Left, LocalRect.Top, LocalRect.Width, HeaderHeight + 4 );

		Paint.Antialiasing = true;

		// Background
		Paint.SetPen( Theme.ControlBackground.Darken( 0.2f ) );
		Paint.SetBrush( Theme.ControlBackground.WithAlpha( 0.85f ) );
		Paint.DrawRect( LocalRect, 4 );

		// title bar
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground.WithAlpha( 0.85f ) );
		Paint.DrawRect( headerRect, 4 );

		float left = 6.0f;
		if ( !string.IsNullOrEmpty( Icon ) )
		{
			Paint.SetPen( Theme.TextControl.WithAlpha( 0.3f ) );
			var r = Paint.DrawIcon( headerRect.Shrink( left, 0 ), Icon, 14, TextFlag.LeftCenter );
			left += r.Width + 4;
		}

		// title
		if ( !string.IsNullOrWhiteSpace( WindowTitle ) )
		{
			Paint.SetPen( Theme.TextControl.WithAlpha( 0.5f ) );
			Paint.SetDefaultFont();
			Paint.DrawText( headerRect.Shrink( left, 0 ), WindowTitle, TextFlag.LeftCenter | TextFlag.SingleLine );
		}
	}

	protected override void DoLayout()
	{
		//AdjustSize();
		base.DoLayout();

		if ( Parent.IsValid() && !IsFloating )
		{
			ConstrainTo( Parent.LocalRect );
		}
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( IsGrabbable && e.LeftMouseButton && e.LocalPosition.y < HeaderHeight )
		{
			grabbed = true;
			grabPosition = e.LocalPosition;
			Raise();
		}
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		grabbed = false;
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		if ( grabbed )
		{
			if ( IsFloating )
			{
				Position = e.ScreenPosition - grabPosition;
			}
			else
			{
				var localPosition = e.ScreenPosition - Parent.ScreenPosition;
				Position = localPosition - grabPosition;
				ConstrainTo( Parent.LocalRect );
			}
		}
	}

	internal void Release()
	{
		grabbed = false;
	}
}
