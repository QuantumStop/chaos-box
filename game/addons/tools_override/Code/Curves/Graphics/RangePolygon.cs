namespace Editor.GraphicsItems;

public partial class RangePolygon : GraphicsItem
{
	public EditableCurve CurveA { get; set; }
	public EditableCurve CurveB { get; set; }


	public RangePolygon( EditableCurve a, EditableCurve b ) : base( null )
	{
		CurveA = a;
		CurveB = b;
	}

	protected override void OnPaint()
	{
		Paint.SetBrushAndPen( CurveA.CurveColor.WithAlpha( 0.2f ) );

		CurveRange cr = new( CurveA.GetViewportAdjustedCurve(), CurveB.GetViewportAdjustedCurve() );
		cr.DrawArea( LocalRect, 3.0f );

		Paint.SetPen( CurveA.CurveColor.WithAlpha( 0.1f ), 1 );

		float steps = 6;

		for ( float i = 0; i <= steps; i++ )
		{
			cr.DrawLine( LocalRect, (1 / steps) + i / (steps + 2), 10 );
		}
	}

}
