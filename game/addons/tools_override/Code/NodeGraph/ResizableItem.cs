using System;
using System.Reflection;

namespace Editor.NodeEditor;

/// <summary>
/// Example of a resizable item for when I need it
/// </summary>
public class ResizableItem : GraphicsItem
{
	[Flags]
	private enum SizeDirection
	{
		None = 0,
		Top = 1 << 0,
		Bottom = 1 << 1,
		Left = 1 << 2,
		Right = 1 << 3
	}

	private bool _resizing;
	private Vector2 _offset;
	private Vector2 _minSize => new( 32, 32 );
	private Vector2 _maxSize => new( 10000, 10000 );
	private Vector2 _dragSize => new( 16, 16 );
	private SizeDirection _direction;

	public ResizableItem()
	{
		Movable = true;
		Selectable = true;
		HoverEvents = true;
		ZIndex = -1;
		Size = new Vector2( 256, 256 );
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.Blue.WithAlpha( 0.9f ) );
		Paint.DrawRect( LocalRect.Shrink( 4 ), 4 );
	}

	private void UpdateDirection( Vector2 position )
	{
		_direction = SizeDirection.None;

		Cursor = CursorShape.None;

		if ( position.x <= _dragSize.x )
		{
			_direction |= SizeDirection.Left;
			_offset.x = position.x;
		}
		else if ( position.x >= Size.x - _dragSize.x )
		{
			_direction |= SizeDirection.Right;
			_offset.x = position.x - Size.x;
		}

		if ( position.y <= _dragSize.y )
		{
			_direction |= SizeDirection.Top;
			_offset.y = position.y;
			Cursor = _direction.HasFlag( SizeDirection.Left ) ? CursorShape.SizeFDiag :
				_direction.HasFlag( SizeDirection.Right ) ? CursorShape.SizeBDiag : CursorShape.SizeV;
		}
		else if ( position.y >= Size.y - _dragSize.y )
		{
			_direction |= SizeDirection.Bottom;
			_offset.y = position.y - Size.y;
			Cursor = _direction.HasFlag( SizeDirection.Left ) ? CursorShape.SizeBDiag :
				_direction.HasFlag( SizeDirection.Right ) ? CursorShape.SizeFDiag : CursorShape.SizeV;
		}
		else if ( _direction.HasFlag( SizeDirection.Left ) || _direction.HasFlag( SizeDirection.Right ) )
		{
			Cursor = CursorShape.SizeH;
		}
		else
		{
			Cursor = CursorShape.None;
		}
	}

	protected override void OnMousePressed( GraphicsMouseEvent e )
	{
		if ( e.LeftMouseButton && !_resizing )
		{
			UpdateDirection( e.LocalPosition );

			if ( _direction != SizeDirection.None )
			{
				_resizing = true;
				e.Accepted = true;
			}
		}

		if ( _resizing )
		{
			e.Accepted = true;
		}

		base.OnMousePressed( e );
	}

	protected override void OnMouseReleased( GraphicsMouseEvent e )
	{
		if ( _resizing )
		{
			e.Accepted = true;

			if ( !e.LeftMouseButton )
			{
				_resizing = false;
			}
		}

		base.OnMouseReleased( e );
	}

	protected override void OnHoverMove( GraphicsHoverEvent e )
	{
		base.OnHoverMove( e );

		UpdateDirection( e.LocalPosition );
	}

	protected override void OnHoverEnter( GraphicsHoverEvent e )
	{
		base.OnHoverEnter( e );

		UpdateDirection( e.LocalPosition );
	}

	protected override void OnHoverLeave( GraphicsHoverEvent e )
	{
		base.OnHoverLeave( e );

		_direction = SizeDirection.None;
		Cursor = CursorShape.None;
	}

	protected override void OnMouseMove( GraphicsMouseEvent e )
	{
		base.OnMouseMove( e );

		if ( !_resizing )
			return;

		e.Accepted = true;

		var gridSize = GraphicsView is IGridSizeView view ? view.GridSize : 16f;

		var position = (e.ScenePosition - _offset).SnapToGrid( gridSize );
		var rect = SceneRect;

		if ( _direction.HasFlag( SizeDirection.Left ) )
			rect = ResizeLeft( rect, position.x );
		else if ( _direction.HasFlag( SizeDirection.Right ) )
			rect = ResizeRight( rect, position.x );

		if ( _direction.HasFlag( SizeDirection.Top ) )
			rect = ResizeTop( rect, position.y );
		else if ( _direction.HasFlag( SizeDirection.Bottom ) )
			rect = ResizeBottom( rect, position.y );

		SceneRect = rect;

		PrepareGeometryChange();
		Update();
	}

	protected override void OnPositionChanged()
	{
		Position = Position.SnapToGrid( 16.0f );
	}

	private Rect ResizeTop( Rect rect, float position )
	{
		rect.Top = position;
		var size = rect.Bottom - rect.Top;
		size -= size.Clamp( _minSize.y, _maxSize.y );
		rect.Top += size;
		return rect;
	}

	private Rect ResizeLeft( Rect rect, float position )
	{
		rect.Left = position;
		var size = rect.Right - rect.Left;
		size -= size.Clamp( _minSize.x, _maxSize.x );
		rect.Left += size;
		return rect;
	}

	private Rect ResizeBottom( Rect rect, float position )
	{
		rect.Bottom = position;
		var size = rect.Bottom - rect.Top;
		size -= size.Clamp( _minSize.y, _maxSize.y );
		rect.Bottom -= size;
		return rect;
	}

	private Rect ResizeRight( Rect rect, float position )
	{
		rect.Right = position;
		var size = rect.Right - rect.Left;
		size -= size.Clamp( _minSize.x, _maxSize.x );
		rect.Right -= size;
		return rect;
	}
}
