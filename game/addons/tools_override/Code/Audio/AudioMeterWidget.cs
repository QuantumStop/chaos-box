using Sandbox.Audio;

namespace Editor.Audio;

/// <summary>
/// Shows left and right volume bars, with a peak indicator.
/// </summary>
public class AudioMeterWidget : Widget
{
	public float Left { get; private set; }
	public float Right { get; private set; }


	public AudioMeterWidget()
	{
		HorizontalSizeMode = SizeMode.Expand;
	}

	protected override Vector2 SizeHint() => new( 32, 200 );

	float TopDb = 10;
	float BottomDb = -80;

	float DecibelsToWidget( float db ) => db.Remap( TopDb, BottomDb, 0, Height );

	protected override void OnPaint()
	{
		var bg = Theme.Green.Darken( 0.85f );
		bg = Theme.ControlBackground;

		var ll = Left;
		var lr = Right;

		ll = Helper.LinearToDecibels( ll );
		lr = Helper.LinearToDecibels( lr );

		Paint.Antialiasing = true;

		Paint.SetBrushAndPen( bg.WithAlpha( 0.35f ) );
		Paint.DrawRect( LocalRect.Shrink( 1 ), 4 );

		var inner = LocalRect.Shrink( 1 );
		var gap = 2;
		var w = (inner.Width / 2.0f) - gap / 2.0f;


		float danger = DecibelsToWidget( -20 );

		{
			var h = inner.Height - DecibelsToWidget( ll );
			if ( h > 0 )
			{
				var r = new Rect( inner.Left, inner.Top + inner.Height - h, w, h );
				Paint.ClearPen();
				Paint.SetBrushLinear( new Vector2( 0, 0 ), new Vector2( 0, danger ), Theme.Red, Theme.Green );
				Paint.DrawRect( r, 0 );
			}

			if ( historyFrames.Count > 0 )
			{
				var peak = historyFrames.Max( x => x.MaxLevelLeft );
				if ( peak > 0 )
				{
					float zero = DecibelsToWidget( Helper.LinearToDecibels( peak ) );
					Paint.SetBrushAndPen( Color.Transparent, Color.Yellow.WithAlpha( 0.7f ), 1 );
					Paint.DrawLine( new Vector2( inner.Left, zero ), new Vector2( inner.Left + w, zero ) );
				}
			}
		}

		{
			var h = inner.Height - DecibelsToWidget( lr );
			if ( h > 0 )
			{
				var r = new Rect( inner.Left + w + gap, inner.Top + inner.Height - h, w, h );
				Paint.ClearPen();
				Paint.SetBrushLinear( new Vector2( 0, 0 ), new Vector2( 0, danger ), Theme.Red, Theme.Green );
				Paint.DrawRect( r, 0 );
			}

			if ( historyFrames.Count > 0 )
			{
				var peak = historyFrames.Max( x => x.MaxLevelRight );
				if ( peak > 0 )
				{
					float zero = DecibelsToWidget( Helper.LinearToDecibels( peak ) );
					Paint.SetBrushAndPen( Color.Transparent, Color.Yellow.WithAlpha( 0.7f ), 1 );
					Paint.DrawLine( new Vector2( inner.Left + w + gap, zero ), new Vector2( inner.Left + w + gap + w, zero ) );
				}
			}
		}

		for ( float i = BottomDb; i <= TopDb; i += 2 )
		{
			bool sig = i % 10 == 0;

			float zero = DecibelsToWidget( i );
			Paint.SetBrushAndPen( Color.Transparent, bg.WithAlpha( sig ? 0.3f : 0.1f ), 1 );

			if ( i == 0 ) Paint.Pen = bg;

			Paint.DrawLine( new Vector2( 0, zero ), new Vector2( Width, zero ) );
		}

		Paint.SetBrushAndPen( Color.Transparent, bg, 1 );
		Paint.DrawRect( LocalRect.Shrink( 1 ), 2 );

		Paint.DrawLine( new Vector2( Width * 0.5f, 0 ), new Vector2( Width * 0.5f, Height ) );
	}

	RealTimeSince timeSinceUpdate = 0;

	List<AudioMeter.Frame> historyFrames = new List<AudioMeter.Frame>();

	internal void UpdateValues( AudioMeter meter )
	{
		if ( timeSinceUpdate < 1 / 60.0f )
			return;

		timeSinceUpdate = 0;

		Left = meter.Current.MaxLevelLeft;
		Right = meter.Current.MaxLevelRight;

		historyFrames.Add( meter.Current );

		if ( historyFrames.Count > 60 )
			historyFrames.RemoveAt( 0 );

		Update();
	}
}
