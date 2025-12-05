using static Editor.Inspectors.AssetInspector;

namespace Editor.Inspectors;

[CanEdit( "asset:vsnd" )]
public class SoundFileCompileSettings : Widget, IAssetInspector
{
	private Asset Asset;

	public class Settings
	{
		[Title( "Enabled" ), Category( "Looping" )]
		public bool Loop { get; set; }

		[Category( "Looping" ), ShowIf( nameof( Loop ), true )]
		[Description( "Start Time" )]
		public float Start { get; set; }

		[Category( "Looping" ), ShowIf( nameof( Loop ), true )]
		[Description( "End Time, 0 is end of sound" )]
		public float End { get; set; }

		[Title( "Sample Rate" ), Category( "Resampling" )]
		public SamplingRate Rate { get; set; } = SamplingRate.Rate44100;

		[Title( "Enabled" ), Category( "Compression" )]
		public bool Compress { get; set; }

		[Title( "Bitrate" ), Category( "Compression" ), MinMax( 128, 256 )]
		public int Bitrate { get; set; } = 256;

		public enum SamplingRate
		{
			[Title( "8000" )] Rate8000 = 8000,
			[Title( "11025" )] Rate11025 = 11025,
			[Title( "12000" )] Rate12000 = 12000,

			[Title( "16000" )] Rate16000 = 16000,
			[Title( "22050" )] Rate22050 = 22050,
			[Title( "24000" )] Rate24000 = 24000,

			[Title( "32000" )] Rate32000 = 32000,
			[Title( "44100" )] Rate44100 = 44100
		}
	}

	private Settings _settings = new();

	public SoundFileCompileSettings( Widget parent ) : base( parent )
	{
		VerticalSizeMode = SizeMode.CanGrow;
	}

	public void SetAsset( Asset asset )
	{
		Asset = asset;

		if ( Asset.MetaData is null )
			return;

		_settings.Loop = Asset.MetaData.Get( "loop", false );
		_settings.Start = Asset.MetaData.Get( "start", 0.0f );
		_settings.End = Asset.MetaData.Get( "end", 0.0f );
		_settings.Rate = Asset.MetaData.Get( "rate", Settings.SamplingRate.Rate44100 );
		_settings.Compress = Asset.MetaData.Get( "compress", false );
		_settings.Bitrate = Asset.MetaData.Get( "bitrate", 256 );

		var so = EditorTypeLibrary.GetSerializedObject( _settings );
		Layout = ControlSheet.Create( so );
		so.OnPropertyChanged += ValuesChanged;
	}

	void ValuesChanged( SerializedProperty property )
	{
		if ( Asset.MetaData is null )
			return;

		Asset.MetaData.Set( "loop", _settings.Loop );
		Asset.MetaData.Set( "start", _settings.Start );
		Asset.MetaData.Set( "end", _settings.End );
		Asset.MetaData.Set( "rate", _settings.Rate );
		Asset.MetaData.Set( "compress", _settings.Compress );
		Asset.MetaData.Set( "bitrate", _settings.Bitrate );
	}
}
