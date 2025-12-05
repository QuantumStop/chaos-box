namespace Editor;

public abstract class ComponentTemplate
{
	public virtual string NameFilter => "Cs File (*.cs)";
	public virtual string Suffix => ".cs";
	public virtual string DefaultDirectory => Project.Current.GetCodePath();

	public abstract void Create( string componentName, string path );

	/// <summary>
	/// Get all component template types that aren't abstract.
	/// </summary>
	/// <returns></returns>
	public static TypeDescription[] GetAllTypes()
	{
		return EditorTypeLibrary.GetTypes<ComponentTemplate>().OrderByDescending( x => x.Name ).Where( x => !x.IsAbstract ).ToArray();
	}
}
