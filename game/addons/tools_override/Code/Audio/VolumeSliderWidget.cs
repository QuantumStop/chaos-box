namespace Editor.Audio;

public class VolumeSliderWidget : Widget
{
	SerializedProperty sp;

	float TopDb = 10;
	float BottomDb = -80;

	float DecibelsToWidget( float db ) => db.Remap( TopDb, BottomDb, 0, Height );
	float WidgetToDecibels( float y ) => (y).Remap( 0, Height, TopDb, BottomDb );

	public VolumeSliderWidget( SerializedProperty property )
	{
		sp = property;
	}

	protected override Vector2 SizeHint() => 32;

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.Antialiasing = true;

		var volume = sp.As.Float;
		var volumeDb = Helper.LinearToDecibels( volume );

		var pos = new Vector2( Width * 0.5f, DecibelsToWidget( volumeDb ) );

		var handle = new Rect( pos, new Vector2( 32, 14 ) );
		handle.Position -= handle.Size * 0.5f;
		if ( handle.Top < 0 ) handle.Position = new Vector2( 0, 0 );
		if ( handle.Bottom > Height ) handle.Position = new Vector2( 0, Height - handle.Height );

		Paint.SetBrushAndPen( Theme.ControlBackground );
		//Paint.DrawRect( handle, 4 );
		Paint.DrawPolygon( new Vector2( 2, handle.Center.y ), new Vector2( 10, handle.Top ), handle.TopRight, handle.BottomRight, new Vector2( 10, handle.Bottom ) );

		Paint.SetPen( Theme.TextControl.WithAlpha( Paint.HasMouseOver ? 0.8f : 0.3f ) );
		Paint.SetDefaultFont( 7 );
		Paint.DrawText( handle.Shrink( 5, 0 ), $"{volumeDb:0}", TextFlag.RightCenter );
	}

	bool _dragging;

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.LeftMouseButton )
		{
			_dragging = true;

			var db = WidgetToDecibels( e.LocalPosition.y );
			sp.SetValue( Helper.DecibelsToLinear( db ) );
			Update();
		}
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		_dragging = false;
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		base.OnMouseMove( e );

		if ( _dragging )
		{
			var db = WidgetToDecibels( e.LocalPosition.y );
			sp.SetValue( Helper.DecibelsToLinear( db ) );
			Update();
		}
	}
}
