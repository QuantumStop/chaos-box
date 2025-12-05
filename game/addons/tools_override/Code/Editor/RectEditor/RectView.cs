
namespace Editor.RectEditor;

public enum DragState
{
	None,
	WaitingForMovement,
	Dragging,
}

enum GridSnapMode
{
	Nearest,
	RoundDown,
	RoundUp,
}

public class RectView : Widget
{
	private readonly Window Session;
	private Document Document => Session.Document;

	private Rect DrawRect;
	internal Pixmap SourceImage;
	private Pixmap ScaledImage;
	private DragState DragState;
	private Vector2 DragStartPos;
	private Vector2 HoveredCorner;
	private Rect NewRect;
	private List<Document.Rectangle> RectanglesUnderCursor;
	private HashSet<Document.Rectangle> DraggingRectangles = new();

	public RectView( Window session ) : base( session )
	{
		Session = session;

		Name = "Rect View";
		WindowTitle = "Rect View";
		SetWindowIcon( "space_dashboard" );

		MouseTracking = true;
		FocusMode = FocusMode.Click;

		DrawRect = GetDrawRect();
	}

	private Vector2 SnapUVToGrid( Vector2 uv )
	{
		var gridCountX = GetGridCountX();
		var gridCountY = GetGridCountY();

		var x = (int)(gridCountX * uv.x + 0.5f);
		var y = (int)(gridCountY * uv.y + 0.5f);

		return new Vector2( x / (float)gridCountX, y / (float)gridCountY );
	}

	private Vector2 PixelToUV_OnGrid( Vector2 vPixel )
	{
		return SnapUVToGrid( PixelToUV( vPixel ) );
	}

	private void DragCreateRect( Vector2 mousePos )
	{
		var minStart = PixelToUV_OnGrid( DragStartPos );
		var maxStart = PixelToUV_OnGrid( DragStartPos );
		var current = PixelToUV_OnGrid( mousePos );
		var min = Vector2.Min( current, minStart );
		var max = Vector2.Max( current, maxStart );

		NewRect = new Rect( min, max - min );

		Update();
	}

	private void DragMoveRect( Vector2 mousePos )
	{
		var start = PixelToUV_OnGrid( DragStartPos );
		var end = PixelToUV_OnGrid( mousePos );
		var diff = end - start;
		if ( diff.x == 0 && diff.y == 0 )
			return;

		foreach ( var rectangle in DraggingRectangles )
		{
			rectangle.Min += diff;
			rectangle.Max += diff;
		}
		DragStartPos = mousePos;

		Document.Modified = true;
		Document.OnModified?.Invoke();
		Update();
	}

	private void DragResizeRect( Vector2 mousePos )
	{
		var start = PixelToUV_OnGrid( DragStartPos );
		var end = PixelToUV_OnGrid( mousePos );
		var diff = end - start;
		if ( diff.x == 0 && diff.y == 0 )
			return;

		foreach ( var rectangle in DraggingRectangles )
		{
			if ( HoveredCorner.x < 0 )
			{
				rectangle.Min += diff.WithY( 0 );
			}
			else if ( HoveredCorner.x > 0 )
			{
				rectangle.Max += diff.WithY( 0 );
			}

			if ( HoveredCorner.y < 0 )
			{
				rectangle.Min += diff.WithX( 0 );
			}
			else if ( HoveredCorner.y > 0 )
			{
				rectangle.Max += diff.WithX( 0 );
			}
		}
		DragStartPos = mousePos;

		Document.Modified = true;
		Document.OnModified?.Invoke();
		Update();
	}

