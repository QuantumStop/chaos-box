using System;

namespace Editor.Widgets;

public class VideoGallery : Widget
{
	public VideoGallery( Widget parent ) : base( parent )
	{
		Layout = Layout.Grid();
		Layout.Spacing = 0;

		if ( Layout is GridLayout gridLayout )
		{
			gridLayout.AddCell( 0, 0, new VideoWidget( this, "https://files.facepunch.com/layla/1b0511b1/homelander_TRH_sfm_test_v008_720p.mp4" ), 1 );
		}
	}

	[WidgetGallery]
	[Title( "VideoPlayer" )]
	[Icon( "movie" )]
	internal static Widget WidgetGallery()
	{
		var canvas = new VideoGallery( null );
		return canvas;
	}
}

/// <summary>
/// A widget that uses a pixmap to display a video.
/// </summary>
public class VideoWidget : Widget
{
	/// <summary>
	/// Access to the video player to control playback.
	/// </summary>
	public VideoPlayer Player { get; private set; }

	private Pixmap background;

	public VideoWidget( Widget parent, string url ) : base( parent )
	{
		MinimumSize = 50;

		Player = new VideoPlayer
		{
			Repeat = true,
			OnTextureData = OnTextureData
		};

		if ( !string.IsNullOrWhiteSpace( url ) )
		{
			Player.Play( url );
		}
	}

	private void OnTextureData( ReadOnlySpan<byte> span, Vector2 size )
	{
		if ( background == null || background.Size != size )
		{
			background = new Pixmap( size );
		}

		background.UpdateFromPixels( span, size, ImageFormat.RGBA8888 );
		Update();
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Color.Black );
		Paint.DrawRect( LocalRect );

		if ( background == null )
			return;

		var textureSize = background.Size;
		var viewportSize = Size;
		var scaleW = viewportSize.x / textureSize.x;
		var scaleH = viewportSize.y / textureSize.y;
		var scale = Math.Min( scaleW, scaleH );
		var newSize = new Vector2( textureSize.x * scale, textureSize.y * scale );
		var rect = new Rect( (viewportSize.x - newSize.x) / 2, (viewportSize.y - newSize.y) / 2, newSize.x, newSize.y );

		Paint.Draw( rect, background );
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		Player?.Dispose();
		Player = null;
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		Player?.Present();
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		if ( e.LeftMouseButton )
		{
			Player?.TogglePause();
		}
	}
}
