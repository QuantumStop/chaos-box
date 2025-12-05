using HalfEdgeMesh;
using Sandbox.UI;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Editor.MeshEditor;

/// <summary>
/// References a face handle and the mesh component it belongs to.
/// </summary>
public struct MeshFace : IMeshElement
{
	[Hide, JsonInclude] public MeshComponent Component { get; private set; }
	[Hide, JsonIgnore] public readonly FaceHandle Handle => Component.IsValid() ? Component.Mesh.FaceHandleFromIndex( HandleIndex ) : default;
	[Hide, JsonInclude] public int HandleIndex { get; set; }

	[Hide, JsonIgnore] public readonly bool IsValid => Component.IsValid() && Handle.IsValid;
	[Hide, JsonIgnore] public readonly Transform Transform => IsValid ? Component.WorldTransform : Transform.Zero;

	[Hide, JsonIgnore] public readonly Vector3 Center => IsValid ? Component.Mesh.GetFaceCenter( Handle ) : Vector3.Zero;

	public MeshFace( MeshComponent component, FaceHandle handle )
	{
		Component = component;
		HandleIndex = handle.Index;
	}

	public readonly override int GetHashCode() => HashCode.Combine( Component, nameof( MeshFace ), Handle );
	public override readonly string ToString() => IsValid ? $"{Component.GameObject.Name} Face {Handle}" : "Invalid Face";

	[JsonIgnore]
	[Title( "Shift" ), Group( "Texture State" )]
	public readonly Vector2 TextureOffset
	{
		get => IsValid ? Component.Mesh.GetTextureOffset( Handle ) : default;
		set => Component?.Mesh.SetTextureOffset( Handle, value );
	}

	[JsonIgnore]
	[Title( "Scale" ), Group( "Texture State" )]
	public readonly Vector2 TextureScale
	{
		get => IsValid ? Component.Mesh.GetTextureScale( Handle ) : default;
		set => Component?.Mesh.SetTextureScale( Handle, value );
	}

	[JsonIgnore]
	[Group( "Texture State" )]
	public readonly Material Material
	{
		get => IsValid ? Component.Mesh.GetFaceMaterial( Handle ) : default;
		set => Component?.Mesh.SetFaceMaterial( Handle, value );
	}

	[Hide]
	public readonly MeshVertex GetClosestVertex( Vector2 point, float maxDistance )
	{
		if ( !IsValid )
			return default;

		var transform = Transform;
		var minDistance = maxDistance;
		var closestVertex = VertexHandle.Invalid;

		foreach ( var vertex in Component.Mesh.GetFaceVertices( Handle ) )
		{
			var vertexPosition = transform.PointToWorld( Component.Mesh.GetVertexPosition( vertex ) );
			var vertexCoord = Gizmo.Camera.ToScreen( vertexPosition );
			var distance = vertexCoord.Distance( point );
			if ( distance < minDistance )
			{
				minDistance = distance;
				closestVertex = vertex;
			}
		}

		return new MeshVertex( Component, closestVertex );
	}

	[Hide]
	public readonly MeshEdge GetClosestEdge( Vector3 position, Vector2 point, float maxDistance )
	{
		if ( !IsValid )
			return default;

		if ( !Component.Mesh.GetFaceVerticesConnectedToFace( Handle, out var hEdges ) )
			return default;

		var transform = Transform;
		var minDistance = maxDistance;
		var hClosestEdge = HalfEdgeHandle.Invalid;

		foreach ( var hEdge in hEdges )
		{
			var line = Component.Mesh.GetEdgeLine( hEdge );
			line = new Line( transform.PointToWorld( line.Start ), transform.PointToWorld( line.End ) );
			var closestPoint = line.ClosestPoint( position );
			var pointCoord = Gizmo.Camera.ToScreen( closestPoint );
			var distance = pointCoord.Distance( point );
			if ( distance < minDistance )
			{
				minDistance = distance;
				hClosestEdge = hEdge;
			}
		}

		if ( !hClosestEdge.IsValid )
			return default;

		var hOppositeEdge = Component.Mesh.GetOppositeHalfEdge( hClosestEdge );
		if ( hClosestEdge.Index < hOppositeEdge.Index )
		{
			return new MeshEdge( Component, hClosestEdge );
		}
		else
		{
			return new MeshEdge( Component, hOppositeEdge );
		}
	}
}

