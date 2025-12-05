namespace Editor;

public class ToggleSwitch : Widget
{
	private bool _value;

	public bool Value
	{
		get => _value;
		set
		{
			_value = value;
			AnimateCircle();
		}
	}

	public string Text { get; set; }

	public ToggleSwitch( string text, Widget parent = null ) : base( parent )
	{
		Text = text;
	}

	protected override Vector2 SizeHint()
	{
		return new Vector2( 40, 20 );
	}

	private float CirclePos = 0.0f; // 0 to 1

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.ClearBrush();
		Paint.SetDefaultFont();
		Paint.Antialiasing = true;

		var r = LocalRect;
		r.Size = new Vector2( 40, 20 );

		if ( Value )
			Paint.SetBrush( Theme.Primary );
		else
			Paint.SetBrush( Theme.ButtonBackground );

		Paint.SetPen( Theme.Text.WithAlpha( 0.25f ), 1.0f );
		Paint.DrawRect( r, 10.0f );
		Paint.ClearPen();

		if ( Paint.HasMouseOver )
		{
			Paint.SetBrush( Color.White.WithAlpha( 0.2f ) );
			Paint.DrawRect( r, 10.0f );
		}

		r = r.Shrink( 4.0f );

		{
			var circle = r;
			circle.Size = circle.Size.WithX( circle.Size.y );

			circle.Position = circle.Position.WithX( r.Left.LerpTo( r.Right - 12.0f, CirclePos ) );

			Paint.SetBrush( Color.White.WithAlpha( 0.5f ) );
			Paint.DrawRect( circle, 16.0f );
		}

		// Draw label
		if ( !string.IsNullOrEmpty( Text ) )
		{
			r = LocalRect + new Vector2( 40 + 8, 0 );
			Paint.SetDefaultFont();
			Paint.ClearBrush();
			Paint.SetPen( Theme.TextControl );
			Paint.DrawText( r, Text, TextFlag.LeftCenter );
		}
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		Value = !Value;

		base.OnMouseClick( e );
	}

	private void AnimateCircle()
	{
		Animate.Add( this, 0.1f, Value ? 0.0f : 1.0f, Value ? 1.0f : 0.0f, x => CirclePos = x, "ease-out" );
	}
}
