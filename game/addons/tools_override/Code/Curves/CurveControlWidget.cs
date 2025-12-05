using Sandbox.Diagnostics;

namespace Editor;

[CustomEditor( typeof( Curve ) )]
public class CurveControlWidget : ControlWidget
{
	public Color HighlightColor = Theme.Yellow;

	public override bool SupportsMultiEdit => true;

	public CurveControlWidget( SerializedProperty property ) : base( property )
	{
		Cursor = CursorShape.Finger;
	}

	protected override void PaintOver()
	{
		var value = SerializedProperty.GetValue<Curve>();
		var col = HighlightColor.WithAlpha( Paint.HasMouseOver ? 1 : 0.5f );
		var inner = LocalRect.Shrink( 4.0f );

		Paint.SetPen( col.WithAlphaMultiplied( 0.1f ), 20 );
		value.DrawLine( inner, 20.0f );

		Paint.SetPen( col.WithAlphaMultiplied( 0.1f ), 10 );
		value.DrawLine( inner, 10.0f );

		Paint.SetPen( col.WithAlphaMultiplied( 0.1f ), 2 );
		value.DrawLine( inner, 6.0f );

		Paint.SetPen( col, 1 );
		value.DrawLine( inner, 3.0f );

		Paint.SetBrushAndPen( Color.Transparent, Theme.ControlBackground, 2 );
		Paint.DrawRect( LocalRect.Shrink( 1 ), 3 );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.LeftMouseButton )
		{
			// open curve editor
			var editor = new CurveEditorPopup( this );

			editor.AddCurve( SerializedProperty, Update );
			editor.AddPresets( SerializedProperty );

			editor.Position = e.ScreenPosition - editor.Size * new Vector2( 1, 0.5f );
			editor.MinimumSize = new Vector2( 800, 600 );
			editor.WindowTitle = $"Curve";
			editor.Visible = true;

			// Clamp position within the screen bounds
			editor.ConstrainToScreen();
		}
	}
}

[CustomEditor( typeof( CurveRange ) )]
public class CurveRangeControlWidget : ControlWidget
{
	public Color HighlightColor = Theme.Yellow;

	public CurveRangeControlWidget( SerializedProperty property ) : base( property )
	{
		Cursor = CursorShape.Finger;
	}

	protected override void PaintOver()
	{
		var value = SerializedProperty.GetValue<CurveRange>();
		var col = HighlightColor.WithAlpha( Paint.HasMouseOver ? 1 : 0.5f );
		var inner = LocalRect.Shrink( 4.0f );

		Paint.SetBrushAndPen( col );

		value.DrawArea( inner, 3.0f );

		Paint.SetBrushAndPen( Color.Transparent, Theme.ControlBackground, 2 );
		Paint.DrawRect( LocalRect.Shrink( 1 ), 3 );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.LeftMouseButton )
		{
			SerializedProperty.TryGetAsObject( out var obj );
			Assert.NotNull( obj, "CurveRange object was null" );

			var editor = new CurveEditorPopup( this );
			editor.Visible = true;
			editor.Position = e.ScreenPosition - editor.Size * new Vector2( 1, 0.5f );
			editor.ConstrainToScreen();

			{
				var prop = obj.GetProperty( "A" );
				Assert.NotNull( prop, "CurveRange.A property was null" );
				editor.AddCurve( prop, Update );
			}

			{
				var prop = obj.GetProperty( "B" );
				Assert.NotNull( prop, "CurveRange.A property was null" );
				editor.AddCurve( prop, Update );
			}

			editor.SetIsRange();
		}
	}
}
