using Sandbox;

namespace Editor;

public class BoxColliderTool : EditorTool<BoxCollider>
{
	private IDisposable _componentUndoScope;
	private bool _resizing = false;
	private BBox _startBox = new BBox();
	private BBox _deltaBox = new BBox();

	public override void OnUpdate()
	{
		var boxCollider = GetSelectedComponent<BoxCollider>();
		if ( boxCollider == null )
			return;

		var currentBox = BBox.FromPositionAndSize( boxCollider.Center, boxCollider.Scale );

		using ( Gizmo.Scope( "Box Collider Editor", boxCollider.WorldTransform ) )
		{
			if ( Gizmo.Control.BoundingBox( "Bounds", currentBox, out var newBox ) )
			{
				if ( _componentUndoScope == null )
				{
					_componentUndoScope = SceneEditorSession.Active.UndoScope( "Resize Box Collider" )
						.WithComponentChanges( boxCollider ).Push();
				}

				if ( !_resizing )
				{
					_resizing = true;
					_startBox = currentBox;
					_deltaBox = new BBox( Vector3.Zero, Vector3.Zero );
				}

				_deltaBox.Maxs += newBox.Maxs - currentBox.Maxs;
				_deltaBox.Mins += newBox.Mins - currentBox.Mins;

				var snappedBox = Gizmo.Snap( _startBox, _deltaBox );

				boxCollider.Center = snappedBox.Center;
				boxCollider.Scale = snappedBox.Size;
			}

			if ( Gizmo.WasLeftMouseReleased )
			{
				_resizing = false;
				_componentUndoScope?.Dispose();
				_componentUndoScope = null;
			}
		}
	}
}
