using Editor.Assets;

namespace Editor;


[AssetPreview( "sound" )]
class PreviewSound : AssetPreview
{
	public PreviewSound( Asset asset ) : base( asset ) { }

	SoundPreviewWidget previewWidget;

	public override Widget CreateWidget( Widget parent )
	{
		previewWidget = new SoundPreviewWidget( parent );
		previewWidget.Asset = Asset;
		previewWidget.PlayAtWorld( Vector2.Zero );
		return previewWidget;
	}

	public override Widget CreateToolbar()
	{
		var stopButton = new IconButton( "pause", () =>
		{
			previewWidget?.StopPlayingSounds();
		} );
		return stopButton;
	}
}


class SoundPreviewWidget : Widget
{
	Asset _value;
	public Asset Asset
	{
		get => _value;
		set
		{
			_value = value;

			if ( _value.TryLoadResource<SoundEvent>( out var obj ) )
			{
				SoundEvent = obj;
				Update();
			}
		}
	}

	SoundEvent SoundEvent;

	public float Distance => SoundEvent?.Distance ?? 1024f;
	public Curve Falloff => SoundEvent?.Falloff ?? new Curve( new( 0, 1 ), new( 1, 0 ) );

	public SoundPreviewWidget( Widget parent ) : base( parent )
	{
		MouseTracking = true;
		SetSizeMode( SizeMode.Flexible, SizeMode.Flexible );
	}

	Vector3 LocalToWorld( Vector2 pos )
	{
		var size = Parent.LocalRect.Size;
		var maxSize = MathF.Min( size.x, size.y );

		// center is center
		pos -= size * 0.5f;
		pos /= maxSize;
		pos *= Distance * 2f;

		return new Vector3( pos.y, -pos.x, 0 );
	}

	Vector2 WorldToLocal( Vector3 pos )
	{
		var size = Parent.LocalRect.Size;
		var maxSize = MathF.Min( size.x, size.y );

		Vector2 pos2d = new Vector2( -pos.y, pos.x );
		pos2d /= Distance * 2f;
		pos2d *= maxSize;
		pos2d += size * 0.5f;

		return pos2d;
	}


	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.Antialiasing = true;
		var localRect = Parent.LocalRect;
		var maxSize = MathF.Min( localRect.Width, localRect.Height );

		Paint.ClearPen();
		Paint.SetBrush( Theme.Blue.Darken( 0.8f ) );
		Paint.DrawRect( localRect );

		var toWorld = 1 / ((Distance * 2) / maxSize);
		var center = new Vector2( localRect.Width * 0.5f, localRect.Height * 0.5f );
		var scale = (20 / Distance) * maxSize;

		Paint.ClearPen();
		Paint.SetBrush( Theme.Blue.WithAlpha( 0.02f ) );

		// Sound Radius
		{
			var radius = (maxSize - 10) / 2f;
			for ( int i = 0; i < 16; i++ )
			{
				float f = i / 16f;
				float position = radius * (f + 1f / 16f);
				position -= radius / 16f / 2f;
				Paint.SetPen( Theme.Blue.WithAlpha( (0.01f + Falloff.Evaluate( f )) * 0.5f ), MathF.Round( radius / 16f ) + 2f );
				Paint.DrawArc( center, MathF.Round( position ), 0, 360 );
			}
		}

		{
			Paint.SetPen( Theme.Blue.WithAlpha( 0.2f ), 1 );
			Paint.DrawCircle( center, new Vector2( maxSize - 10 ) );

			Paint.SetPen( Theme.Blue.WithAlpha( 0.3f ), 1 );
			Paint.DrawText( new Rect( localRect.Width * 0.5f + 10, 10, localRect.Height * 0.3f, 100 ), $"{(Distance):n0} units\n{Distance.InchToMeter():n0} meters", TextFlag.LeftCenter );
			Paint.DrawLine( new Vector2( localRect.Width * 0.5f, 10 ), new Vector2( localRect.Width * 0.5f, localRect.Height * 0.5f - 10 ) );
		}

		// Person
		Paint.SetPen( Theme.Text, 2 );
		Paint.SetBrush( Color.White.WithAlpha( 0.3f ) );
		Paint.DrawCircle( center, scale );

		foreach ( var sound in PlayingSounds )
		{
			var pos = WorldToLocal( sound.Position );

			Paint.ClearPen();

			Paint.SetBrush( Theme.Green );
			Paint.DrawCircle( pos, 5.0f );

			var green = Theme.Green.Darken( 0.3f );
			Paint.SetPen( green, 2.0f );
			if ( sound == dragSound )
			{
				var distance = sound.Position.Length;
				var gap = 64f / WorldToLocal( Vector3.Forward * distance ).Length;
				var mid1 = center + (pos - center) * (0.5f - gap);
				var mid2 = center + (pos - center) * (0.5f + gap);
				var middle = center + (pos - center) * 0.5f;
				Paint.DrawLine( pos, mid2 );
				Paint.DrawLine( mid1, center );
				var alpha = MathX.Remap( sound.Position.Length - Distance * 0.33f, 0f, distance * 0.33f, 0f, 1f );
				Paint.SetPen( Theme.Green.WithAlpha( alpha ), 2.0f );
				Paint.DrawText( new Rect( middle + Vector2.Up * 6f - Vector2.Right * 32f, new Vector2( 64, 12 ) ), $"{distance:n0}", TextFlag.Center );
				Paint.SetPen( green.WithAlpha( 1.0f - alpha ), 2.0f );
				Paint.DrawLine( mid1, mid2 );
			}
			else
			{
				Paint.DrawLine( pos, center );
			}
		}
	}

	class PlayingSound
	{
		public Sandbox.SoundHandle SoundHandle;
		public Vector3 Position;
	}

	List<PlayingSound> PlayingSounds = new();

	PlayingSound dragSound;

	Vector2 mousePosition;

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		foreach ( var sound in PlayingSounds )
		{
			sound.SoundHandle?.Stop();
		}
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		base.OnMouseMove( e );

		if ( e.ButtonState.HasFlag( MouseButtons.Left ) && dragSound is not null )
		{
			dragSound.SoundHandle.Position = LocalToWorld( e.LocalPosition );
			dragSound.Position = LocalToWorld( e.LocalPosition );
		}

		mousePosition = e.LocalPosition;
		Update();
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		dragSound = null;

		Update();
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		PlayAtLocal( e.LocalPosition );
	}

	public void PlayAtLocal( Vector2 location )
	{
		PlayAtWorld( LocalToWorld( location ) );
	}

	public void PlayAtWorld( Vector2 location )
	{
		var handle = Sound.Play( SoundEvent, location );
		if ( handle is null )
		{
			Log.Warning( $"Couldn't play {Asset.Name}" );
			return;
		}
		handle.ListenLocal = true;

		dragSound = new PlayingSound { Position = location, SoundHandle = handle };
		PlayingSounds.Add( dragSound );

		Update();
		// Play sound from asset name
		// set position, listen position, overrides
		// occlusion
	}

	int redrawHashCode = 0;

	[EditorEvent.Frame]
	public void Tick()
	{
		var hc = System.HashCode.Combine( Distance, Falloff.Frames.Sum( x => x.Time + x.Value + x.In + x.Out ), PlayingSounds.Count );
		if ( hc != redrawHashCode )
		{
			redrawHashCode = hc;
			Update();
		}

		PlayingSounds.RemoveAll( x => !x.SoundHandle.IsPlaying );
	}

	public void StopPlayingSounds()
	{
		foreach ( var sound in PlayingSounds )
		{
			sound.SoundHandle.Stop();
		}

		PlayingSounds.Clear();
	}
}
