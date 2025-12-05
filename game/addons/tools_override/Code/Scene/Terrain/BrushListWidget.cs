using System;

namespace Editor.TerrainEditor;

class BrushListWidget : Widget
{
	public Action BrushSelected { get; set; }

	public BrushListWidget() : base( null )
	{
		ListView list = new();
		// list.MaximumHeight = 96;
		list.SetItems( TerrainEditorTool.BrushList.Brushes.Cast<object>() );
		list.ItemSize = new Vector2( 40, 40 );
		list.ItemAlign = Sandbox.UI.Align.SpaceBetween;
		list.OnPaintOverride += () => PaintListBackground( list );
		list.ItemPaint = PaintBrushItem;
		list.ItemSelected = ( item ) => { if ( item is Brush brush ) { SelectBrush( brush ); } };
		list.SelectItem( TerrainEditorTool.BrushList.Selected );

		Layout = Layout.Column();

		var label = new Label( "Brushes" );
		label.SetStyles( "font-weight: bold" );
		Layout.Add( label );
		Layout.AddSpacingCell( 8 );
		Layout.Add( list );
	}

	private void SelectBrush( Brush brush )
	{
		TerrainEditorTool.BrushList.Selected = brush;
		BrushSelected?.Invoke();
	}

	private void PaintBrushItem( VirtualWidget widget )
	{
		var brush = (Brush)widget.Object;

		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		if ( widget.Hovered || widget.Selected )
		{
			Paint.ClearPen();
			Paint.SetBrush( widget.Selected ? Theme.Primary : Color.White.WithAlpha( 0.1f ) );
			Paint.DrawRect( widget.Rect.Grow( 2 ), 3 );
		}

		Paint.Draw( widget.Rect, brush.Pixmap );
	}

	private bool PaintListBackground( Widget widget )
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( widget.LocalRect );

		return false;
	}
}
