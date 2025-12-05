namespace Editor;

internal static class AudioMenu
{
	[Menu( "Editor", "Settings/Mute" )]
	public static bool MuteSound
	{
		get => ConsoleSystem.GetValue( "snd_mute" ).ToBool();
		set => ConsoleSystem.SetValue( "snd_mute", value ? 1 : 0 );
	}

	[Menu( "Editor", "Settings/Mute when unfocused" )]
	public static bool PlayGameSoundOnTab
	{
		get => ConsoleSystem.GetValue( "snd_mute_losefocus" ).ToBool();
		set => ConsoleSystem.SetValue( "snd_mute_losefocus", value ? 1 : 0 );
	}

	[Event( "asset.contextmenu", Priority = 50 )]
	public static void OnSoundFileAssetContext( AssetContextMenu e )
	{
		// Are all the files we have selected sound assets?
		if ( !e.SelectedList.All( x => x.AssetType == AssetType.SoundFile ) )
			return;

		e.Menu.AddOption( $"Create Sound Event", "audio_file", action: () => CreateSoundEventUsingSoundFiles( e.SelectedList ) );

		if ( e.SelectedList.Count > 1 )
		{
			e.Menu.AddOption( $"Create {e.SelectedList.Count} Sound Events", "audio_file", action: () => CreateSoundEventsUsingSoundFiles( e.SelectedList ) );
		}
	}

	private static async void CreateSoundEventUsingSoundFiles( IEnumerable<AssetEntry> entries )
	{
		var fd = new FileDialog( null );
		fd.Title = "Create Sound Event from Sound Files..";
		fd.Directory = System.IO.Path.GetDirectoryName( entries.First().Asset.AbsolutePath );
		fd.DefaultSuffix = ".sound";
		var fileName = System.IO.Path.GetFileNameWithoutExtension( entries.First().Name );
		fd.SelectFile( $"{fileName}.sound" );
		fd.SetFindFile();
		fd.SetModeSave();
		fd.SetNameFilter( "Sound File (*.sound)" );

		if ( !fd.Execute() )
			return;

		var asset = AssetSystem.CreateResource( "sound", fd.SelectedFile );
		await asset.CompileIfNeededAsync();

		//
		// Load the sound event, configure it and save it
		//
		if ( asset.TryLoadResource<SoundEvent>( out var obj ) )
		{
			obj.Sounds = entries.Select( x => SoundFile.Load( x.Asset.Path ) ).ToList();
			asset.SaveToDisk( obj );
		}

		// These 3 lines are gonna be quite common I think.
		MainAssetBrowser.Instance?.Local.UpdateAssetList();
		MainAssetBrowser.Instance?.Local.FocusOnAsset( asset );
		EditorUtility.InspectorObject = asset;
	}

	private static async void CreateSoundEventsUsingSoundFiles( IEnumerable<AssetEntry> entries )
	{
		foreach ( var entry in entries )
		{
			var asset = entry.Asset;
			var newAsset = AssetSystem.CreateResource( "sound", System.IO.Path.ChangeExtension( asset.AbsolutePath, ".sound" ) );
			await newAsset.CompileIfNeededAsync();

			//
			// Load the sound event, configure it and save it
			//
			if ( newAsset.TryLoadResource<SoundEvent>( out var obj ) )
			{
				obj.Sounds = new List<SoundFile> { SoundFile.Load( asset.Path ) };
				newAsset.SaveToDisk( obj );
			}
		}

		MainAssetBrowser.Instance?.Local.UpdateAssetList();
	}
}
