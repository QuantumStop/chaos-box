namespace Editor;

/// <summary>
/// Navigation tool contains tools for setting up and testing navigation in the scene
/// </summary>
[EditorTool( "tools.navmesh-tool" )]
[Title( "Navigation Tool" )]
[Icon( "directions_run" )]
[Alias( "navmeshtool" )]
[Group( "5" )]
internal class NavMeshTool : EditorTool
{
	public override IEnumerable<EditorTool> GetSubtools()
	{
		yield return new NavTestSettings();
		yield return new NavTestTool();
	}

	public override void OnDisabled()
	{
		SceneOverlay.Parent.Cursor = CursorShape.Arrow;
	}
}
