using HalfEdgeMesh;
using System.Text.Json.Serialization;

namespace Editor.MeshEditor;

/// <summary>
/// References a edge handle and the mesh component it belongs to.
/// </summary>
public struct MeshEdge : IMeshElement
{
	[Hide, JsonInclude] public MeshComponent Component { get; private init; }
	[Hide, JsonIgnore] public readonly HalfEdgeHandle Handle => Component.IsValid() ? Component.Mesh.HalfEdgeHandleFromIndex( HandleIndex ) : default;
	[Hide, JsonInclude] public int HandleIndex { get; set; }

	[Hide, JsonIgnore] public readonly bool IsValid => Component.IsValid() && Handle.IsValid;
	[Hide, JsonIgnore] public readonly Transform Transform => IsValid ? Component.WorldTransform : Transform.Zero;

	[Hide, JsonIgnore] public readonly bool IsOpen => IsValid && Component.Mesh.IsEdgeOpen( Handle );
	[Hide, JsonIgnore] public readonly Line Line => IsValid ? Component.Mesh.GetEdgeLine( Handle ) : default;

	public MeshEdge( MeshComponent component, HalfEdgeHandle handle )
	{
		Component = component;
		HandleIndex = handle.Index;
	}

	[Hide, JsonIgnore]
	public readonly PolygonMesh.EdgeSmoothMode EdgeSmoothing
	{
		get => IsValid ? Component.Mesh.GetEdgeSmoothing( Handle ) : default;
		set => Component?.Mesh.SetEdgeSmoothing( Handle, value );
	}

	public readonly override int GetHashCode() => HashCode.Combine( Component, nameof( MeshEdge ), Handle );
	public override readonly string ToString() => IsValid ? $"{Component.GameObject.Name} Edge {Handle}" : "Invalid Edge";
}

public struct BevelEdges
{
	[Hide, JsonInclude] public MeshComponent Component { get; set; }
	[Hide, JsonInclude] public PolygonMesh Mesh { get; set; }
	[Hide, JsonInclude] public List<int> Edges { get; set; }
}

[Inspector( typeof( BevelEdges ) )]
public class BevelEdgesInspector : InspectorWidget
{
	private readonly BevelEdges[] Edges = null;

	private static int BevelSteps { get; set; } = 1;
	private static float BevelShape { get; set; } = 0.5f;
	private static float BevelWidth { get; set; } = 8.0f;

	private struct BevelProperties
	{
		[Title( "Steps" ), Group( "Bevel" ), Range( 1, 32 )]
		public readonly int Steps { get => BevelSteps; set => BevelSteps = value; }

		[Title( "Shape" ), Group( "Bevel" ), Range( 0.0f, 1.0f )]
		public readonly float Shape { get => BevelShape; set => BevelShape = value; }

		[Title( "Width" ), Group( "Bevel" ), Range( 0.0625f, 256.0f )]
		public readonly float Width { get => BevelWidth; set => BevelWidth = value; }
	}

	[InlineEditor( Label = false )]
	private readonly BevelProperties bevelProperties = new();

	private readonly Layout ContentLayout;

	public BevelEdgesInspector( SerializedObject so ) : base( so )
	{
		Edges = SerializedObject.Targets
			.OfType<BevelEdges>()
			.ToArray();

		Layout = Layout.Column();

		var sheet = new ControlSheet();
		var c = sheet.AddRow( this.GetSerialized().GetProperty( nameof( bevelProperties ) ) );
		c.OnChildValuesChanged += ( e ) => UpdateMesh();
		Layout.Add( sheet );

		ContentLayout = Layout.AddColumn();
		ContentLayout.Spacing = 8;
		ContentLayout.Margin = 8;

		CreateButton( "Apply", "mesh.edge-bevel-apply", Apply );
		CreateButton( "Cancel", "mesh.edge-bevel-cancel", Cancel );

		Layout.AddStretchCell();

		UpdateMesh();
	}

