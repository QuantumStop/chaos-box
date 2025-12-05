
using Sandbox;

namespace Editor.MeshEditor;

/// <summary>
/// Base class for vertex, edge and face tools.
/// </summary>
public abstract class BaseMeshTool : EditorTool
{
	public SelectionSystem MeshSelection => Selection;
	public HashSet<MeshVertex> VertexSelection { get; init; } = new();

	public static Vector2 RayScreenPosition => SceneViewportWidget.MousePosition;

	public static bool IsMultiSelecting => Application.KeyboardModifiers.HasFlag( KeyboardModifiers.Ctrl ) ||
				Application.KeyboardModifiers.HasFlag( KeyboardModifiers.Shift );

	public Vector3 Pivot { get; set; }

	private bool _meshSelectionDirty;
	private bool _nudge;
	private bool _invertSelection;

	private MeshComponent _hoverMesh;

	protected IDisposable _undoScope;

	protected virtual bool DrawVertices => false;

	public override IEnumerable<EditorTool> GetSubtools()
	{
		yield return new PositionTool( this );
		yield return new RotateTool( this );
		yield return new ScaleTool( this );
		yield return new PivotTool( this );
	}

	public override void OnEnabled()
	{
		base.OnEnabled();

		AllowGameObjectSelection = false;

		Selection.Clear();

		MeshSelection.OnItemAdded += OnMeshSelectionChanged;
		MeshSelection.OnItemRemoved += OnMeshSelectionChanged;

		SceneEditorSession.Active.UndoSystem.OnUndo += ( _ ) => OnMeshSelectionChanged();
		SceneEditorSession.Active.UndoSystem.OnRedo += ( _ ) => OnMeshSelectionChanged();
	}

	public override void OnDisabled()
	{
		base.OnDisabled();

		var selectedObjects = GetSelectedObjects();
		Selection.Clear();

		foreach ( var go in selectedObjects )
		{
			if ( !go.IsValid() )
				continue;

			Selection.Add( go );
		}
	}

	public override void OnUpdate()
	{
		base.OnUpdate();

		if ( Gizmo.WasLeftMouseReleased && !Gizmo.Pressed.Any && Gizmo.Pressed.CursorDelta.Length < 1 )
		{
			Gizmo.Select();
		}

		var removeList = GetInvalidSelection().ToList();
		foreach ( var s in removeList )
			MeshSelection.Remove( s );

		if ( Application.IsKeyDown( KeyCode.I ) )
		{
			if ( !_invertSelection && Gizmo.IsCtrlPressed )
			{
				InvertSelection();
			}

			_invertSelection = true;
		}
		else
		{
			_invertSelection = false;
		}

		UpdateNudge();

		if ( _meshSelectionDirty )
		{
			CalculateSelectionVertices();
			OnMeshSelectionChanged();
		}

		DrawSelection();
	}

	private void InvertSelection()
	{
		if ( !MeshSelection.Any() )
			return;

		var newSelection = GetAllSelectedElements()
			.Except( MeshSelection )
			.ToArray();

		MeshSelection.Clear();

		foreach ( var element in newSelection )
			MeshSelection.Add( element );
	}

	protected virtual IEnumerable<IMeshElement> GetAllSelectedElements()
	{
		return Enumerable.Empty<IMeshElement>();
	}

	private void DrawSelection()
	{
		var face = TraceFace();
		if ( face.IsValid() )
			_hoverMesh = face.Component;

		if ( _hoverMesh.IsValid() )
			DrawMesh( _hoverMesh );

		foreach ( var group in MeshSelection.OfType<IMeshElement>()
			.GroupBy( x => x.Component ) )
		{
			var component = group.Key;
			if ( !component.IsValid() )
				continue;

			if ( component == _hoverMesh )
				continue;

			DrawMesh( component );
		}
	}

	protected void DrawMesh( MeshComponent mesh )
	{
		using ( Gizmo.ObjectScope( mesh.GameObject, mesh.WorldTransform ) )
		{
			var color = new Color( 0.3137f, 0.7843f, 1.0f, 0.5f );

			if ( DrawVertices )
			{
				using ( Gizmo.Scope( "Vertices" ) )
				{
					Gizmo.Draw.Color = color;

					foreach ( var v in mesh.Mesh.GetVertexPositions() )
					{
						Gizmo.Draw.Sprite( v, 8, null, false );
					}
				}
			}

			using ( Gizmo.Scope( "Edges" ) )
			{
				Gizmo.Draw.Color = color;
				Gizmo.Draw.LineThickness = 2;

				foreach ( var v in mesh.Mesh.GetEdges() )
				{
					Gizmo.Draw.Line( v );
				}
			}
		}
	}

