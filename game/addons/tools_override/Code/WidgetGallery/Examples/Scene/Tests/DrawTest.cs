namespace Editor.Widgets.SceneTests;

[Title( "Draw" ), Icon( "create" )]
[Description( "Drawing tests for every shape - quick overview to see if you broke anything" )]
internal class DrawTests : ISceneTest
{
	public void Initialize( CameraComponent camera )
	{

		Log.Info( "Test" );
		camera.WorldPosition = Vector3.Forward * -600.0f;
		camera.WorldRotation = Rotation.LookAt( Vector3.Forward, Vector3.Up );
		camera.FieldOfView = 70;
	}

	public void Frame()
	{
		var pos = Vector3.Zero;

		using ( Gizmo.Scope( "Draw", pos ) )
		{
			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.LineThickness = 2.0f;
			Gizmo.Draw.Line( Vector3.Left * 20.0f, Vector3.Right * 20.0f );

			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "Line", new Transform( Vector3.Down * 15.0f ) );
		}

		pos += Vector3.Up * 100.0f;

		using ( Gizmo.Scope( "LineSphere", pos ) )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "LineSphere", new Transform( Vector3.Down * 35.0f ) );

			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.LineThickness = 2.0f;
			Gizmo.Draw.LineSphere( new Sphere( Vector3.Right * 50.0f, 20.0f ), 4 );
			Gizmo.Draw.LineSphere( new Sphere( 0, 20.0f ), 8 );

			using ( Gizmo.Scope( "Spinner", new Transform( Vector3.Left * 50.0f, Rotation.From( RealTime.Now * 45.0f, RealTime.Now * 90.0f, 0 ) ) ) )
			{
				Gizmo.Draw.LineSphere( new Sphere( 0, 20.0f ), 16 );
			}
		}

		pos += Vector3.Up * 100.0f;

		using ( Gizmo.Scope( "BBox", pos ) )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "BBox", new Transform( Vector3.Down * 35.0f ) );

			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.LineThickness = 2.0f;

			using ( Gizmo.Scope( "Spinner", new Transform( 0, Rotation.From( 45, RealTime.Now * 10.0f, 45 ) ) ) )
			{
				var depth = 24.0f + MathF.Sin( RealTime.Now * 8.0f ) * 8.0f;

				Gizmo.Draw.LineBBox( new BBox( new Vector3( -16, -depth, -16 ), new Vector3( 16, depth, 16 ) ) );
			}
		}

		pos += Vector3.Left * 200.0f;

		using ( Gizmo.Scope( "Circle", pos ) )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "Circle", new Transform( Vector3.Down * 35.0f ) );

			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.LineThickness = 2.0f;

			Gizmo.Draw.LineCircle( Vector3.Left * 60.0f, 24.0f, RealTime.Now * -10.0f, 180, 64 );

			using ( Gizmo.Scope( "Spinner", new Transform( 0, Rotation.From( 0, RealTime.Now * 45.0f, 0 ) ) ) )
			{
				Gizmo.Draw.LineCircle( 0, 24.0f );
			}
		}

		pos += Vector3.Down * 100.0f;

		using ( Gizmo.Scope( "Model", pos ) )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "Model", new Transform( Vector3.Down * 35.0f ) );

			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.LineThickness = 2.0f;

			using ( Gizmo.Scope( "Spinner", new Transform( 0, Rotation.From( 0, RealTime.Now * 90.0f, 0 ) ) ) )
			{
				Gizmo.Draw.Model( "models/citizen_props/crate01.vmdl" );
			}
		}

		pos += Vector3.Down * 100.0f;

		using ( Gizmo.Scope( "SolidCircle", pos ) )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "SolidCircle", new Transform( Vector3.Down * 35.0f ) );

			Gizmo.Draw.Color = Color.White;

			Gizmo.Draw.SolidCircle( Vector3.Left * 60.0f, 24.0f, RealTime.Now * 25.0f, 180 );
			Gizmo.Draw.SolidCircle( Vector3.Right * 60.0f, 24.0f, sections: (int)(16 + MathF.Sin( RealTime.Now ) * 13.0f) );
			Gizmo.Draw.SolidCircle( 0, 24.0f, RealTime.Now * 180.0f, 45.0f, 32 );
		}

		pos += Vector3.Down * 100.0f;

		using ( Gizmo.Scope( "SolidRing Ring", pos ) )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "SolidRing Ring", new Transform( Vector3.Down * 35.0f ) );

			Gizmo.Draw.Color = Color.White;

			Gizmo.Draw.SolidRing( Vector3.Left * 60.0f, 16.0f, 24.0f, RealTime.Now * 25.0f, 180 );
			Gizmo.Draw.SolidRing( Vector3.Right * 60.0f, 16.0f, 24.0f, sections: (int)(16 + MathF.Sin( RealTime.Now ) * 13.0f) );

			Gizmo.Draw.SolidRing( 0, 20.0f, 24.0f, RealTime.Now * 180.0f, 45.0f );
			Gizmo.Draw.SolidRing( 0, 10.0f, 24.0f, RealTime.Now * 180.0f + 90.0f, 45.0f );

			Gizmo.Draw.SolidRing( 0, 20.0f, 24.0f, RealTime.Now * 180.0f + 180.0f, 45.0f );
			Gizmo.Draw.SolidRing( 0, 10.0f, 24.0f, RealTime.Now * 180.0f + 270.0f, 45.0f );
		}

		pos += Vector3.Right * 200.0f;

		using ( Gizmo.Scope( "SolidCone", pos ) )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "SolidCone", new Transform( Vector3.Down * 35.0f ) );

			Gizmo.Draw.Color = Color.White;

			Gizmo.Draw.SolidCone( Vector3.Left * 60.0f, Vector3.Up * 20.0f, 10.0f + MathF.Sin( RealTime.Now ) * 5.0f );
			Gizmo.Draw.SolidCone( 0, Vector3.Up * 20.0f, 10.0f );
			Gizmo.Draw.SolidCone( Vector3.Right * 60.0f, Vector3.Up * 20.0f, 10.0f, 3 );
		}

		pos += Vector3.Right * 200.0f;

		using ( Gizmo.Scope( "Sprite", pos ) )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "Sprite", new Transform( Vector3.Down * 35.0f ) );

			Gizmo.Draw.Color = Color.White;

			// TODO - we should be able to load sprites from disk without making a material!!!

			Gizmo.Draw.Sprite( 0, 16, "https://files.facepunch.com/garry/19f51cdf-43de-4d33-9b1c-7fc8166d6b1b.png" );
			Gizmo.Draw.Sprite( Vector3.Right * 60.0f, 12.0f + MathF.Sin( RealTime.Now * 16.0f ) * 4.0f, "https://icons.iconarchive.com/icons/famfamfam/silk/16/box-icon.png" );
			Gizmo.Draw.Sprite( Vector3.Right * -60.0f, 16, "https://icons.iconarchive.com/icons/famfamfam/silk/16/heart-icon.png" );
		}

		pos += Vector3.Up * 100.0f;

		using ( Gizmo.Scope( "SolidScreenAlignedCircle", pos ) )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.Text( "SolidScreenAlignedCircle", new Transform( Vector3.Down * 35.0f ) );

			Gizmo.Draw.Color = Color.White;

			// TODO - we should be able to load sprites from disk without making a material!!!

			Gizmo.Draw.ScreenBiasedHalfCircle( 0, 16 );
		}
	}
}
