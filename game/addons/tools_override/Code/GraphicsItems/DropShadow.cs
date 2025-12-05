namespace Editor;

public class DropShadow : GraphicsItem
{
	public DropShadow()
	{
		ZIndex = -1000;
	}

	protected override void OnPaint()
	{
		var rect = new Rect();
		rect.Size = Size - 10;

		Paint.ClearPen();
		Paint.SetBrush( new Color( 0, 0, 0, 0.15f ) );
		Paint.DrawRect( rect, 10.0f );
		Paint.DrawRect( rect + 5, 20.0f );

	}
}