	private void UpdateNudge()
	{
		if ( Gizmo.Pressed.Any || !Application.FocusWidget.IsValid() )
			return;

		var keyUp = Application.IsKeyDown( KeyCode.Up );
		var keyDown = Application.IsKeyDown( KeyCode.Down );
		var keyLeft = Application.IsKeyDown( KeyCode.Left );
		var keyRight = Application.IsKeyDown( KeyCode.Right );

		if ( !keyUp && !keyDown && !keyLeft && !keyRight )
		{
			_nudge = false;

			_undoScope?.Dispose();
			_undoScope = null;

			return;
		}

		if ( _nudge )
			return;

		var basis = CalculateSelectionBasis();
		var direction = new Vector2( keyLeft ? 1 : keyRight ? -1 : 0, keyUp ? 1 : keyDown ? -1 : 0 );
		var delta = Gizmo.Nudge( basis, direction );

		var components = MeshSelection.OfType<IMeshElement>().Select( x => x.Component );

		_undoScope ??= SceneEditorSession.Active.UndoScope( "Nudge Vertices" ).WithComponentChanges( components ).Push();

		if ( Gizmo.IsShiftPressed )
		{
			ExtrudeSelection( delta );
		}
		else
		{
			foreach ( var vertex in VertexSelection )
			{
				var transform = vertex.Transform;
				var position = vertex.Component.Mesh.GetVertexPosition( vertex.Handle );
				position = transform.PointToWorld( position ) + delta;
				vertex.Component.Mesh.SetVertexPosition( vertex.Handle, transform.PointToLocal( position ) );
			}
		}

		_nudge = true;
	}

	public virtual List<MeshFace> ExtrudeSelection( Vector3 delta = default )
	{
		return default;
	}

	public override void OnSelectionChanged()
	{
		base.OnSelectionChanged();

		if ( Selection.OfType<GameObject>().Any() )
		{
			EditorToolManager.CurrentModeName = "object";
		}
	}

	public BBox CalculateSelectionBounds()
	{
		return BBox.FromPoints( VertexSelection
			.Where( x => x.IsValid() )
			.Select( x => x.Transform.PointToWorld( x.Component.Mesh.GetVertexPosition( x.Handle ) ) ) );
	}

	public virtual Rotation CalculateSelectionBasis()
	{
		return Rotation.Identity;
	}

	public virtual Vector3 CalculateSelectionOrigin()
	{
		var bounds = CalculateSelectionBounds();
		return bounds.Center;
	}

	public void CalculateSelectionVertices()
	{
		VertexSelection.Clear();

		foreach ( var face in MeshSelection.OfType<MeshFace>() )
		{
			foreach ( var vertex in face.Component.Mesh.GetFaceVertices( face.Handle )
				.Select( i => new MeshVertex( face.Component, i ) ) )
			{
				VertexSelection.Add( vertex );
			}
		}

		foreach ( var vertex in MeshSelection.OfType<MeshVertex>() )
		{
			VertexSelection.Add( vertex );
		}

		foreach ( var edge in MeshSelection.OfType<MeshEdge>() )
		{
			edge.Component.Mesh.GetEdgeVertices( edge.Handle, out var hVertexA, out var hVertexB );
			VertexSelection.Add( new MeshVertex( edge.Component, hVertexA ) );
			VertexSelection.Add( new MeshVertex( edge.Component, hVertexB ) );
		}

		_meshSelectionDirty = false;
	}

	private HashSet<GameObject> GetSelectedObjects()
	{
		var objects = new HashSet<GameObject>();
		foreach ( var face in MeshSelection.OfType<IMeshElement>()
			.Where( x => x.IsValid() ) )
		{
			objects.Add( face.GameObject );
		}

		return objects;
	}

	private IEnumerable<IMeshElement> GetInvalidSelection()
	{
		foreach ( var selection in MeshSelection.OfType<IMeshElement>()
			.Where( x => !x.IsValid() || x.Scene != Scene ) )
		{
			yield return selection;
		}
	}

	private void OnMeshSelectionChanged( object o )
	{
		_hoverMesh = null;
		_meshSelectionDirty = true;
	}

	private void OnMeshSelectionChanged()
	{
		Pivot = CalculateSelectionOrigin();
	}

	protected void Select( IMeshElement element )
	{
		if ( Application.KeyboardModifiers.HasFlag( KeyboardModifiers.Ctrl ) )
		{
			if ( MeshSelection.Contains( element ) )
			{
				MeshSelection.Remove( element );
			}
			else
			{
				MeshSelection.Add( element );
			}

			return;
		}
		else if ( Application.KeyboardModifiers.HasFlag( KeyboardModifiers.Shift ) )
		{
			if ( !MeshSelection.Contains( element ) )
			{
				MeshSelection.Add( element );
			}

			return;
		}

		MeshSelection.Set( element );
	}

	protected void UpdateSelection( IMeshElement element )
	{
		if ( Gizmo.WasLeftMousePressed )
		{
			if ( element.IsValid() )
			{
				Select( element );
			}
			else if ( !IsMultiSelecting )
			{
				MeshSelection.Clear();
			}
		}
		else if ( Gizmo.IsLeftMouseDown && Gizmo.CursorMoveDelta.LengthSquared > 0 && element.IsValid() )
		{
			if ( Application.KeyboardModifiers.HasFlag( KeyboardModifiers.Ctrl ) )
			{
				if ( MeshSelection.Contains( element ) )
					MeshSelection.Remove( element );
			}
			else
			{
				if ( !MeshSelection.Contains( element ) )
					MeshSelection.Add( element );
			}
		}
	}

