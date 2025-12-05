namespace Editor.Audio;

public class VolumeTicksWidget : Widget
{
	float TopDb = 10;
	float BottomDb = -80;

	float DecibelsToWidget( float db ) => db.Remap( TopDb, BottomDb, 0, Height );

	protected override Vector2 SizeHint() => 32;

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.Antialiasing = true;

		PaintTick( 10 );
		PaintTick( 0 );
		PaintTick( -10 );
		PaintTick( -20 );
		PaintTick( -30 );
		PaintTick( -40 );
		PaintTick( -50 );
		PaintTick( -60 );
		PaintTick( -70 );
		PaintTick( -80 );
	}

	void PaintTick( float db )
	{
		var y = DecibelsToWidget( db );

		Paint.SetPen( Theme.ControlBackground, 1 );
		var r = new Rect( 0, y - 10, Width - 10, 20 );
		if ( r.Bottom > Height ) r.Bottom -= r.Bottom - Height;
		if ( r.Top < 0 ) r.Top = 0;

		if ( y < 4 ) y = 4;
		if ( y > Height - 4 ) y = Height - 4;

		Paint.SetPen( Theme.ControlBackground.WithAlpha( 0.7f ), 1 );
		Paint.DrawLine( new Vector2( Width - 4, y ), new Vector2( Width - 10, r.Center.y ) );

		Paint.DrawText( r.Shrink( 4, 0 ), $"{db:n0}", TextFlag.RightCenter );
	}
}
