
namespace Editor.NodeEditor;

public abstract class ValueEditor : GraphicsItem
{
	public bool Enabled { get; set; }
	public virtual bool HideLabel { get; } = true;

	public ValueEditor( GraphicsItem parent ) : base( parent )
	{
		Enabled = true;
	}
}
