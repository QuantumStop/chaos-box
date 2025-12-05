
using Sandbox.Utility;

namespace Editor.Widgets.SceneTests;

[Title( "Lines" )]
[Icon( "show_chart" )]
[Description( "Testing line drawing and line hitboxes.\nYou should be able to click on a line to select it." )]
internal class LineTest : ISceneTest
{
	public void Frame()
	{

		for ( var row = 0; row < 32; row++ )
		{
			var x = row * 10.0f;
			Vector3 last = 0.0f;

			using ( Gizmo.Scope( $"Line{row}", Transform.Zero.WithPosition( new Vector3( -320, 0, -100 ) ) ) )
			{
				Gizmo.Draw.LineThickness = 6;
				Gizmo.Draw.Color = Gizmo.IsHovered ? Color.Green : Color.White;

				if ( Gizmo.IsSelected )
					Gizmo.Draw.Color = Color.Red;

				using var hitScope = Gizmo.Hitbox.LineScope();

				if ( Gizmo.HasClicked && Gizmo.Pressed.This )
				{
					Gizmo.Select( true, true );
				}

				for ( var i = 0; i < 32; i++ )
				{
					var y = i * 10.0f;
					var p = new Vector3( x, y, Noise.Simplex( RealTime.Now * 10.0f + x * 0.3f, y * 0.3f, 0 ) * 30.0f );

					if ( i > 0 )
					{
						Gizmo.Draw.Line( last, p );
					}

					last = p;
				}
			}
		}
	}
}
