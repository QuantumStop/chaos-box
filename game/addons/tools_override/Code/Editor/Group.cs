namespace Editor;

public class Group : Widget
{
	Widget widget;

	public string Title { get; set; } = "Untitled Group";
	public string Icon { get; set; }

	protected int headerSize;

	public Action OnCreateWidget { get; set; }

	public Group( Widget parent ) : base( parent )
	{
		SetHeaderSize( (int)(Theme.RowHeight * 2f) );
	}

	public void SetWidget( Widget w )
	{
		widget?.Destroy();

		widget = w;
		widget.Parent = this;
		widget.Position = new Vector2( 0, headerSize );
		widget.AdjustSize();
		widget.Width = Width;

		Update();
		DoLayout();
	}

	public void SetHeaderSize( int height )
	{
		headerSize = height;
		MinimumSize = height;

		if ( widget.IsValid() )
			widget.Position = new Vector2( 0, headerSize );
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		if ( widget.IsValid() )
		{
			widget.AdjustSize();
			widget.Width = Width;

			if ( !Animate.IsActive( this ) )
				FixedHeight = IdealHeight;
		}
	}

	protected override void OnPaint()
	{
		var headerRect = new Rect( 0, 0, Width, headerSize );
		headerRect.Bottom--;

		Paint.ClearPen();
		Paint.SetBrush( Theme.ButtonBackground.WithAlpha( 0.1f ) );
		Paint.DrawRect( LocalRect.Shrink( 0, 1 ), 4.0f );

		Paint.ClearBrush();

		var rect = new Rect( 0, Size );

		rect.Height = headerSize;

		Paint.SetPen( Theme.Text.WithAlpha( 0.5f ) );

		rect.Left += 14;

		if ( !string.IsNullOrWhiteSpace( Icon ) )
		{
			Paint.SetPen( Theme.Text.WithAlpha( 0.8f ) );
			Paint.DrawIcon( headerRect.Shrink( rect.Left, 0, 0, 0 ), Icon, 18, TextFlag.LeftCenter );

			rect.Left += 24;
		}

		Paint.SetDefaultFont( 8, 400 );
		Paint.SetPen( Theme.Text.WithAlpha( 1.0f ) );
		Paint.DrawText( headerRect.Shrink( rect.Left, 0, 0, 0 ), Title, TextFlag.LeftCenter );

		var bodyRect = new Rect( 0, headerSize, Width, Height - headerSize );
		bodyRect.Bottom--;
		bodyRect.Right--;
	}

	float IdealHeight => headerSize + widget.Height;

	public void SetHeight()
	{
		Animate.CancelAll( this, true );
		FixedHeight = IdealHeight;
		Update();
	}
}
