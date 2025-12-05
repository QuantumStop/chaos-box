
namespace Editor.MeshEditor;

/// <summary>
/// A mesh element can be a vertex, edge or face belonging to a mesh
/// </summary>
public interface IMeshElement : IValid
{
	public MeshComponent Component { get; }
	public GameObject GameObject => Component.IsValid() ? Component.GameObject : null;
	public Scene Scene => Component.IsValid() ? Component.Scene : null;
}
