using static Editor.Inspectors.AssetInspector;

namespace Editor.Inspectors;

[CanEdit( "asset:jpg" )]
public class TextureCompileSettings : Widget, IAssetInspector
{
	private Asset Asset;

	public class Settings
	{
		public bool NoCompress { get; set; }
		public bool NoLod { get; set; }
		public bool NoMip { get; set; }

		[Group( "Clamping" )]
		public bool ClampU { get; set; }

		[Group( "Clamping" )]
		public bool ClampV { get; set; }

		[Group( "Clamping" )]
		public bool ClampW { get; set; }

		[Range( 0, 1 )]
		public float Brightness { get; set; }
	}

	readonly Settings settings = new();

	public TextureCompileSettings( Widget parent ) : base( parent )
	{
		VerticalSizeMode = SizeMode.CanGrow;
	}

	public void SetAsset( Asset asset )
	{
		Asset = asset;

		settings.ClampU = Asset.MetaData.Get<bool>( "clampu" );
		settings.ClampV = Asset.MetaData.Get<bool>( "clampv" );
		settings.ClampW = Asset.MetaData.Get<bool>( "clampw" );
		settings.NoLod = Asset.MetaData.Get<bool>( "nolod" );
		settings.NoMip = Asset.MetaData.Get<bool>( "nomip" );
		settings.NoCompress = Asset.MetaData.Get<bool>( "nocompress" );
		settings.Brightness = Asset.MetaData.Get( "brightness", 1.0f );

		var so = settings.GetSerialized();
		Layout = ControlSheet.Create( so );

		so.OnPropertyChanged += OnPropertyChanged;
	}

	public void OnPropertyChanged( SerializedProperty prop )
	{
		Asset.MetaData.Set( "clampu", settings.ClampU );
		Asset.MetaData.Set( "clampv", settings.ClampV );
		Asset.MetaData.Set( "clampw", settings.ClampW );
		Asset.MetaData.Set( "nolod", settings.NoLod );
		Asset.MetaData.Set( "nomip", settings.NoMip );
		Asset.MetaData.Set( "nocompress", settings.NoCompress );
		Asset.MetaData.Set( "brightness", settings.Brightness );
	}
}