	private void UpdateMesh()
	{
		var steps = BevelSteps % 2 == 0 ? BevelSteps : BevelSteps - 1;

		foreach ( var edge in Edges )
		{
			var mesh = new PolygonMesh();
			mesh.Transform = edge.Mesh.Transform;
			mesh.MergeMesh( edge.Mesh, Transform.Zero, out _, out _, out _ );
			var edges = edge.Edges.Select( x => mesh.HalfEdgeHandleFromIndex( x ) ).ToList();

			var newOuterEdges = new List<HalfEdgeHandle>();
			var newInnerEdges = new List<HalfEdgeHandle>();
			var facesNeedingUVs = new List<FaceHandle>();
			var newFaces = new List<FaceHandle>();
			if ( !mesh.BevelEdges( edges, PolygonMesh.BevelEdgesMode.RemoveClosedEdges, steps, BevelWidth, BevelShape, newOuterEdges, newInnerEdges, newFaces, facesNeedingUVs ) )
				continue;

			foreach ( var hFace in facesNeedingUVs )
			{
				mesh.TextureAlignToGrid( mesh.Transform, hFace );
			}

			mesh.ComputeFaceTextureParametersFromCoordinates( newFaces );

			edge.Component.Mesh = mesh;
		}
	}

	[Shortcut( "mesh.edge-bevel-cancel", "ESC", ShortcutType.Application )]
	private void Cancel()
	{
		var components = Edges.Select( x => x.Component ).ToArray();

		using ( SceneEditorSession.Active.UndoScope( "Cancel Bevel Edges" )
			.WithComponentChanges( components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var edge in Edges )
			{
				edge.Component.Mesh = edge.Mesh;
				edge.Component.RebuildMesh();
			}
		}
	}

	[Shortcut( "mesh.edge-bevel-apply", "enter", ShortcutType.Application )]
	private void Apply()
	{
		var components = Edges.Select( x => x.Component ).ToArray();

		using ( SceneEditorSession.Active.UndoScope( "Apply Bevel Edges" )
			.WithComponentChanges( components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();
		}
	}

	void CreateButton( string text, string keybind, Action clicked, bool enabled = true )
	{
		var btn = ContentLayout.Add( new Button.Primary( text, this )
		{
			Enabled = enabled,
			Clicked = clicked,
			ToolTip = text
		} );

		if ( !string.IsNullOrEmpty( keybind ) )
		{
			btn.ToolTip = text + " [" + EditorShortcuts.GetKeys( keybind ) + "]";
		}
	}
}

[Inspector( typeof( MeshEdge ) )]
public class EdgeInspector : InspectorWidget
{
	private readonly MeshEdge[] Edges = null;
	private readonly List<IGrouping<MeshComponent, MeshEdge>> EdgeGroups;
	private readonly List<MeshComponent> Components;

	private readonly Layout ContentLayout;

