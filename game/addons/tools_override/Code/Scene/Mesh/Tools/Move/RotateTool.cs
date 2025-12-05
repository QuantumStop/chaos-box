
namespace Editor.MeshEditor;

/// <summary>
/// Rotate selected Mesh Elements.<br/> <br/> 
/// <b>Ctrl</b> - toggle snap to grid
/// <b>Shift</b> - extrude selection
/// </summary>
[Title( "Rotate" )]
[Icon( "360" )]
[Alias( "tools.rotate-tool" )]
[Group( "1" )]
public sealed class RotateTool : BaseMoveTool
{
	private Angles _moveDelta;
	private Vector3 _origin;
	private Rotation _basis;

	public RotateTool( BaseMeshTool meshTool ) : base( meshTool )
	{
	}

	public override void OnUpdate()
	{
		base.OnUpdate();

		if ( !Selection.OfType<IMeshElement>().Any() )
			return;

		if ( !Gizmo.Pressed.Any && Gizmo.HasMouseFocus )
		{
			EndDrag();

			_moveDelta = default;
			_basis = MeshTool.CalculateSelectionBasis();
			_origin = MeshTool.Pivot;
		}

		using ( Gizmo.Scope( "Tool", new Transform( _origin, _basis ) ) )
		{
			Gizmo.Hitbox.DepthBias = 0.01f;

			if ( Gizmo.Control.Rotate( "rotation", out var angleDelta ) )
			{
				var components = TransformVertices.Select( x => x.Key.Component ).Distinct();

				_moveDelta += angleDelta;

				StartDrag();

				var snapDelta = Gizmo.Snap( _moveDelta, _moveDelta );

				foreach ( var entry in TransformVertices )
				{
					var rotation = _basis * snapDelta * _basis.Inverse;
					var position = entry.Value - _origin;
					position *= rotation;
					position += _origin;

					var transform = entry.Key.Transform;
					entry.Key.Component.Mesh.SetVertexPosition( entry.Key.Handle, transform.PointToLocal( position ) );
				}

				UpdateDrag();
			}
		}
	}

	[Shortcut( "tools.rotate-tool", "e", typeof( SceneViewportWidget ) )]
	public static void ActivateSubTool()
	{
		if ( !(EditorToolManager.CurrentModeName == nameof( VertexTool ) || EditorToolManager.CurrentModeName == nameof( FaceTool ) || EditorToolManager.CurrentModeName == nameof( EdgeTool )) ) return;
		EditorToolManager.SetSubTool( nameof( RotateTool ) );
	}
}