[Inspector( typeof( MeshFace ) )]
public class FaceInspector : InspectorWidget
{
	private readonly MeshFace[] Faces;
	private readonly List<IGrouping<MeshComponent, MeshFace>> FaceGroups;
	private readonly List<MeshComponent> Components;

	[Range( 0, 128, slider: false )]
	private int FitX = 1;

	[Range( 0, 128, slider: false )]
	private int FitY = 1;

	private bool TreatAsOne = false;

	Widget OperationsBody;

	[Range( 0, 64, slider: false ), Step( 1 )]
	private Vector2Int NumCuts = 1;

	private class TextureModifyButton : Widget
	{
		private readonly string Icon;

		public TextureModifyButton( Widget parent, string tooltip, string icon, Action onClick ) : base( parent )
		{
			Icon = icon;
			ToolTip = tooltip;
			FixedSize = 26;
			Cursor = CursorShape.Finger;
			MouseClick = onClick;
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			Paint.ClearPen();
			var bg = Theme.ControlBackground;
			if ( Paint.HasMouseOver )
				bg = bg.Lighten( 0.3f );
			Paint.SetBrush( bg );
			Paint.DrawRect( LocalRect, Theme.ControlRadius );
			Paint.Draw( LocalRect.Shrink( 3 ), Icon );
		}
	}

	private class GroupHeader : Widget
	{
		readonly Layout toggleLayout;

		public GroupHeader( Widget parent, string name ) : base( parent )
		{
			Title = name;
			FixedHeight = Theme.RowHeight;
			VerticalSizeMode = SizeMode.CanGrow;
			HorizontalSizeMode = SizeMode.Flexible;
			Layout = Layout.Row();
			Layout.Spacing = 5;
			Layout.Margin = new Margin( 20, 0, 0, 0 );

			toggleLayout = Layout.AddColumn();

			Layout.AddStretchCell();

			State = ProjectCookie.Get( $"{Title}.state", true );
		}

		public string Title { get; set; }

		protected override void OnMousePress( MouseEvent e )
		{
			base.OnMousePress( e );

			if ( e.Button == MouseButtons.Left )
			{
				Toggle();
			}
		}

		protected override void OnDoubleClick( MouseEvent e )
		{
			e.Accepted = false;
		}

		protected override void OnPaint()
		{
			bool isChecked = true;

			{
				Paint.ClearPen();
				Paint.SetBrush( Color.Black.WithAlpha( 0.16f ) );
				Paint.DrawRect( LocalRect );
			}

			float spacing = 0;

			Paint.ClearBrush();
			Paint.Pen = Theme.Text.WithAlpha( Paint.HasMouseOver ? 0.7f : (isChecked ? 0.6f : 0.3f) );

			Paint.SetDefaultFont( 10, weight: 500, sizeInPixels: true );
			Paint.DrawText( LocalRect.Shrink( toggleLayout.OuterRect.Right + spacing, 0, 0, 0 ), Title, TextFlag.LeftCenter );

			Paint.DrawIcon( LocalRect.Shrink( 0, 0 ), State ? "arrow_drop_down" : "arrow_right", 16, TextFlag.LeftCenter );
		}

		public bool State { get; private set; } = true;
		public Action<bool> OnToggled;

		public void Toggle()
		{
			State = !State;
			OnToggled?.Invoke( State );
			ProjectCookie.Set( $"{Title}.state", State );
		}
	}