	public EdgeInspector( SerializedObject so ) : base( so )
	{
		Edges = SerializedObject.Targets
			.OfType<MeshEdge>()
			.ToArray();

		EdgeGroups = Edges.GroupBy( x => x.Component ).ToList();
		Components = EdgeGroups.Select( x => x.Key ).ToList();

		Layout = Layout.Column();

		ContentLayout = Layout.AddColumn();
		ContentLayout.Spacing = 8;
		ContentLayout.Margin = 8;

		CreateButton( "Dissolve", "mesh.dissolve", Dissolve, CanDissolve() );
		CreateButton( "Collapse", "mesh.collapse", Collapse, CanCollapse() );
		CreateButton( "Bevel", "mesh.edge-bevel", Bevel, CanBevel() );
		CreateButton( "Connect", "mesh.connect", Connect, CanConnect() );
		CreateButton( "Extend", "mesh.extend", Extend, CanExtend() );
		CreateButton( "Merge", "mesh.merge", Merge, CanMerge() );
		CreateButton( "Split", "mesh.split", Split, CanSplit() );
		CreateButton( "Snap Edge to Edge", "mesh.snap-edge-to-edge", SnapEdgeToEdge, Edges.Length == 2 );
		CreateButton( "Fill Hole", "mesh.fill-hole", FillHole, CanFillHole() );
		CreateButton( "Bridge", "mesh.bridge-edges", BridgeEdges, CanBridgeEdges() );
		CreateButton( "Hard Normals", "mesh.hard-normals", HardNormals );
		CreateButton( "Soft Normals", "mesh.soft-normals", SoftNormals );
		CreateButton( "Default Normals", "mesh.default-normals", DefaultNormals );
		CreateButton( "Weld UVs", "mesh.edge-weld-uvs", WeldUVs );
		CreateButton( "Select Loop", "mesh.select-loop", SelectLoop, CanSelectLoop() );
		CreateButton( "Select Ring", "mesh.select-ring", SelectRing, CanSelectRing() );
		CreateButton( "Select Ribs", "mesh.select-ribs", SelectRibs, CanSelectRibs() );

		Layout.AddStretchCell();
	}

	void CreateButton( string text, string keybind, Action clicked, bool enabled = true )
	{
		var btn = ContentLayout.Add( new Button.Primary( text, this )
		{
			Enabled = enabled,
			Clicked = clicked,
			ToolTip = text
		} );

		if ( !string.IsNullOrEmpty( keybind ) )
		{
			btn.ToolTip = text + " [" + EditorShortcuts.GetKeys( keybind ) + "]";
		}
	}

	private void SetNormals( PolygonMesh.EdgeSmoothMode mode )
	{
		foreach ( var edge in Edges )
			edge.EdgeSmoothing = mode;
	}

	[Shortcut( "mesh.edge-weld-uvs", "CTRL+F", typeof( SceneViewportWidget ) )]
	private void WeldUVs()
	{
		if ( Edges.Length < 1 )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Weld UVs" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var group in EdgeGroups )
			{
				var component = group.Key;
				var mesh = component.Mesh;
				mesh.AverageEdgeUVs( group.Select( x => x.Handle ).ToList() );
			}
		}
	}

	private bool CanBevel()
	{
		return Edges.Length != 0;
	}

	[Shortcut( "mesh.edge-bevel", "ALT+F", typeof( SceneViewportWidget ) )]
	private void Bevel()
	{
		if ( !CanBevel() )
			return;

		using ( SceneEditorSession.Active.UndoScope( "Bevel Edges" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in EdgeGroups )
			{
				var component = group.Key;
				var mesh = component.Mesh;

				var newMesh = new PolygonMesh();
				newMesh.Transform = mesh.Transform;
				newMesh.MergeMesh( mesh, Transform.Zero, out _, out var newEdges, out _ );
				var edges = group.Select( x => newEdges[x.Handle].Index ).ToList();

				selection.Add( new BevelEdges()
				{
					Component = component,
					Mesh = newMesh,
					Edges = edges,
				} );
			}
		}
	}

	private bool CanMerge()
	{
		if ( Edges.Length != 2 )
			return false;

		var edgeA = Edges[0];
		if ( !edgeA.IsValid() )
			return false;

		var edgeB = Edges[1];
		if ( !edgeB.IsValid() )
			return false;

		if ( !edgeA.IsOpen )
			return false;

		if ( !edgeB.IsOpen )
			return false;

		return true;
	}

	private static MeshEdge MergeMeshesOfEdges( MeshEdge edgeA, MeshEdge edgeB )
	{
		if ( edgeB.Component != edgeA.Component )
		{
			var meshA = edgeA.Component;
			var meshB = edgeB.Component;

			var transform = meshA.WorldTransform.ToLocal( meshB.WorldTransform );
			meshA.Mesh.MergeMesh( meshB.Mesh, transform, out _, out var newHalfEdges, out _ );

			meshB.DestroyGameObject();

			edgeB = new MeshEdge( meshA, newHalfEdges[edgeB.Handle] );
		}

		return edgeB;
	}

