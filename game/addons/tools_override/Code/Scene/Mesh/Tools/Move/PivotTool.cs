
namespace Editor.MeshEditor;

/// <summary>
/// Set the location of the gizmo for the current selection.
/// </summary>
[Title( "Pivot Tool" )]
[Icon( "adjust" )]
[Alias( "mesh.pivot" )]
[Group( "3" )]
public sealed class PivotTool : BaseMoveTool
{
	private Vector3 _pivot;
	private Rotation _basis;

	public PivotTool( BaseMeshTool meshTool ) : base( meshTool )
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
			_pivot = origin;
			_basis = MeshTool.CalculateSelectionBasis();
		}

		using ( Gizmo.Scope( "Tool", new Transform( origin ) ) )
		{
			Gizmo.Hitbox.DepthBias = 0.01f;

			if ( Gizmo.Control.Position( "position", Vector3.Zero, out var delta, _basis ) )
			{
				_pivot += delta;

				var pivot = Gizmo.Snap( _pivot * _basis.Inverse, delta * _basis.Inverse );
				MeshTool.Pivot = pivot * _basis;
			}
		}
	}
}
