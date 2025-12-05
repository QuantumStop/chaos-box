using HalfEdgeMesh;
using System.Text.Json.Serialization;

namespace Editor.MeshEditor;

/// <summary>
/// References a vertex handle and the mesh component it belongs to.
/// </summary>
public struct MeshVertex : IMeshElement
{
	[Hide, JsonInclude] public MeshComponent Component { get; private init; }
	[Hide, JsonIgnore] public readonly VertexHandle Handle => Component.IsValid() ? Component.Mesh.VertexHandleFromIndex( HandleIndex ) : default;
	[Hide, JsonInclude] public int HandleIndex { get; set; }

	[Hide, JsonIgnore] public readonly bool IsValid => Component.IsValid() && Handle.IsValid;
	[Hide, JsonIgnore] public readonly Transform Transform => IsValid ? Component.WorldTransform : Transform.Zero;

	[Hide, JsonIgnore] public readonly Vector3 PositionLocal => IsValid ? Component.Mesh.GetVertexPosition( Handle ) : Vector3.Zero;
	[Hide, JsonIgnore] public readonly Vector3 PositionWorld => IsValid ? Transform.PointToWorld( PositionLocal ) : Vector3.Zero;

	public MeshVertex( MeshComponent component, VertexHandle handle )
	{
		Component = component;
		HandleIndex = handle.Index;
	}

	public readonly override int GetHashCode() => HashCode.Combine( Component, nameof( MeshVertex ), Handle );
	public override readonly string ToString() => IsValid ? $"{Component.GameObject.Name} Vertex {Handle}" : "Invalid Vertex";
}

[Inspector( typeof( MeshVertex ) )]
public class VertexInspector : InspectorWidget
{
	private readonly MeshVertex[] Vertices;
	private readonly List<IGrouping<MeshComponent, MeshVertex>> VertexGroups;
	private readonly List<MeshComponent> Components;
	private readonly Layout ContentLayout;

	public enum MergeRange
	{
		Infinite,
		Grid,
		Fixed,
	}

	private static MergeRange MergeRangeMode { get; set; } = MergeRange.Infinite;
	private static float MergeDistance { get; set; } = 0.1f;

	private struct MergeProperties
	{
		[Group( "Merge" )]
		public readonly MergeRange Range { get => MergeRangeMode; set => MergeRangeMode = value; }

		[Group( "Merge" ), ShowIf( nameof( Range ), MergeRange.Fixed )]
		public readonly float Distance { get => MergeDistance; set => MergeDistance = value; }
	}

	[InlineEditor( Label = false )]
	private readonly MergeProperties mergeProperties = new();

	public VertexInspector( SerializedObject so ) : base( so )
	{
		Vertices = SerializedObject.Targets
			.OfType<MeshVertex>()
			.ToArray();

		VertexGroups = Vertices.GroupBy( x => x.Component ).ToList();
		Components = VertexGroups.Select( x => x.Key ).ToList();

		Layout = Layout.Column();

		var sheet = new ControlSheet();
		sheet.AddRow( this.GetSerialized().GetProperty( nameof( mergeProperties ) ) );
		Layout.Add( sheet );

		ContentLayout = Layout.AddColumn();
		ContentLayout.Spacing = 8;
		ContentLayout.Margin = 8;

		CreateButton( "Merge", "mesh.merge", Merge, Vertices.Length > 1 );
		CreateButton( "Snap To Vertex", "mesh.snap_to_vertex", SnapToVertex, Vertices.Length > 1 );
		CreateButton( "Weld UVs", "mesh.vertex-weld-uvs", WeldUVs, Vertices.Length > 0 );
		CreateButton( "Bevel", "mesh.bevel", Bevel, Vertices.Length > 0 );
		CreateButton( "Connect", "mesh.connect", Connect, Vertices.Length > 1 );

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

	[Shortcut( "mesh.connect", "V", typeof( SceneViewportWidget ) )]
	private void Connect()
	{
		if ( Vertices.Length < 2 )
			return;

		using var scope = SceneEditorSession.Scope();

		var pairs = new Dictionary<PolygonMesh, List<(VertexHandle, VertexHandle)>>();

		using ( SceneEditorSession.Active.UndoScope( "Connect Vertices" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var group in VertexGroups )
			{
				var mesh = group.Key.Mesh;
				pairs[mesh] = [];

				foreach ( var hVertex in group )
				{
					mesh.GetFacesConnectedToVertex( hVertex.Handle, out var connectedFaces );

					foreach ( var hFace in connectedFaces )
					{
						var hFaceVertex = mesh.FindFaceVertexConnectedToVertex( hVertex.Handle, hFace );
						var hNextFaceVertex = mesh.GetNextVertexInFace( hFaceVertex );

						while ( hNextFaceVertex != hFaceVertex )
						{
							var hNextVertex = mesh.GetVertexConnectedToFaceVertex( hNextFaceVertex );

							if ( Vertices.FirstOrDefault( x => x.Handle == hNextVertex ).IsValid() )
							{
								pairs[mesh].Add( (hVertex.Handle, hNextVertex) );
								break;
							}

							hNextFaceVertex = mesh.GetNextVertexInFace( hNextFaceVertex );
						}
					}
				}
			}

			foreach ( var group in VertexGroups )
			{
				var mesh = group.Key.Mesh;
				var vertexPairs = pairs[mesh];
				var numPairs = vertexPairs.Count;

				if ( vertexPairs.Count == 0 )
					continue;

				foreach ( var pair in vertexPairs )
				{
					mesh.ConnectVertices( pair.Item1, pair.Item2, out _ );
				}

				mesh.ComputeFaceTextureCoordinatesFromParameters();
			}
		}
	}

	[Shortcut( "mesh.snap_to_vertex", "B", typeof( SceneViewportWidget ) )]
	private void SnapToVertex()
	{
		if ( Vertices.Length < 2 )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Snap Vertices" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var position = Vertices[^1].PositionWorld;
			foreach ( var vertex in Vertices )
				vertex.Component.Mesh.SetVertexPosition( vertex.Handle, vertex.Transform.PointToLocal( position ) );
		}
	}

