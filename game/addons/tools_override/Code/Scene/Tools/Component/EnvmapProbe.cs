using Sandbox;

namespace Editor;

public class EnvmapProbeTool : EditorTool<EnvmapProbe>
{
	private IDisposable _componentUndoScope;

	public override void OnUpdate()
	{
		var envmapProbe = GetSelectedComponent<EnvmapProbe>();
		if ( envmapProbe == null )
			return;

		var currentBounds = envmapProbe.Bounds;

		using ( Gizmo.Scope( "EnvmapPrope Collider Editor", envmapProbe.WorldTransform ) )
		{
			if ( Gizmo.Control.BoundingBox( "Bounds", currentBounds, out var newBounds ) )
			{
				if ( Gizmo.WasLeftMousePressed )
				{
					_componentUndoScope = SceneEditorSession.Active.UndoScope( "Resize EnvmapPrope Bounds" ).WithComponentChanges( envmapProbe ).Push();
				}
				envmapProbe.Bounds = newBounds;
			}

			if ( Gizmo.WasLeftMouseReleased )
			{
				_componentUndoScope?.Dispose();
				_componentUndoScope = null;
			}
		}
	}
}
