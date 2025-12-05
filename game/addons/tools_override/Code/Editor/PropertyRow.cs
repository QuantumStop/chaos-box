using Sandbox.UI;

namespace Editor;

public class PropertyRow : Widget
{
	DisplayInfo Info;

	int LabelWidth = 150;
	public bool Errors { set; get; } = false;

	public PropertyRow( Widget parent ) : base( parent )
	{
		Layout = Layout.Row();
		Layout.Margin = new( LabelWidth, 2, 8, 2 );
		MinimumSize = 23;
		SetSizeMode( SizeMode.Default, SizeMode.Expand );
	}

	public void SetLabel( string text )
	{
		Info.Name = text;
	}

	public void SetLabel( DisplayInfo info )
	{
		Info = info;
	}

	public T SetWidget<T>( T w ) where T : Widget
	{
		Layout.Add( w, 1 );

		if ( Info.Placeholder != null && w is LineEdit e )
		{
			e.PlaceholderText = Info.Placeholder;
		}

		return w;
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		if ( string.IsNullOrEmpty( Info.Name ) )
			return;

		var size = LocalRect;
		size.Width = LabelWidth - 16;

		if ( size.Height > 28 )
			size.Height = 28;

		size.Left += 11;
		Paint.SetDefaultFont();
		Paint.SetPen( Errors ? Theme.Red.Lighten( 0.3f ) : Theme.Border.Lighten( 0.3f ) );
		Paint.DrawText( size, Info.Name, TextFlag.LeftCenter );
	}
}

public class PropertyRowError : Widget
{
	public Label Label;

	public PropertyRowError( string title = null, Widget parent = null ) : base( parent )
	{
		Layout = Layout.Column();

		Label = new Label( title, this );
		Label.WordWrap = true;
		Label.Alignment = TextFlag.LeftTop;

		Layout.Add( Label );
		Layout.Margin = new Margin( 16, 16, 16, 16 );
	}

	void DrawBoxWithTriangle( Rect rect )
	{
		var triOffset = 24;
		var triWidth = 16;
		var triHeight = 8;

		List<Vector2> points = new();
		points.Add( rect.TopLeft );
		points.Add( rect.TopLeft + new Vector2( triOffset, 0 ) );
		points.Add( rect.TopLeft + new Vector2( triOffset + triWidth / 2, -triHeight ) );
		points.Add( rect.TopLeft + new Vector2( triOffset + triWidth, 0 ) );
		points.Add( rect.TopRight - new Vector2( 1, 0 ) );
		points.Add( rect.BottomRight - new Vector2( 1, 0 ) );
		points.Add( rect.BottomLeft );

		Paint.DrawPolygon( points );
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.SetPen( Theme.Red.WithAlpha( 0.8f ), 1 );
		Paint.SetBrush( Theme.Red.WithAlpha( 0.2f ) );

		var headerSize = 8;

		var bodyRect = new Rect( 0, headerSize, Width, Height - headerSize );
		bodyRect.Bottom -= 8;

		DrawBoxWithTriangle( bodyRect );
	}
}
