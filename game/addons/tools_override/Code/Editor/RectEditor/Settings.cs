namespace Editor.RectEditor;

public class Settings
{
	[Hide] public string ReferenceMaterial { get; set; } = null;
	[Hide] public bool ShowNormalizedValues { get; set; } = false;
	public int GridSize { get; set; } = 16;
}
