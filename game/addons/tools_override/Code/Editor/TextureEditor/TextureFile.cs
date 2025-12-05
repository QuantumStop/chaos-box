
namespace Editor.TextureEditor;

public enum GammaType
{
	Linear,
	SRGB,
}

public enum ImageFormatType
{
	DXT5,
	DXT3,
	DXT1,
	RGBA8888,
	BC7,
	BC6H,
	RGBA16161616,
	RGBA16161616F,
	RGBA32323232F,
	R32F,
}

public enum MipAlgorithm
{
	None,
	Box,
	// Everything else is kind of bullshit
}

public class TextureSequence
{
	[Title( "Images" ), Group( "Input" ), ImageAssetPath, KeyProperty]
	public string Source { get; set; }

	[ToggleGroup( "Sequence" )]
	public bool IsLooping { get; set; }

	[ToggleGroup( "FlipBook" )]
	public bool FlipBook { get; set; }

	[ToggleGroup( "FlipBook" )]
	public int Columns { get; set; }

	[ToggleGroup( "FlipBook" )]
	public int Rows { get; set; }

	[ToggleGroup( "FlipBook" )]
	public int Frames { get; set; } = 64;
}

public class TextureFile
{
	[Title( "Images" ), Group( "Input" ), ImageAssetPath, Hide]
	public List<string> Images { get; set; }

	[Title( "Sequences" ), Group( "Input" )]
	public List<TextureSequence> Sequences { get; set; }

	[Title( "Color Space" ), Group( "Input" )]
	public GammaType InputColorSpace { get; set; }

	[Title( "Image Format" ), Group( "Output" )]
	public ImageFormatType OutputFormat { get; set; }

	[Title( "Color Space" ), Group( "Output" )]
	public GammaType OutputColorSpace { get; set; }

	[Title( "Mip Algorithm" ), Group( "Output" )]
	public MipAlgorithm OutputMipAlgorithm { get; set; }

	[Hide]
	public string OutputTypeString { get; set; }

	public static TextureFile CreateDefault( IEnumerable<string> images, bool noCompress = false )
	{
		return new TextureFile
		{
			Sequences = images.Select( x => new TextureSequence()
			{
				Source = x,
				IsLooping = true
			} )
			.ToList(),

			OutputFormat = noCompress ? ImageFormatType.RGBA8888 : ImageFormatType.DXT5,
			OutputColorSpace = GammaType.Linear,
			OutputMipAlgorithm = MipAlgorithm.None,
			InputColorSpace = GammaType.Linear,
			OutputTypeString = "2D"
		};
	}

	/// <summary>
	/// Upgrade from "images" to "sequence"
	/// </summary>
	internal void Upgrade()
	{
		Sequences ??= new List<TextureSequence>();

		if ( Images is not null )
		{
			foreach ( var image in Images )
			{
				Sequences.Add( new TextureSequence
				{
					Source = image,
					IsLooping = true
				} );
			}

			Images = null;
		}

	}
}