	[Shortcut( "mesh.vertex-weld-uvs", "CTRL+F", typeof( SceneViewportWidget ) )]
	private void WeldUVs()
	{
		if ( Vertices.Length < 1 )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Weld UVs" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var group in VertexGroups )
			{
				var component = group.Key;
				var mesh = component.Mesh;
				mesh.AverageVertexUVs( group.Select( x => x.Handle ).ToList() );
			}
		}
	}

	[Shortcut( "mesh.bevel", "F", typeof( SceneViewportWidget ) )]
	private void Bevel()
	{
		if ( Vertices.Length <= 0 )
			return;

		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Bevel Vertices" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var bevelWidth = EditorScene.GizmoSettings.GridSpacing;

			foreach ( var group in VertexGroups )
			{
				if ( !group.Key.Mesh.BevelVertices( group.Select( x => x.Handle ).ToArray(), bevelWidth, out var newVertices ) )
					continue;

				var selection = SceneEditorSession.Active.Selection;
				selection.Clear();

				foreach ( var hVertex in newVertices )
					selection.Add( new MeshVertex( group.Key, hVertex ) );
			}
		}
	}

	[Shortcut( "mesh.merge", "M", typeof( SceneViewportWidget ) )]
	private void Merge()
	{
		if ( Vertices.Length < 2 )
			return;

		using var scope = SceneEditorSession.Scope();

		var gameObjects = Components.Skip( 1 ).Select( x => x.GameObject ).ToList();
		var meshA = Components[0];
		var vertices = VertexGroups[0].ToList();

		using ( SceneEditorSession.Active.UndoScope( "Merge Vertices" )
			.WithComponentChanges( Components )
			.WithGameObjectDestructions( gameObjects )
			.Push() )
		{
			foreach ( var group in VertexGroups.Skip( 1 ) )
			{
				var meshB = group.Key;
				var transform = meshA.WorldTransform.ToLocal( meshB.WorldTransform );
				meshA.Mesh.MergeMesh( meshB.Mesh, transform, out var remapVertices, out _, out _ );
				vertices.AddRange( group.Select( v => new MeshVertex( meshA, remapVertices[v.Handle] ) ) );

				meshB.DestroyGameObject();
			}

			var mergeDistance = MergeRangeMode switch
			{
				MergeRange.Grid => EditorScene.GizmoSettings.GridSpacing,
				MergeRange.Fixed => MergeDistance,
				_ => -1.0f
			};

			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			if ( meshA.Mesh.MergeVerticesWithinDistance( vertices.Select( x => x.Handle ).ToList(), mergeDistance, true, false, out var finalVertices ) > 0 )
			{
				foreach ( var hVertex in finalVertices )
					selection.Add( new MeshVertex( meshA, hVertex ) );

				meshA.Mesh.ComputeFaceTextureCoordinatesFromParameters();
			}
			else
			{
				foreach ( var hVertex in vertices )
					selection.Add( hVertex );
			}
		}
	}

	[Shortcut( "editor.delete", "DEL", typeof( SceneViewportWidget ) )]
	private void DeleteSelection()
	{
		var groups = Vertices.GroupBy( face => face.Component );

		if ( !groups.Any() )
			return;

		var components = groups.Select( x => x.Key ).ToArray();

		using ( SceneEditorSession.Active.UndoScope( "Delete Vertices" ).WithComponentChanges( components ).Push() )
		{
			foreach ( var group in groups )
				group.Key.Mesh.RemoveVertices( group.Select( x => x.Handle ) );
		}
	}
}
