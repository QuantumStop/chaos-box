namespace Editor;

public static class CurveExtensions
{
	static List<Vector2> pointCache = new List<Vector2>();

	/// <summary>
	/// Draw curve. start and end should be specified in normalized time space, ie 0-1, not range.x-range.y
	/// </summary>
	public static void DrawLine( this in Curve curve, in Rect rect, float spacing = 3.0f, float start = 0.0f, float end = 1.0f )
	{
		pointCache.Clear();

		var step = (1.0f / (rect.Width / spacing));
		for ( float f = 0; f < 1.0f + step * 0.1f; f += step )
		{
			var v = curve.EvaluateDelta( start.LerpTo( end, f ) );
			pointCache.Add( new Vector2( rect.Left + f * rect.Width, rect.Top + rect.Height - (v * rect.Height) ) );
		}

		Paint.DrawLine( pointCache );
		pointCache.Clear();
	}

	/// <summary>
	/// Draw curve. start and end should be specified in normalized time space, ie 0-1, not range.x-range.y
	/// </summary>
	public static void DrawArea( this in CurveRange curve, in Rect rect, float spacing = 3.0f, float start = 0.0f, float end = 1.0f )
	{
		pointCache.Clear();

		var step = (1.0f / (rect.Width / spacing));
		for ( float f = 0; f < 1.0f + step * 0.1f; f += step )
		{
			var v = curve.EvaluateDelta( start.LerpTo( end, f ), 1 );
			pointCache.Add( new Vector2( rect.Left + f * rect.Width, rect.Top + rect.Height - (v * rect.Height) ) );
		}

		for ( float f = 1.0f; f > 0; f -= step )
		{
			var v = curve.EvaluateDelta( start.LerpTo( end, f ), 0 );
			pointCache.Add( new Vector2( rect.Left + f * rect.Width, rect.Top + rect.Height - (v * rect.Height) ) );
		}

		Paint.DrawPolygon( pointCache );
		pointCache.Clear();
	}

	/// <summary>
	/// Draw curve. start and end should be specified in normalized time space, ie 0-1, not range.x-range.y
	/// </summary>
	public static void DrawLine( this in CurveRange curve, in Rect rect, float y, float spacing = 3.0f, float start = 0.0f, float end = 1.0f )
	{
		pointCache.Clear();

		var step = (1.0f / (rect.Width / spacing));
		for ( float f = 0; f < 1.0f + step * 0.1f; f += step )
		{
			var v = curve.EvaluateDelta( start.LerpTo( end, f ), y );
			pointCache.Add( new Vector2( rect.Left + f * rect.Width, rect.Top + rect.Height - (v * rect.Height) ) );
		}

		Paint.DrawLine( pointCache );
		pointCache.Clear();
	}


}
