namespace Editor.Widgets.SceneTests;

[Title( "Rotation" ), Icon( "screen_rotation" )]
[Description( "Standard rotation widget" )]
internal class RotationSimple : ISceneTest
{
	Angles angles;

	public void Frame()
	{
		using ( Gizmo.Scope( "ObjectRotation", new Transform( new Vector3( 15, 12, 10 ) ) ) )
		{
			Gizmo.Draw.Color = Gizmo.Colors.Green;

			if ( Gizmo.Control.Rotate( "my-arrow", out var angleDelta ) )
			{
				angles += angleDelta;
			}

			using ( Gizmo.Scope( "Object", new Transform( 0, angles.ToRotation() ) ) )
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.Model( "models/editor/playerstart.vmdl" );
			}
		}

		Gizmo.Draw.Color = Color.Black;
		Gizmo.Draw.Text( $"Value: {angles.pitch:n0}, {angles.yaw:n0}, {angles.roll:n0}", new Transform( Vector3.Down * 50.0f ) );
	}
}
