namespace Editor.TerrainEditor;

/// <summary>
/// Brushes you can use
/// </summary>
public class BrushList
{
	public Brush Selected { get; set; }
	public List<Brush> Brushes = new();

	public BrushList()
	{
		LoadAll();
	}

	public void LoadAll()
	{
		// Not available in unit test
		if ( FileSystem.Content is null )
			return;

		foreach ( var filename in FileSystem.Content.FindFile( "materials/tools/terrain/brushes", "*.png" ) )
		{
			Brushes.Add( Brush.LoadFromFile( $"materials/tools/terrain/brushes/{filename}" ) );
		}

		Selected = Brushes.FirstOrDefault();
	}
}

public class Brush
{
	public string Name { get; private set; }
	public Texture Texture { get; private set; }
	public Pixmap Pixmap { get; private set; }

	public void Set( string name )
	{
		Texture = Texture.Load( $"materials/tools/terrain/brushes/{name}.png" );
	}

	internal static Brush LoadFromFile( string filename )
	{
		var brush = new Brush();
		brush.Name = System.IO.Path.GetFileNameWithoutExtension( filename );
		brush.Texture = Texture.Load( filename );
		brush.Pixmap = Pixmap.FromFile( FileSystem.Content.GetFullPath( filename ) );
		return brush;
	}
}
