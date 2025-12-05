
namespace Editor.MeshEditor;

/// <summary>
/// Move selected Mesh Elements.<br/> <br/> 
/// <b>Ctrl</b> - toggle snap to grid<br/>
/// <b>Shift</b> - extrude selection
/// </summary>
[Title( "Move/Position" )]
[Icon( "control_camera" )]
[Alias( "tools.position-tool" )]
[Group( "0" )]
public sealed class PositionTool : BaseMoveTool
{
	private Vector3 _moveDelta;
	private Vector3 _origin;
	private Rotation _basis;

	public PositionTool( BaseMeshTool meshTool ) : base( meshTool )
	{
	}

	public override void OnUpdate()
	{
		base.OnUpdate();

		if ( !Selection.OfType<IMeshElement>().Any() )
			return;

		var origin = MeshTool.Pivot;

		if ( !Gizmo.Pressed.Any && Gizmo.HasMouseFocus )
		{
			EndDrag();

			_basis = MeshTool.CalculateSelectionBasis();
			_origin = origin;
			_moveDelta = default;
		}

		using ( Gizmo.Scope( "Tool", new Transform( origin ) ) )
		{
			Gizmo.Hitbox.DepthBias = 0.01f;

			if ( Gizmo.Control.Position( "position", Vector3.Zero, out var delta, _basis ) )
			{
				_moveDelta += delta;

				var moveDelta = (_moveDelta + _origin) * _basis.Inverse;
				moveDelta = Gizmo.Snap( moveDelta, _moveDelta * _basis.Inverse );
				moveDelta *= _basis;

				MeshTool.Pivot = moveDelta;

				moveDelta -= _origin;

				StartDrag();

				var comps = TransformVertices.Select( x => x.Key.Component ).Distinct();

				foreach ( var entry in TransformVertices )
				{
					var position = entry.Value + moveDelta;
					var transform = entry.Key.Transform;
					entry.Key.Component.Mesh.SetVertexPosition( entry.Key.Handle, transform.PointToLocal( position ) );
				}

				UpdateDrag();
			}
		}
	}

	[Shortcut( "tools.position-tool", "w", typeof( SceneViewportWidget ) )]
	public static void ActivateSubTool()
	{
		if ( !(EditorToolManager.CurrentModeName == nameof( VertexTool ) || EditorToolManager.CurrentModeName == nameof( FaceTool ) || EditorToolManager.CurrentModeName == nameof( EdgeTool )) ) return;
		EditorToolManager.SetSubTool( nameof( PositionTool ) );
	}
}
