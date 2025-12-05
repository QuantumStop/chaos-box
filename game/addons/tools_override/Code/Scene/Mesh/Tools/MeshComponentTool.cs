using HalfEdgeMesh;

namespace Editor.MeshEditor;

public class MeshComponentTool : EditorTool<MeshComponent>
{
	private MeshComponent _component;
	private BBox _startBox;
	private BBox _deltaBox;
	private BBox _startDeltaBox;
	private BBox _box;
	private bool _dragging;
	private Transform _startTransform;
	private readonly Dictionary<VertexHandle, Vector3> _vertices = new();
	private IDisposable _undoScope;

	public override void OnSelectionChanged()
	{
		_component = GetSelectedComponent<MeshComponent>();

		Reset();
	}

	private void Reset()
	{
		_dragging = false;
		_deltaBox = default;
		_startDeltaBox = default;
		_vertices.Clear();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();

		if ( !_component.IsValid() )
			return;

		if ( Manager.CurrentSubTool is not null )
			return;

		if ( !_dragging )
		{
			_box = _component.Mesh.CalculateBounds();
			_startTransform = _component.WorldTransform;
		}

		using ( Gizmo.Scope( "Tool", _startTransform ) )
		{
			Gizmo.Hitbox.DepthBias = 0.01f;

			if ( !Gizmo.Pressed.Any && Gizmo.HasMouseFocus )
			{
				Reset();
			}

			using ( Gizmo.Scope( "Box" ) )
			{
				var textSize = 22 * Gizmo.Settings.GizmoScale * Application.DpiScale;

				Gizmo.Draw.IgnoreDepth = true;
				Gizmo.Draw.LineThickness = 2;
				Gizmo.Draw.Color = Gizmo.Colors.Active;
				Gizmo.Draw.LineBBox( _box );

				var l = _startTransform.PointToWorld( _box.Maxs.WithY( _box.Center.y ) );
				var w = _startTransform.PointToWorld( _box.Maxs.WithX( _box.Center.x ) );
				var h = _startTransform.PointToWorld( _box.Maxs.WithZ( _box.Center.z ) );

				Gizmo.Draw.Color = Gizmo.Colors.Left;
				Gizmo.Draw.ScreenText( $"L: {_box.Size.y:0.#}", l, Vector2.Up * 32, size: textSize );
				Gizmo.Draw.Color = Gizmo.Colors.Forward;
				Gizmo.Draw.ScreenText( $"W: {_box.Size.x:0.#}", w, Vector2.Up * 32, size: textSize );
				Gizmo.Draw.Color = Gizmo.Colors.Up;
				Gizmo.Draw.ScreenText( $"H: {_box.Size.z:0.#}", h, Vector2.Up * 32, size: textSize );
			}

			if ( Gizmo.Control.BoundingBox( "Resize", _box, out var outBox ) )
			{
				_undoScope ??= SceneEditorSession.Active.UndoScope( "Resize Mesh" )
					.WithGameObjectChanges( _component.GameObject, GameObjectUndoFlags.Properties )
					.WithComponentChanges( _component ).Push();

				_deltaBox.Maxs += outBox.Maxs - _box.Maxs;
				_deltaBox.Mins += outBox.Mins - _box.Mins;

				if ( !_dragging )
				{
					_startBox = _box;
					_startDeltaBox = _deltaBox;
					_dragging = true;

					foreach ( var hVertex in _component.Mesh.VertexHandles )
						_vertices[hVertex] = _component.Mesh.GetVertexPosition( hVertex ) - _startBox.Center;
				}

				var origin = _startTransform.Position;
				_box.Maxs = Gizmo.Snap( origin + _startBox.Maxs + _deltaBox.Maxs, _startDeltaBox.Maxs ) - origin;
				_box.Mins = Gizmo.Snap( origin + _startBox.Mins + _deltaBox.Mins, _startDeltaBox.Mins ) - origin;

				var scale = new Vector3(
					_startBox.Size.x != 0 ? _box.Size.x / _startBox.Size.x : 1,
					_startBox.Size.y != 0 ? _box.Size.y / _startBox.Size.y : 1,
					_startBox.Size.z != 0 ? _box.Size.z / _startBox.Size.z : 1
				);

				foreach ( var i in _vertices )
					_component.Mesh.SetVertexPosition( i.Key, (_startBox.Center + i.Value) * scale );

				var offset = _box.Center - (_startBox.Center * scale);
				var position = _component.WorldPosition;
				_component.WorldPosition = _startTransform.Position + (_startTransform.Rotation * offset);
				_component.RebuildMesh();
			}

			if ( Gizmo.WasLeftMouseReleased )
			{
				_undoScope?.Dispose();
				_undoScope = null;
			}
		}
	}
}