	public FaceInspector( SerializedObject so ) : base( so )
	{
		Faces = SerializedObject.Targets
			.OfType<MeshFace>()
			.ToArray();

		FaceGroups = Faces.GroupBy( x => x.Component ).ToList();
		Components = FaceGroups.Select( x => x.Key ).ToList();

		Layout = Layout.Column();

		var sheet = new ControlSheet();
		Layout.Add( sheet );

		sheet.AddObject( so );

		{
			var header = Layout.Add( new GroupHeader( this, "Modify Texture" ) );

			var body = new Widget
			{
				VerticalSizeMode = SizeMode.CanGrow,
				HorizontalSizeMode = SizeMode.Flexible,
			};

			var modifyLayout = Layout.Grid();
			body.Layout = modifyLayout;

			modifyLayout.Spacing = 6;
			modifyLayout.Margin = new Margin( 20, 6, 20, 6 );
			modifyLayout.AddCell( 0, 0, new Label.Small( "Align:" ) );
			modifyLayout.AddCell( 1, 0, new TextureModifyButton( this, "Align to Grid", "hammer/texture_align_grid.png", AlignToGrid ) );
			modifyLayout.AddCell( 2, 0, new TextureModifyButton( this, "Align to Face", "hammer/texture_align_face.png", AlignToFace ) );
			modifyLayout.AddCell( 3, 0, new TextureModifyButton( this, "Align to View", "hammer/texture_align_view.png", AlignToView ) );
			modifyLayout.SetColumnStretch( 0, 0, 0, 0, 0, 0, 1 );

			modifyLayout.AddCell( 0, 1, new Label.Small( "Scale:" ) );
			modifyLayout.AddCell( 1, 1, new TextureModifyButton( this, "Scale X Up", "hammer/texture_scale_up_x.png", () => DoScaleX( true ) ) );
			modifyLayout.AddCell( 2, 1, new TextureModifyButton( this, "Scale X Down", "hammer/texture_scale_dn_x.png", () => DoScaleX( false ) ) );
			modifyLayout.AddCell( 3, 1, new TextureModifyButton( this, "Scale Y Up", "hammer/texture_scale_up_y.png", () => DoScaleY( true ) ) );
			modifyLayout.AddCell( 4, 1, new TextureModifyButton( this, "Scale Y Down", "hammer/texture_scale_dn_y.png", () => DoScaleY( false ) ) );

			modifyLayout.AddCell( 0, 2, new Label.Small( "Shift:" ) );
			modifyLayout.AddCell( 1, 2, new TextureModifyButton( this, "Shift Left", "hammer/texture_shift_left.png", () => DoShiftX( true ) ) );
			modifyLayout.AddCell( 2, 2, new TextureModifyButton( this, "Shift Right", "hammer/texture_shift_right.png", () => DoShiftX( false ) ) );
			modifyLayout.AddCell( 3, 2, new TextureModifyButton( this, "Shift Up", "hammer/texture_shift_up.png", () => DoShiftY( true ) ) );
			modifyLayout.AddCell( 4, 2, new TextureModifyButton( this, "Shift Down", "hammer/texture_shift_down.png", () => DoShiftY( false ) ) );

			modifyLayout.AddCell( 0, 3, new Label.Small( "Rotate:" ) );
			modifyLayout.AddCell( 1, 3, new TextureModifyButton( this, "Rotate CW", "hammer/texture_rotate_cw.png", () => DoRotate( true ) ) );
			modifyLayout.AddCell( 2, 3, new TextureModifyButton( this, "Rotate CCW", "hammer/texture_rotate_ccw.png", () => DoRotate( false ) ) );

			modifyLayout.AddCell( 0, 4, new Label.Small( "Fit:" ) );
			modifyLayout.AddCell( 1, 4, new TextureModifyButton( this, "Fit Both", "hammer/texture_fit_both.png", () => DoFit( FitX, FitY ) ) );
			modifyLayout.AddCell( 2, 4, new TextureModifyButton( this, "Fit X", "hammer/texture_fit_x.png", () => DoFit( FitX, -1 ) ) );
			modifyLayout.AddCell( 3, 4, new TextureModifyButton( this, "Fit Y", "hammer/texture_fit_y.png", () => DoFit( -1, FitY ) ) );

			{
				var cs = new ControlSheet();
				cs.AddProperty( this, x => x.FitX );
				cs.AddProperty( this, x => x.FitY );
				modifyLayout.AddCell( 4, 4, cs, 3, 1 );
			}

			modifyLayout.AddCell( 0, 5, new Label.Small( "Justify:" ) );
			modifyLayout.AddCell( 1, 5, new TextureModifyButton( this, "Justify Left", "hammer/texture_justify_l.png", () => DoJustify( PolygonMesh.TextureJustification.Left ) ) );
			modifyLayout.AddCell( 2, 5, new TextureModifyButton( this, "Justify Right", "hammer/texture_justify_r.png", () => DoJustify( PolygonMesh.TextureJustification.Right ) ) );
			modifyLayout.AddCell( 3, 5, new TextureModifyButton( this, "Justify Top", "hammer/texture_justify_t.png", () => DoJustify( PolygonMesh.TextureJustification.Top ) ) );
			modifyLayout.AddCell( 4, 5, new TextureModifyButton( this, "Justify Bottom", "hammer/texture_justify_b.png", () => DoJustify( PolygonMesh.TextureJustification.Bottom ) ) );
			modifyLayout.AddCell( 5, 5, new TextureModifyButton( this, "Justify Center", "hammer/texture_justify_c.png", () => DoJustify( PolygonMesh.TextureJustification.Center ) ) );


			{
				var cs = new ControlSheet();
				cs.AddProperty( this, x => x.TreatAsOne );
				modifyLayout.AddCell( 0, 6, cs, 6, 1, TextFlag.Center );
			}

			if ( !header.State )
			{
				body.Hide();
			}
			Layout.Add( body );

			header.OnToggled += ( s ) =>
			{
				using var x = SuspendUpdates.For( Parent );

				if ( !s )
				{
					body.Hide();
				}
				else
				{
					body.Show();
				}

				UpdateGeometry();
				Parent?.UpdateGeometry();
			};
		}

		{
			var header = Layout.Add( new GroupHeader( this, "Operations" ) );

			OperationsBody = new Widget
			{
				VerticalSizeMode = SizeMode.CanGrow,
				HorizontalSizeMode = SizeMode.Flexible,
				Layout = Layout.Column()
			};

			OperationsBody.Layout.Spacing = 6;
			OperationsBody.Layout.Margin = 6;

			CreateButton( "Extract Faces", "mesh.extract-faces", ExtractFaces );
			CreateButton( "Detach Faces", "mesh.detach-faces", DetachFaces );
			CreateButton( "Combine Faces", "mesh.combine-faces", CombineFaces );
			CreateButton( "Collapse Faces", "mesh.collapse", Collapse );
			CreateButton( "Remove Bad Faces", "mesh.remove-bad-faces", RemoveBadFaces );
			CreateButton( "Flip All Faces", "mesh.flip-all-faces", FlipAllFaces );

			OperationsBody.Layout.AddSeparator();
			OperationsBody.Layout.Add( ControlWidget.Create( this.GetSerialized().GetProperty( nameof( NumCuts ) ) ) );

			CreateButton( "Slice", "mesh.quad-slice", QuadSlice, Faces.Length > 0 );

			if ( !header.State )
			{
				OperationsBody.Hide();
			}

			Layout.Add( OperationsBody );

			header.OnToggled += ( s ) =>
			{
				using var x = SuspendUpdates.For( Parent );

				if ( !s )
				{
					OperationsBody.Hide();
				}
				else
				{
					OperationsBody.Show();
				}

				UpdateGeometry();
				Parent?.UpdateGeometry();
			};
		}

		Layout.AddStretchCell();
	}

