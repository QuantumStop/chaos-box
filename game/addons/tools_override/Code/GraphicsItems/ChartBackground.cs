namespace Editor.GraphicsItems;

/// <summary>
/// A generic chart background. Has axis down left and along bottom.
/// </summary>
public partial class ChartBackground : GraphicsItem
{
	public Vector2 RangeX { get; set; } = new Vector2( 0, 1 );
	public Vector2 RangeY { get; set; } = new Vector2( 0, 1 );
	public Vector4 Highlight { get; set; } = new Vector4( 0, 0, 0, 0 );

	public struct AxisConfig
	{
		public Color LineColor { get; set; }
		public Color LabelColor { get; set; }
		public int Ticks { get; set; }
		public float Width { get; set; }
	}

	public AxisConfig AxisX = new AxisConfig { LineColor = Color.White.WithAlpha( 0.25f ), Ticks = 8, Width = 50.0f, LabelColor = Color.White.WithAlpha( 0.5f ) };
	public AxisConfig AxisY = new AxisConfig { LineColor = Color.White.WithAlpha( 0.25f ), Ticks = 8, Width = 20.0f, LabelColor = Color.White.WithAlpha( 0.5f ) };

	public Rect ChartRect => new Rect( Position.x + AxisX.Width, Position.y, Size.x - AxisX.Width, Size.y - AxisY.Width );

	public ChartBackground()
	{
		ZIndex = -1;
		HoverEvents = true;
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		// X axis grid lines at major increments
		{
			var range = MathF.Abs( RangeX.y - RangeX.x );
			var niceStep = GetNiceIncrement( range, AxisX.Ticks );
			var first = MathF.Ceiling( RangeX.x / niceStep ) * niceStep;
			var last = RangeX.y;

			var space = AxisX.Width;
			var size = (Size.x - space);

			for ( float v = first; v <= last + niceStep * 0.5f; v += niceStep )
			{
				float t = (v - RangeX.x) / range;
				float af = space + t * size;
				bool special = v.AlmostEqual( 0.0f );

				Paint.SetPen( AxisX.LineColor.WithAlphaMultiplied( special ? 1f : 0.3f ), 1 );
				Paint.DrawLine( new Vector2( af, 0 ), new Vector2( af, Size.y - AxisY.Width ) );

				// Draw label, skip if out of bounds
				if ( af >= space && af <= Size.x - 0.3f * size / AxisX.Ticks )
				{
					Paint.SetPen( AxisX.LabelColor.WithAlphaMultiplied( 0.5f ) );
					Paint.DrawText(
						new Rect( new Vector2( af - 100, Size.y - AxisY.Width ), new Vector2( 200, AxisY.Width ) ),
						$"{v:0.######}", TextFlag.Center );
				}
			}
		}

		// Y axis grid lines at major increments
		{
			var range = MathF.Abs( RangeY.y - RangeY.x );
			var niceStep = GetNiceIncrement( range, AxisY.Ticks );
			var first = MathF.Ceiling( RangeY.x / niceStep ) * niceStep;
			var last = RangeY.y;

			var space = AxisY.Width;
			var size = (Size.y - space);

			for ( float v = first; v <= last + niceStep * 0.5f; v += niceStep )
			{
				float t = (v - RangeY.x) / range;
				float yf = size - t * size;
				bool special = v.AlmostEqual( 0.0f );

				Paint.SetPen( AxisY.LineColor.WithAlphaMultiplied( special ? 0.4f : 0.3f ), 1 );
				Paint.DrawLine( new Vector2( AxisX.Width, yf ), new Vector2( Size.x, yf ) );

				// Draw label, skip if out of bounds
				if ( yf >= 0 && yf <= size - 0.3f * size / AxisY.Ticks )
				{
					Paint.SetPen( AxisY.LabelColor.WithAlphaMultiplied( 0.5f ) );
					Paint.DrawText(
						new Rect( new Vector2( 0, yf - 100 ), new Vector2( AxisX.Width - 4, 200 ) ),
						$"{v:0.######}", TextFlag.RightCenter );
				}
			}
		}

		// Draw Highlight Rect
		if ( Highlight.Length != 0 )
		{
			var _mappedX1 = Highlight.x.Remap( RangeX.x, RangeX.y, 0, Size.x - AxisX.Width, false ) + AxisX.Width;
			var _mappedY1 = Highlight.y.Remap( RangeY.x, RangeY.y, Size.y - AxisY.Width, 0, false );
			var _mappedX2 = Highlight.z.Remap( RangeX.x, RangeX.y, 0, Size.x - AxisX.Width, false ) + AxisX.Width;
			var _mappedY2 = Highlight.w.Remap( RangeY.x, RangeY.y, Size.y - AxisY.Width, 0, false );
			Paint.SetPen( Color.White.WithAlpha( 0.2f ), 2, PenStyle.Dash );
			Paint.SetBrush( Color.White.WithAlpha( 0.05f ) );
			Paint.DrawRect( new Rect( _mappedX1, _mappedY1, _mappedX2 - _mappedX1, _mappedY2 - _mappedY1 ) );
		}
	}

	public float GetCurrentIncrementX()
	{
		var range = MathF.Abs( RangeX.y - RangeX.x );
		return GetNiceIncrement( range, AxisX.Ticks );
	}

	public float GetCurrentIncrementY()
	{
		var range = MathF.Abs( RangeY.y - RangeY.x );
		return GetNiceIncrement( range, AxisY.Ticks );
	}

	// Returns a "nice" increment for grid lines (1, 2, 5, 10, etc.)
	private static float GetNiceIncrement( float range, int ticks )
	{
		if ( ticks < 1 ) ticks = 1;
		float roughStep = range / ticks;
		float exponent = MathF.Floor( MathF.Log10( roughStep ) );
		float fraction = roughStep / MathF.Pow( 10, exponent );

		float niceFraction;
		if ( fraction < 1.5f )
			niceFraction = 1f;
		else if ( fraction < 3f )
			niceFraction = 2f;
		else if ( fraction < 7f )
			niceFraction = 5f;
		else
			niceFraction = 10f;

		return niceFraction * MathF.Pow( 10, exponent );
	}
}