	protected MeshVertex GetClosestVertex( int radius )
	{
		var point = RayScreenPosition;
		var bestFace = TraceFace( out var bestHitDistance );
		var bestVertex = bestFace.GetClosestVertex( point, radius );

		if ( bestFace.IsValid() && bestVertex.IsValid() )
			return bestVertex;

		var results = TraceFaces( radius, point );
		foreach ( var result in results )
		{
			var face = result.MeshFace;
			var hitDistance = result.Distance;
			var vertex = face.GetClosestVertex( point, radius );
			if ( !vertex.IsValid() )
				continue;

			if ( hitDistance < bestHitDistance || !bestFace.IsValid() )
			{
				bestHitDistance = hitDistance;
				bestVertex = vertex;
				bestFace = face;
			}
		}

		return bestVertex;
	}

	protected MeshEdge GetClosestEdge( int radius )
	{
		var point = RayScreenPosition;
		var bestFace = TraceFace( out var bestHitDistance );
		var hitPosition = Gizmo.CurrentRay.Project( bestHitDistance );
		var bestEdge = bestFace.GetClosestEdge( hitPosition, point, radius );

		if ( bestFace.IsValid() && bestEdge.IsValid() )
			return bestEdge;

		var results = TraceFaces( radius, point );
		foreach ( var result in results )
		{
			var face = result.MeshFace;
			var hitDistance = result.Distance;
			hitPosition = Gizmo.CurrentRay.Project( hitDistance );

			var edge = face.GetClosestEdge( hitPosition, point, radius );
			if ( !edge.IsValid() )
				continue;

			if ( hitDistance < bestHitDistance || !bestFace.IsValid() )
			{
				bestHitDistance = hitDistance;
				bestEdge = edge;
				bestFace = face;
			}
		}

		return bestEdge;
	}

	private MeshFace TraceFace( out float distance )
	{
		distance = default;

		var result = MeshTrace.Run();
		if ( !result.Hit || result.Component is not MeshComponent component )
			return default;

		distance = result.Distance;
		var face = component.Mesh.TriangleToFace( result.Triangle );
		return new MeshFace( component, face );
	}

	public MeshFace TraceFace()
	{
		var result = MeshTrace.Run();
		if ( !result.Hit || result.Component is not MeshComponent component )
			return default;

		var face = component.Mesh.TriangleToFace( result.Triangle );
		return new MeshFace( component, face );
	}

	private struct MeshFaceTraceResult
	{
		public MeshFace MeshFace;
		public float Distance;
	}

	private List<MeshFaceTraceResult> TraceFaces( int radius, Vector2 point )
	{
		var rays = new List<Ray> { Gizmo.CurrentRay };
		for ( var ring = 1; ring < radius; ring++ )
		{
			rays.Add( Gizmo.Camera.GetRay( point + new Vector2( 0, ring ) ) );
			rays.Add( Gizmo.Camera.GetRay( point + new Vector2( ring, 0 ) ) );
			rays.Add( Gizmo.Camera.GetRay( point + new Vector2( 0, -ring ) ) );
			rays.Add( Gizmo.Camera.GetRay( point + new Vector2( -ring, 0 ) ) );
		}

		var faces = new List<MeshFaceTraceResult>();
		var faceHash = new HashSet<MeshFace>();
		foreach ( var ray in rays )
		{
			var result = MeshTrace.Ray( ray, Gizmo.RayDepth ).Run();
			if ( !result.Hit )
				continue;

			if ( result.Component is not MeshComponent component )
				continue;

			var face = component.Mesh.TriangleToFace( result.Triangle );
			var faceElement = new MeshFace( component, face );
			if ( faceHash.Add( faceElement ) )
				faces.Add( new MeshFaceTraceResult { MeshFace = faceElement, Distance = result.Distance } );
		}

		return faces;
	}

	protected static Vector3 ComputeTextureVAxis( Vector3 normal ) => FaceDownVectors[GetOrientationForPlane( normal )];

	private static int GetOrientationForPlane( Vector3 plane )
	{
		plane = plane.Normal;
		var maxDot = 0.0f;
		int orientation = 0;

		for ( int i = 0; i < 6; i++ )
		{
			var dot = Vector3.Dot( plane, FaceNormals[i] );
			if ( dot >= maxDot )
			{
				maxDot = dot;
				orientation = i;
			}
		}

		return orientation;
	}

	private static readonly Vector3[] FaceNormals =
	{
		new( 0, 0, 1 ),
		new( 0, 0, -1 ),
		new( 0, -1, 0 ),
		new( 0, 1, 0 ),
		new( -1, 0, 0 ),
		new( 1, 0, 0 ),
	};

	private static readonly Vector3[] FaceDownVectors =
	{
		new( 0, -1, 0 ),
		new( 0, -1, 0 ),
		new( 0, 0, -1 ),
		new( 0, 0, -1 ),
		new( 0, 0, -1 ),
		new( 0, 0, -1 ),
	};
}