	[Shortcut( "mesh.merge", "M", typeof( SceneViewportWidget ) )]
	private void Merge()
	{
		if ( !CanMerge() )
			return;

		using var scope = SceneEditorSession.Scope();

		var edgeA = Edges[0];
		var edgeB = Edges[1];

		var undoScope = SceneEditorSession.Active.UndoScope( "Merge Edges" );

		if ( edgeA.Component != edgeB.Component )
		{
			undoScope = undoScope.WithComponentChanges( edgeA.Component )
				.WithGameObjectDestructions( edgeB.Component.GameObject );
		}
		else
		{
			undoScope = undoScope.WithComponentChanges( [edgeA.Component, edgeB.Component] );
		}

		using ( undoScope.Push() )
		{
			edgeB = MergeMeshesOfEdges( edgeA, edgeB );
			var mesh = edgeA.Component.Mesh;

			if ( mesh.MergeEdges( edgeA.Handle, edgeB.Handle, out var hEdge ) )
			{
				mesh.ComputeFaceTextureCoordinatesFromParameters();

				var selection = SceneEditorSession.Active.Selection;
				selection.Set( new MeshEdge( edgeA.Component, hEdge ) );
			}
		}
	}

	private bool CanSplit()
	{
		return Edges.Length != 0;
	}

