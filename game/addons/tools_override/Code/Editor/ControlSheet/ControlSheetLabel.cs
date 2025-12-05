namespace Editor;

/// <summary>
/// A draggable label.
/// </summary>
class ControlSheetLabel : Widget
{
	private SerializedProperty Property { get; }
	private Drag _drag;

	public ControlSheetLabel( SerializedProperty property )
	{
		Property = property;

		MinimumHeight = Theme.RowHeight;
		MinimumWidth = 140f;
		HorizontalSizeMode = SizeMode.Flexible;

		IsDraggable = IsDraggableProperty( property );
	}

	private bool IsDraggableProperty( SerializedProperty property )
	{
		if ( property.IsMultipleValues ) return false;

		var target = property.Parent?.Targets.FirstOrDefault();

		return target is GameObject or Component or GameTransform;
	}

	protected override void OnDragStart()
	{
		base.OnDragStart();

		_drag = new Drag( this )
		{
			Data = { Object = Property, Text = Property.As.String }
		};

		_drag.Execute();
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.SetBrushAndPen( Theme.TextControl, Theme.TextControl );

		Paint.Pen = Theme.TextControl.WithAlpha( Paint.HasMouseOver ? 1.0f : 0.7f );

		var contentRect = LocalRect.Shrink( 4f );

		Paint.DrawText( contentRect, Property.DisplayName, TextFlag.LeftTop );

		if ( !IsDraggable ) return;

		var isDragging = _drag.IsValid();
		if ( isDragging )
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.Pink.WithAlpha( 0.3f ) );
			Paint.DrawRect( ContentRect, 3f );
		}
	}
}