	void CreateButton( string text, string keybind, Action clicked, bool enabled = true )
	{
		var btn = OperationsBody.Layout.Add( new Button.Primary( text, this )
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

	[Shortcut( "mesh.collapse", "SHIFT+O", typeof( SceneViewportWidget ) )]
	private void Collapse()
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Collapse Faces" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var hFace in Faces )
			{
				if ( !hFace.IsValid )
					continue;

				hFace.Component.Mesh.CollapseFace( hFace.Handle, out _ );
			}
		}
	}

	[Shortcut( "mesh.remove-bad-faces", "", typeof( SceneViewportWidget ) )]
	private void RemoveBadFaces()
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Remove Bad Faces" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var component in Components )
			{
				component.Mesh.RemoveBadFaces();
			}
		}
	}

	[Shortcut( "editor.delete", "DEL", typeof( SceneViewportWidget ) )]
	private void DeleteSelection()
	{
		var groups = Faces.GroupBy( face => face.Component );

		if ( !groups.Any() )
			return;

		var components = groups.Select( x => x.Key ).ToArray();

		using ( SceneEditorSession.Active.UndoScope( "Delete Faces" ).WithComponentChanges( components ).Push() )
		{
			foreach ( var group in groups )
				group.Key.Mesh.RemoveFaces( group.Select( x => x.Handle ) );
		}
	}

	private void AlignToGrid()
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Align to Grid" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var face in Faces )
			{
				face.Component.Mesh.TextureAlignToGrid( face.Transform, face.Handle );
			}
		}
	}

	private void AlignToFace()
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Align to Face" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var face in Faces )
			{
				face.Component.Mesh.TextureAlignToFace( face.Transform, face.Handle );
			}
		}
	}

	private void AlignToView()
	{
		var sceneView = SceneViewportWidget.LastSelected;
		if ( !sceneView.IsValid() )
			return;

		using var scope = SceneEditorSession.Scope();

		var position = sceneView.State.CameraPosition;
		var rotation = sceneView.State.CameraRotation;
		var uAxis = rotation.Right;
		var vAxis = rotation.Up;
		var offset = new Vector2( uAxis.Dot( position ), vAxis.Dot( position ) );

		using ( SceneEditorSession.Active.UndoScope( "Align to View" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var face in Faces )
			{
				face.Component.Mesh.SetFaceTextureParameters( face.Handle, offset, uAxis, vAxis );
			}
		}
	}

	private void DoRotate( bool clockwise )
	{
		using var scope = SceneEditorSession.Scope();

		var amount = EditorScene.GizmoSettings.AngleSpacing * (clockwise ? 1 : -1);

		using ( SceneEditorSession.Active.UndoScope( "Rotate" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var face in Faces )
			{
				var mesh = face.Component.Mesh;
				mesh.GetFaceTextureParameters( face.Handle, out var axisU, out var axisV, out var scale );

				Vector3 newAxisU = (Vector3)axisU;
				Vector3 newAxisV = (Vector3)axisV;
				var axis = Vector3.Cross( newAxisU, newAxisV );
				axis = axis.Normal;

				var rotation = Rotation.FromAxis( axis, amount );
				newAxisU *= rotation;
				newAxisV *= rotation;
				newAxisU = newAxisU.Normal;
				newAxisV = newAxisV.Normal;

				mesh.SetFaceTextureParameters( face.Handle, new Vector4( newAxisU, axisU.w ), new Vector4( newAxisV, axisV.w ), scale );
			}
		}
	}

	private void DoShiftX( bool positive )
	{
		using var scope = SceneEditorSession.Scope();

		var gridSpacing = EditorScene.GizmoSettings.GridSpacing;

		using ( SceneEditorSession.Active.UndoScope( "Shift X" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var face in Faces )
			{
				var mesh = face.Component.Mesh;
				var scale = mesh.GetTextureScale( face.Handle ).x;
				scale = scale.AlmostEqual( 0.0f ) ? 0.25f : scale;
				var amount = gridSpacing / scale;
				var offset = mesh.GetTextureOffset( face.Handle );
				offset = offset.WithX( offset.x + amount * (positive ? 1.0f : -1.0f) );
				mesh.SetTextureOffset( face.Handle, offset );
			}
		}
	}

	private void DoShiftY( bool positive )
	{
		using var scope = SceneEditorSession.Scope();

		var gridSpacing = EditorScene.GizmoSettings.GridSpacing;

		using ( SceneEditorSession.Active.UndoScope( "Shift Y" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var face in Faces )
			{
				var mesh = face.Component.Mesh;
				var scale = mesh.GetTextureScale( face.Handle ).y;
				scale = scale.AlmostEqual( 0.0f ) ? 0.25f : scale;
				var amount = gridSpacing / scale;
				var offset = mesh.GetTextureOffset( face.Handle );
				offset = offset.WithY( offset.y + amount * (positive ? 1.0f : -1.0f) );
				mesh.SetTextureOffset( face.Handle, offset );
			}
		}
	}

	private void DoScaleX( bool positive )
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Scale X" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var face in Faces )
			{
				var mesh = face.Component.Mesh;
				var scale = mesh.GetTextureScale( face.Handle );
				scale = scale.WithX( scale.x * (positive ? 2.0f : 0.5f) );
				mesh.SetTextureScale( face.Handle, scale );
			}
		}
	}

	private void DoScaleY( bool positive )
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Scale Y" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var face in Faces )
			{
				var mesh = face.Component.Mesh;
				var scale = mesh.GetTextureScale( face.Handle );
				scale = scale.WithY( scale.y * (positive ? 2.0f : 0.5f) );
				mesh.SetTextureScale( face.Handle, scale );
			}
		}
	}

	private void DoJustify( PolygonMesh.TextureJustification justification )
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Justify" )
			.WithComponentChanges( Components )
			.Push() )
		{
			JustifyTexturesForFaceSelection( justification );

			foreach ( var group in FaceGroups )
			{
				var mesh = group.Key.Mesh;
				mesh.ComputeFaceTextureCoordinatesFromParameters( group.Select( x => x.Handle ) );
			}
		}
	}

	private void DoFit( int repeatX, int repeatY )
	{
		using var scope = SceneEditorSession.Scope();

		var justification = PolygonMesh.TextureJustification.Fit;
		if ( repeatX == -1 ) justification = PolygonMesh.TextureJustification.FitY;
		else if ( repeatY == -1 ) justification = PolygonMesh.TextureJustification.FitX;

		using ( SceneEditorSession.Active.UndoScope( "Fit" )
			.WithComponentChanges( Components )
			.Push() )
		{
			JustifyTexturesForFaceSelection( justification );

			if ( repeatX > 0 || repeatY > 0 )
			{
				foreach ( var face in Faces )
				{
					var mesh = face.Component.Mesh;
					var scale = mesh.GetTextureScale( face.Handle );

					if ( repeatX > 0 )
						scale.x /= repeatX;

					if ( repeatY > 0 )
						scale.y /= repeatY;

					mesh.SetTextureScale( face.Handle, scale );
				}
			}

			if ( repeatX != -1 )
				JustifyTexturesForFaceSelection( PolygonMesh.TextureJustification.Left );

			if ( repeatY != -1 )
				JustifyTexturesForFaceSelection( PolygonMesh.TextureJustification.Top );

			foreach ( var group in FaceGroups )
			{
				var mesh = group.Key.Mesh;
				mesh.ComputeFaceTextureCoordinatesFromParameters( group.Select( x => x.Handle ) );
			}
		}
	}

	private void JustifyTexturesForFaceSelection( PolygonMesh.TextureJustification justification )
	{
		PolygonMesh.FaceExtents extents = null;

		if ( TreatAsOne )
		{
			extents = new PolygonMesh.FaceExtents();

			foreach ( var group in FaceGroups )
			{
				var mesh = group.Key.Mesh;
				mesh.UnionExtentsForFaces( group.Select( x => x.Handle ), mesh.Transform, extents );
			}
		}

		foreach ( var group in FaceGroups )
		{
			var mesh = group.Key.Mesh;
			mesh.JustifyFaceTextureParameters( group.Select( x => x.Handle ), justification, extents );
		}
	}

	[Shortcut( "mesh.extract-faces", "ALT+N", typeof( SceneViewportWidget ) )]
	private void ExtractFaces()
	{
		using var scope = SceneEditorSession.Scope();

		var options = new GameObject.SerializeOptions();
		var gameObjects = Components.Select( x => x.GameObject );

		using ( SceneEditorSession.Active.UndoScope( "Extract Faces" )
			.WithComponentChanges( Components )
			.WithGameObjectDestructions( gameObjects )
			.WithGameObjectCreations()
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in FaceGroups )
			{
				var entry = group.Key.GameObject;
				var json = group.Key.Serialize( options );
				SceneUtility.MakeIdGuidsUnique( json as JsonObject );

				var go = new GameObject( entry.Name );
				go.WorldTransform = entry.WorldTransform;
				go.MakeNameUnique();

				entry.AddSibling( go, false );

				var newMeshComponent = go.Components.Create<MeshComponent>( true );
				newMeshComponent.DeserializeImmediately( json as JsonObject );
				var newMesh = newMeshComponent.Mesh;

				var faceIndices = group.Select( x => x.Handle.Index ).ToArray();
				var facesToRemove = newMesh.FaceHandles
					.Where( f => !faceIndices.Contains( f.Index ) )
					.ToArray();

				newMesh.RemoveFaces( facesToRemove );

				var transform = go.WorldTransform;
				var newBounds = newMesh.CalculateBounds( transform );
				var newTransfrom = transform.WithPosition( newBounds.Center );
				newMesh.ApplyTransform( new Transform( transform.Rotation.Inverse * (transform.Position - newTransfrom.Position) ) );
				go.WorldTransform = newTransfrom;
				newMeshComponent.RebuildMesh();

				foreach ( var hFace in newMesh.FaceHandles )
					selection.Add( new MeshFace( newMeshComponent, hFace ) );

				var mesh = group.Key.Mesh;
				var faces = group.Select( x => x.Handle );

				if ( faces.Count() == mesh.FaceHandles.Count() )
				{
					entry.Destroy();
				}
				else
				{
					mesh.RemoveFaces( faces );
				}
			}
		}
	}

	[Shortcut( "mesh.detach-faces", "N", typeof( SceneViewportWidget ) )]
	private void DetachFaces()
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Detach Faces" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in FaceGroups )
			{
				group.Key.Mesh.DetachFaces( group.Select( x => x.Handle ).ToArray(), out var newFaces );
				foreach ( var hFace in newFaces )
					selection.Add( new MeshFace( group.Key, hFace ) );
			}
		}
	}

	[Shortcut( "mesh.combine-faces", "Backspace", typeof( SceneViewportWidget ) )]
	private void CombineFaces()
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Combine Faces" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in FaceGroups )
			{
				var mesh = group.Key.Mesh;
				mesh.CombineFaces( group.Select( x => x.Handle ).ToArray() );
				mesh.ComputeFaceTextureCoordinatesFromParameters();
			}
		}
	}

	[Shortcut( "mesh.flip-all-faces", "F", typeof( SceneViewportWidget ) )]
	private void FlipAllFaces()
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Flip All Faces" )
			.WithComponentChanges( Components )
			.Push() )
		{
			foreach ( var component in Components )
			{
				component.Mesh.FlipAllFaces();
			}
		}
	}

	[Shortcut( "mesh.quad-slice", "CTRL+D", typeof( SceneViewportWidget ) )]
	private void QuadSlice()
	{
		using var scope = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Quad Slice" )
			.WithComponentChanges( Components )
			.Push() )
		{
			var selection = SceneEditorSession.Active.Selection;
			selection.Clear();

			foreach ( var group in FaceGroups )
			{
				var mesh = group.Key.Mesh;
				var newFaces = new List<FaceHandle>();
				mesh.QuadSliceFaces( group.Select( x => x.Handle ).ToArray(), NumCuts.x, NumCuts.y, 60.0f, newFaces );
				mesh.ComputeFaceTextureCoordinatesFromParameters(); // TODO: Shouldn't be needed, something in quad slice isn't computing these

				foreach ( var hFace in newFaces )
				{
					selection.Add( new MeshFace( group.Key, hFace ) );
				}
			}
		}
	}
}
