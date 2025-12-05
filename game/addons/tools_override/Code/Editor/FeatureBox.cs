namespace Editor;

public class FeatureBox : Widget
{
	public Widget Header { get; }
	public Widget Content { get; }
	public string Title { set; get; }

	bool _checked = true;
	public bool Value
	{
		get => _checked;
		set
		{
			if ( _checked == value ) return;

			_checked = value;
			Update();

			Content.Visible = _checked;
		}
	}

	public FeatureBox( Widget parent, string title, float margin = 8 ) : base( parent )
	{
		Title = title;

		Layout = Layout.Column();
		Layout.Margin = new( margin, margin, margin, margin );

		Header = new Widget( this );
		Header.MinimumSize = 32;
		Header.MaximumSize = new Vector2( 1024, 32 );
		Header.Cursor = CursorShape.Finger;
		Header.MouseClick = () =>
		{
			Value = !Value;
			SignalValuesChanged();
		};

		Layout.Add( Header );

		Content = Layout.Add( new Widget( this ) );
		Content.Layout = Layout.Column();
		Content.Layout.Margin = new Sandbox.UI.Margin( 8 );
	}

	public void Add( Widget widget )
	{
		Content.Layout.Add( widget );
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground.WithAlpha( 0.2f ) );
		Paint.DrawRect( LocalRect.Shrink( Layout.Margin.Left ), 2 );

		var headerRect = new Rect( Header.Position, Header.Size );

		var checkRect = headerRect;
		checkRect.Left += 4;
		checkRect.Right = checkRect.Left + checkRect.Height;
		checkRect = checkRect.Shrink( 10 );

		if ( Enabled )
		{
			Paint.SetPen( Theme.Text.WithAlpha( 0.2f ), 2 );
			Paint.ClearBrush();
			Paint.DrawCircle( checkRect );

			if ( Value )
			{
				Paint.SetPen( Theme.Text.WithAlpha( 0.8f ) );
				Paint.DrawIcon( checkRect, "check_circle", checkRect.Height + 2 );
			}
		}
		else
		{
			Paint.SetPen( Theme.Text.WithAlpha( 0.8f ) );
			Paint.DrawIcon( checkRect, "close", checkRect.Height + 2 );
		}

		checkRect.Top -= 2;
		checkRect.Bottom += 3;
		checkRect.Left = checkRect.Right + 12;
		checkRect.Right = LocalRect.Right - 32;

		Paint.SetDefaultFont();
		Paint.SetPen( Theme.Text.WithAlpha( 0.8f ) );
		Paint.DrawText( checkRect, Title, TextFlag.LeftCenter );
	}
}
