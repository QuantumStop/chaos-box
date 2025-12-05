
namespace Editor.MeshEditor;

/// <summary>
/// Base class for moving mesh elements (move, rotate, scale)
/// </summary>
public abstract class BaseMoveTool : EditorTool
{
	protected BaseMeshTool MeshTool { get; private init; }

	protected IReadOnlyDictionary<MeshVertex, Vector3> TransformVertices => _transformVertices;

	private readonly Dictionary<MeshVertex, Vector3> _transformVertices = new();
	private List<MeshFace> _transformFaces;

	public BaseMoveTool( BaseMeshTool meshTool )
	{
		MeshTool = meshTool;
	}

	private IDisposable _undoScope;

	protected void StartDrag()
	{
		if ( _transformVertices.Any() )
			return;


		var components = MeshTool.Selection.OfType<IMeshElement>().Select( x => x.Component );

		_undoScope ??= SceneEditorSession.Active.UndoScope( $"{(Gizmo.IsShiftPressed ? "Extrude" : "Move")} Selection" ).WithComponentChanges( components ).Push();

		if ( Gizmo.IsShiftPressed )
		{
			_transformFaces = MeshTool.ExtrudeSelection();
		}

		foreach ( var vertex in MeshTool.VertexSelection )
		{
			_transformVertices[vertex] = vertex.PositionWorld;
		}
	}

	protected void UpdateDrag()
	{
		if ( _transformFaces is not null )
		{
			foreach ( var group in _transformFaces.GroupBy( x => x.Component ) )
			{
				var mesh = group.Key.Mesh;
				var faces = group.Select( x => x.Handle ).ToArray();

				foreach ( var face in faces )
				{
					mesh.TextureAlignToGrid( mesh.Transform, face );
				}
			}
		}

		var meshes = TransformVertices.GroupBy( x => x.Key.Component.Mesh )
			.Select( x => x.Key );

		foreach ( var mesh in meshes )
		{
			mesh.ComputeFaceTextureCoordinatesFromParameters();
		}
	}

	protected void EndDrag()
	{
		_transformVertices.Clear();
		_transformFaces = null;

		_undoScope?.Dispose();
		_undoScope = null;
	}
}