	[Shortcut( "mesh.split", "ALT+N", typeof( SceneViewportWidget ) )]
	private void Split()
	{
		if ( !CanSplit() )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Split Edges" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in EdgeGroups )
			{
				var mesh = group.Key.Mesh;
				mesh.SplitEdges( group.Select( x => x.Handle ).ToArray(), out var newEdgesA, out var newEdgesB );
				if ( newEdgesA is not null )
				{
					foreach ( var hEdge in newEdgesA )
						selection.Add( new MeshEdge( group.Key, hEdge ) );
				}
				if ( newEdgesB is not null )
				{
					foreach ( var hEdge in newEdgesB )
						selection.Add( new MeshEdge( group.Key, hEdge ) );
				}
			}
		}
	}

	[Shortcut( "editor.delete", "DEL", typeof( SceneViewportWidget ) )]
	private void DeleteSelection()
	{
		var groups = Edges.GroupBy( face => face.Component );

		if ( !groups.Any() )
			return;

		var components = groups.Select( x => x.Key ).ToArray();

		using ( SceneEditorSession.Active.UndoScope( "Delete Edges" ).WithComponentChanges( components ).Push() )
		{
			foreach ( var group in groups )
				group.Key.Mesh.RemoveEdges( group.Select( x => x.Handle ) );
		}
	}

	private bool CanConnect()
	{
		return Edges.Length > 1;
	}

	[Shortcut( "mesh.connect", "V", typeof( SceneViewportWidget ) )]
	private void Connect()
	{
		if ( !CanConnect() )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Connect Edges" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in EdgeGroups )
			{
				var mesh = group.Key.Mesh;
				mesh.ConnectEdges( group.Select( x => x.Handle ).ToArray(), out var newEdges );
				foreach ( var hEdge in newEdges )
					selection.Add( new MeshEdge( group.Key, hEdge ) );

				mesh.ComputeFaceTextureCoordinatesFromParameters();
			}
		}
	}

	private bool CanExtend()
	{
		return Edges.Any( x => x.IsOpen );
	}

	[Shortcut( "mesh.extend", "N", typeof( SceneViewportWidget ) )]
	private void Extend()
	{
		if ( !CanExtend() )
			return;

		using var scope = SceneEditorSession.Scope();

		var amount = EditorScene.GizmoSettings.GridSpacing;

		using ( SceneEditorSession.Active.UndoScope( "Extend Edges" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in EdgeGroups )
			{
				if ( !group.Key.Mesh.ExtendEdges( group.Select( x => x.Handle ).ToArray(), amount, out var newEdges, out _ ) )
					continue;

				if ( newEdges is not null )
				{
					foreach ( var hEdge in newEdges )
					{
						selection.Add( new MeshEdge( group.Key, hEdge ) );
					}
				}
			}
		}
	}

	[Shortcut( "mesh.bridge-edges", "ALT+B", typeof( SceneViewportWidget ) )]
	private void BridgeEdges()
	{
		if ( !CanBridgeEdges() )
			return;

		using var scope = SceneEditorSession.Scope();

		var edgeA = Edges[0];
		var edgeB = Edges[1];

		using ( SceneEditorSession.Active.UndoScope( "Bridge Edges" )
			.WithComponentChanges( [edgeA.Component, edgeB.Component] )
			.Push() )
		{
			if ( edgeA.Component.Mesh.BridgeEdges( edgeA.Handle, edgeB.Handle, out var hFace ) )
			{
				var selection = SceneEditorSession.Active.Selection;
				selection.Clear();
			}
		}
	}

	private bool CanBridgeEdges()
	{
		if ( Edges.Length != 2 )
			return false;

		var edgeA = Edges[0];
		if ( !edgeA.IsValid() )
			return false;

		var edgeB = Edges[1];
		if ( !edgeB.IsValid() )
			return false;

		if ( edgeA.Component != edgeB.Component )
			return false;

		if ( !edgeA.IsOpen )
			return false;

		if ( !edgeB.IsOpen )
			return false;

		return true;
	}

	private bool CanDissolve()
	{
		return Edges.Length != 0;
	}

	[Shortcut( "mesh.dissolve", "Backspace", typeof( SceneViewportWidget ) )]
	private void Dissolve()
	{
		if ( !CanDissolve() )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Dissolve Edges" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in EdgeGroups )
			{
				var mesh = group.Key.Mesh;
				mesh.DissolveEdges( group.Select( x => x.Handle ).ToArray(), false, PolygonMesh.DissolveRemoveVertexCondition.InteriorOrColinear );
				mesh.ComputeFaceTextureCoordinatesFromParameters();
			}
		}
	}

	private bool CanCollapse()
	{
		return Edges.Length != 0;
	}

	[Shortcut( "mesh.collapse", "SHIFT+O", typeof( SceneViewportWidget ) )]
	private void Collapse()
	{
		if ( !CanCollapse() )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Collapse Edges" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in EdgeGroups )
			{
				group.Key.Mesh.CollapseEdges( group.Select( x => x.Handle ).ToArray() );
			}
		}
	}

	private bool CanFillHole()
	{
		return Edges.Any( x => x.IsOpen );
	}

	[Shortcut( "mesh.fill-hole", "P", typeof( SceneViewportWidget ) )]
	private void FillHole()
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Fill Hole" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var edge in Edges )
			{
				edge.Component.Mesh.CreateFaceInEdgeLoop( edge.Handle, out var _ );
			}
		}
	}

	private bool CanSelectRibs()
	{
		return Edges.Length != 0;
	}

	[Shortcut( "mesh.select-ribs", "CTRL+G", typeof( SceneViewportWidget ) )]
	private void SelectRibs()
	{
		if ( !CanSelectRibs() )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Select Edge Ribs" ).Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in EdgeGroups )
			{
				var mesh = group.Key.Mesh;
				if ( mesh is null )
					continue;

				mesh.FindEdgeIslands( group.Select( x => x.Handle ).ToArray(), out var edgeIslands );

				foreach ( var edgeIsland in edgeIslands )
				{
					var numRibs = mesh.FindEdgeRibs( edgeIsland, out var leftEdgeRibs, out var rightEdgeRibs );
					for ( var i = 0; i < numRibs; ++i )
					{
						var leftRib = leftEdgeRibs[i];
						var rightRib = rightEdgeRibs[i];

						foreach ( var rib in leftRib )
							selection.Add( new MeshEdge( group.Key, rib ) );

						foreach ( var rib in rightRib )
							selection.Add( new MeshEdge( group.Key, rib ) );
					}
				}
			}
		}
	}

	private bool CanSelectRing()
	{
		return Edges.Length != 0;
	}

	[Shortcut( "mesh.select-ring", "G", typeof( SceneViewportWidget ) )]
	private void SelectRing()
	{
		if ( !CanSelectRing() )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Select Edge Ring" ).Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var hEdge in Edges )
			{
				if ( !hEdge.IsValid )
					continue;

				hEdge.Component.Mesh.FindEdgeRing( hEdge.Handle, out var edgeRing );
				foreach ( var hNewEdge in edgeRing )
					selection.Add( new MeshEdge( hEdge.Component, hNewEdge ) );
			}
		}
	}

	private bool CanSelectLoop()
	{
		return Edges.Length != 0;
	}

	[Shortcut( "mesh.select-loop", "L", typeof( SceneViewportWidget ) )]
	private void SelectLoop()
	{
		if ( !CanSelectLoop() )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Select Edge Loop" ).Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in EdgeGroups )
			{
				group.Key.Mesh.FindEdgeLoopForEdges( group.Select( x => x.Handle ).ToArray(), out var edgeLoop );
				foreach ( var hNewEdge in edgeLoop )
					selection.Add( new MeshEdge( group.Key, hNewEdge ) );
			}
		}
	}

	[Shortcut( "mesh.snap-edge-to-edge", "I", typeof( SceneViewportWidget ) )]
	private void SnapEdgeToEdge()
	{
		if ( Edges.Length != 2 )
			return;

		var edgeA = Edges[0];
		if ( !edgeA.IsValid() )
			return;

		var edgeB = Edges[1];
		if ( !edgeB.IsValid() )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Snap Edges" )
			.WithComponentChanges( [edgeA.Component, edgeB.Component] )
			.Push() )
		{
			var meshA = edgeA.Component.Mesh;
			var meshB = edgeB.Component.Mesh;

			meshB.GetEdgeVertices( edgeB.Handle, out var hVertexA, out var hVertexB );
			var targetPosA = edgeB.Transform.PointToWorld( meshB.GetVertexPosition( hVertexA ) );
			var targetPosB = edgeB.Transform.PointToWorld( meshB.GetVertexPosition( hVertexB ) );
			var edgeDirB = targetPosB - targetPosA;

			meshA.GetEdgeVertices( edgeA.Handle, out hVertexA, out hVertexB );
			var currentPosA = edgeA.Transform.PointToWorld( meshA.GetVertexPosition( hVertexA ) );
			var currentPosB = edgeA.Transform.PointToWorld( meshA.GetVertexPosition( hVertexB ) );
			var edgeDirA = currentPosB - currentPosA;

			if ( edgeDirA.Dot( edgeDirB ) < 0 )
				(targetPosA, targetPosB) = (targetPosB, targetPosA);

			meshA.SetVertexPosition( hVertexA, edgeA.Transform.PointToLocal( targetPosA ) );
			meshA.SetVertexPosition( hVertexB, edgeA.Transform.PointToLocal( targetPosB ) );
		}
	}

	[Shortcut( "mesh.hard-normals", "H", typeof( SceneViewportWidget ) )]
	void HardNormals()
	{
		SetNormals( PolygonMesh.EdgeSmoothMode.Hard );
	}

	[Shortcut( "mesh.soft-normals", "J", typeof( SceneViewportWidget ) )]
	void SoftNormals()
	{
		SetNormals( PolygonMesh.EdgeSmoothMode.Soft );
	}

	[Shortcut( "mesh.default-normals", "K", typeof( SceneViewportWidget ) )]
	void DefaultNormals()
	{
		SetNormals( PolygonMesh.EdgeSmoothMode.Default );
	}
}