	[EditorEvent.Frame]
	protected void OnFrame()
	{
		var mousePos = FromScreen( Application.CursorPosition );
		FindRectanglesUnderCursor( mousePos );
		FindHoveredCorner( mousePos );
		SetCursorFromState();
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		base.OnMouseMove( e );

		if ( DragState == DragState.WaitingForMovement )
		{
			if ( DraggingRectangles.Count == 1 )
			{
				Document.SelectRectangle( DraggingRectangles.First(), SelectionOperation.Set );
			}
			DragState = DragState.Dragging;
		}

		if ( DragState != DragState.None )
		{
			if ( DraggingRectangles.Count > 0 )
			{
				if ( HoveredCorner == 0 || Document.SelectedRectangles.Count > 1 )
				{
					DragMoveRect( e.LocalPosition );
				}
				else
				{
					DragResizeRect( e.LocalPosition );
				}
			}
			else
			{
				DragCreateRect( e.LocalPosition );
			}
		}
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.Button == MouseButtons.Left )
		{
			DragState = DragState.WaitingForMovement;
			DragStartPos = e.LocalPosition;
			DraggingRectangles.Clear();

			var rectUnderCursor = GetFirstRectangleUnderCursor();
			if ( rectUnderCursor is not null )
			{
				if ( Document.SelectedRectangles.Contains( rectUnderCursor ) )
				{
					// Drag all selected rectangles if the rectangle under cursor is selected already
					DraggingRectangles = Document.SelectedRectangles.ToHashSet();
				}
				else
				{
					// Just drag the rectangle under cursor if it is not selected, and then we'll select it afterwards
					DraggingRectangles = [rectUnderCursor];
				}
			}
		}
		else if ( e.Button == MouseButtons.Right && RectanglesUnderCursor.Count > 0 )
		{
			CreateContextMenu( GetFirstRectangleUnderCursor() );
		}
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		if ( e.Button == MouseButtons.Left )
		{
			var operation = (e.HasShift || e.HasCtrl) ? SelectionOperation.Add : SelectionOperation.Set;
			if ( DragState == DragState.Dragging )
			{
				if ( Document is not null && NewRect.Width > 0.0f && NewRect.Height > 0.0f && DraggingRectangles.Count == 0 )
				{
					Session.ExecuteUndoableAction( "Create Rectangle", () => Document.SelectRectangle( Document.AddRectangle( Session, NewRect ), operation ) );
				}
			}
			else if ( DraggingRectangles.Count == 0 || DragState == DragState.WaitingForMovement )
			{
				Session.ExecuteUndoableAction( "Select Rectangle", () => Document.SelectRectangle( GetFirstRectangleUnderCursor(), operation ) );
			}

			DragState = DragState.None;
			NewRect = default;
		}
	}

	private void CreateContextMenu( Document.Rectangle rectangle )
	{
		var m = new ContextMenu( this );

		m.AddOption( "Delete Rectangle", "delete", () => Session.ExecuteUndoableAction( "Delete Rectangle", () => Document.DeleteRectangles( [rectangle] ) ) );

		m.OpenAtCursor();
	}

	public void SetMaterial( Material material )
	{
		SourceImage = null;
		ScaledImage = null;

		if ( material is null )
			return;

		var texture = material.FirstTexture;
		if ( texture is null )
			return;

		SourceImage = Pixmap.FromTexture( texture, false );
		if ( SourceImage is null )
			return;

		UpdateScaledBackgroundImage();
	}

	private void UpdateScaledBackgroundImage()
	{
		ScaledImage = SourceImage?.Resize( DrawRect.Size );
	}

	protected override void OnResize()
	{
		base.OnResize();

		DrawRect = GetDrawRect();

		UpdateScaledBackgroundImage();
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();

		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect );

		Paint.SetBrush( Color.Gray );
		Paint.DrawRect( DrawRect );

		if ( ScaledImage is not null )
		{
			Paint.Draw( DrawRect, ScaledImage );
		}

		if ( Session.GridEnabled )
		{
			DrawGrid();
		}

		DrawRectangleSet( Document?.Rectangles );

		if ( DragState == DragState.Dragging )
		{
			var topLeft = UVToPixel( NewRect.TopLeft );
			var bottomRight = UVToPixel( NewRect.BottomRight );
			var newRect = new Rect( topLeft, bottomRight - topLeft );
			Paint.ClearBrush();
			Paint.SetPen( Color.Yellow, 3 );
			Paint.DrawRect( newRect );
		}
	}

	private Vector2 UVToPixel( Vector2 uv )
	{
		return new Vector2( (int)((uv.x * DrawRect.Width) + DrawRect.Left), (int)((uv.y * DrawRect.Height) + DrawRect.Top) );
	}

	private Vector2 PixelToUV( Vector2 pixel )
	{
		return new Vector2( (pixel.x - DrawRect.Left) / DrawRect.Width, (pixel.y - DrawRect.Top) / DrawRect.Height );
	}

	private int GetGridCountX()
	{
		var width = SourceImage is null ? 512 : System.Math.Max( (int)SourceImage.Width, 1 );
		var gridSize = Session.Settings.GridSize;
		return width / gridSize;
	}

	private int GetGridCountY()
	{
		var height = SourceImage is null ? 512 : System.Math.Max( (int)SourceImage.Height, 1 );
		var gridSize = Session.Settings.GridSize;
		return height / gridSize;
	}

	public Document.Rectangle GetFirstRectangleUnderCursor()
	{
		return RectanglesUnderCursor?.FirstOrDefault();
	}

	void FindRectanglesUnderCursor( Vector2 mousePos )
	{
		RectanglesUnderCursor = FindRectanglesContainingPoint( PixelToUV( mousePos ) );

		Update();
	}

	void FindHoveredCorner( Vector2 mousePos )
	{
		if ( DragState != DragState.None )
			return;

		HoveredCorner = 0;
		var first = RectanglesUnderCursor.FirstOrDefault();
		if ( first is not null )
		{
			HoveredCorner = GetHoveredCornerForRectangle( first, PixelToUV( mousePos ) );
		}
	}
	void SetCursorFromState()
	{
		bool canResize = Document.SelectedRectangles.Count < 2 && HoveredCorner != 0;
		var rectUnderCursor = GetFirstRectangleUnderCursor();

		if ( canResize )
		{
			if ( HoveredCorner.x != 0 && HoveredCorner.y != 0 )
			{
				Cursor = (HoveredCorner.x == HoveredCorner.y) ? CursorShape.SizeFDiag : CursorShape.SizeBDiag;
			}
			else if ( HoveredCorner.x != 0 )
			{
				Cursor = CursorShape.SizeH;
			}
			else if ( HoveredCorner.y != 0 )
			{
				Cursor = CursorShape.SizeV;
			}
		}
		else if ( DragState == DragState.Dragging && DraggingRectangles.Count > 0 )
		{
			Cursor = CursorShape.ClosedHand;
		}
		else if ( DragState != DragState.Dragging )
		{
			if ( rectUnderCursor is not null )
			{
				Cursor = Document.SelectedRectangles.Contains( rectUnderCursor ) ? CursorShape.OpenHand : CursorShape.Finger;
			}
			else
			{
				Cursor = CursorShape.Arrow;
			}
		}
		else
		{
			Cursor = CursorShape.Cross;
		}
	}

	public List<Document.Rectangle> FindRectanglesContainingPoint( Vector2 vPoint )
	{
		return Document.Rectangles
			 .Where( rectangle => rectangle.IsPointInRectangle( vPoint ) )
			 .Select( rectangle => new { Rectangle = rectangle, Distance = rectangle.DistanceFromPointToCenter( vPoint ) } )
			 .OrderBy( item => item.Distance )
			 .Select( item => item.Rectangle )
			 .ToList();
	}

	public Vector2 GetHoveredCornerForRectangle( Document.Rectangle rectangle, Vector2 position )
	{
		var vec = Vector2.Zero;
		var tolerance = 0.02f;

		if ( MathF.Abs( position.x - rectangle.Min.x ) < tolerance )
		{
			vec += new Vector2( -1, 0 );
		}
		else if ( MathF.Abs( position.x - rectangle.Max.x ) < tolerance )
		{
			vec += new Vector2( 1, 0 );
		}

		if ( MathF.Abs( position.y - rectangle.Min.y ) < tolerance )
		{
			vec += new Vector2( 0, -1 );
		}
		else if ( MathF.Abs( position.y - rectangle.Max.y ) < tolerance )
		{
			vec += new Vector2( 0, 1 );
		}

		return vec;
	}

	private void DrawRectangleSet( IEnumerable<Document.Rectangle> rectangles )
	{
		if ( rectangles is null )
			return;

		var rectangleUnderCursor = GetFirstRectangleUnderCursor();
		foreach ( var rectangle in rectangles.Where( x => !Document.IsRectangleSelected( x ) ) )
		{
			Paint.SetBrush( rectangle.Color.WithAlpha( 0.5f ) );
			Paint.SetPen( Color.Black.WithAlpha( 192 / 255.0f ), 1 );
			DrawRectangle( rectangle );
		}

		foreach ( var rectangle in Document.SelectedRectangles )
		{
			Paint.SetBrush( new Color32( 255, 255, 0, 64 ) );
			Paint.SetPen( new Color32( 255, 255, 0 ), 1 );
			DrawRectangle( rectangle, corner: (rectangle == rectangleUnderCursor && Document.SelectedRectangles.Count < 2) ? HoveredCorner : 0 );
		}

		if ( rectangleUnderCursor is not null && !Document.IsRectangleSelected( rectangleUnderCursor ) )
		{
			Paint.SetBrush( new Color32( 0, 255, 0, 64 ) );
			Paint.SetPen( Color.Green );
			DrawRectangle( rectangleUnderCursor, corner: HoveredCorner );
		}
	}

	private void DrawRectangle( Document.Rectangle rectangle, int nMinInset = 0, int nMaxInset = 0, Vector2 corner = default )
	{
		if ( rectangle is null )
			return;

		var minPoint = UVToPixel( rectangle.Min );
		var maxPoint = UVToPixel( rectangle.Max );
		minPoint += new Vector2( nMinInset, nMinInset );
		maxPoint -= new Vector2( nMaxInset + 1, nMaxInset + 1 );

		Paint.DrawRect( new Rect( minPoint, maxPoint - minPoint ) );

		// Draw anchor lines if needed
		if ( corner != 0 )
		{
			var penColor = Paint.Pen;
			Paint.SetPen( Color.Orange, 2 );
			if ( corner.x < 0 )
			{
				Paint.DrawLine( minPoint + Vector2.Right + Vector2.Down, minPoint.WithY( maxPoint.y ) + Vector2.Right );
			}
			else if ( corner.x > 0 )
			{
				Paint.DrawLine( maxPoint.WithY( minPoint.y ) + Vector2.Down, maxPoint );
			}
			if ( corner.y < 0 )
			{
				Paint.DrawLine( minPoint + Vector2.Down + Vector2.Right * 2, minPoint.WithX( maxPoint.x ) + Vector2.Down );
			}
			else if ( corner.y > 0 )
			{
				Paint.DrawLine( minPoint.WithY( maxPoint.y ) + Vector2.Right, maxPoint );
			}
			Paint.SetPen( penColor );
		}
	}

	private void DrawGrid()
	{
		const float gridOpacity = 64 / 255.0f;

		var gridCountX = GetGridCountX();
		var gridCountY = GetGridCountY();

		var stepX = 1.0f / gridCountX;
		var stepY = 1.0f / gridCountY;

		var rect = DrawRect;

		Paint.ClearBrush();

		for ( int ix = 0; ix <= gridCountX; ++ix )
		{
			var u = ix * stepX;
			var gx = UVToPixel( new Vector2( u, 0 ) ).x;

			if ( gx > rect.Left )
			{
				Paint.SetPen( new Color( 1, 1, 1, gridOpacity ) );
				Paint.DrawLine( new Vector2( gx - 1, rect.Top + 1 ), new Vector2( gx - 1, rect.Height + rect.Top - 2 ) );
			}

			if ( gx < (rect.Left + rect.Width) )
			{
				Paint.SetPen( new Color( 0, 0, 0, gridOpacity ) );
				Paint.DrawLine( new Vector2( gx, rect.Top + 1 ), new Vector2( gx, rect.Height + rect.Top - 2 ) );
			}
		}

		for ( int iy = 0; iy <= gridCountY; ++iy )
		{
			var v = iy * stepY;
			var gy = UVToPixel( new Vector2( 0, v ) ).y;

			if ( gy > rect.Top )
			{
				Paint.SetPen( new Color( 1, 1, 1, gridOpacity ) );

				if ( gy == (rect.Top + rect.Height) )
				{
					Paint.DrawLine( new Vector2( rect.Left, gy - 1 ), new Vector2( rect.Width + rect.Left - 1, gy - 1 ) );
				}
				else
				{
					Paint.DrawLine( new Vector2( rect.Left + 1, gy - 1 ), new Vector2( rect.Width + rect.Left - 2, gy - 1 ) );
				}

			}

			if ( gy < (rect.Top + rect.Height) )
			{
				Paint.SetPen( new Color( 0, 0, 0, gridOpacity ) );

				if ( gy == rect.Top )
				{
					Paint.DrawLine( new Vector2( rect.Left, gy ), new Vector2( rect.Width + rect.Left - 1, gy ) );
				}
				else
				{
					Paint.DrawLine( new Vector2( rect.Left + 1, gy ), new Vector2( rect.Width + rect.Left - 2, gy ) );
				}
			}
		}
	}

	private Rect GetDrawRect()
	{
		const int marigin = 16;
		const int drawSnapSize = 4;

		var imageSize = SourceImage is null ? 0 : SourceImage.Size;
		var widgetWidth = System.Math.Max( (int)Width - (marigin * 2), 128 );
		var widgetHeight = System.Math.Max( (int)Height - (marigin * 2), 128 );
		var imageWidth = System.Math.Max( (int)imageSize.x, 1 );
		var imageHeight = System.Math.Max( (int)imageSize.y, 1 );

		int drawWidth;
		int drawHeight;

		if ( (imageWidth > 0) && (imageHeight > 0) )
		{
			var aspect = imageWidth / (float)imageHeight;
			var relativeWidth = (int)(widgetWidth / System.MathF.Max( aspect, 1.0f ));
			var relativeHeight = (int)(widgetHeight * System.MathF.Min( aspect, 1.0f ));

			if ( relativeWidth <= relativeHeight )
			{
				drawWidth = widgetWidth;
				drawHeight = widgetWidth * imageHeight / imageWidth;
			}
			else
			{
				drawHeight = widgetHeight;
				drawWidth = widgetHeight * imageWidth / imageHeight;
			}
		}
		else
		{
			var drawSize = System.Math.Min( widgetWidth, widgetHeight );
			drawHeight = drawSize;
			drawWidth = drawSize;
		}

		drawWidth = drawWidth / drawSnapSize * drawSnapSize;
		drawHeight = drawHeight / drawSnapSize * drawSnapSize;

		return new Rect( marigin, marigin, drawWidth, drawHeight );
	}
}
