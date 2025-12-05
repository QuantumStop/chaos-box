using Sandbox.Localization;

namespace Editor;

public static class LocalizationTools
{
	[ConCmd( "localization.build" )]
	public static async Task PrintLocalization()
	{
		foreach ( var file in FileSystem.Localization.FindFile( "/en/", "*.json" ) )
		{
			var json = FileSystem.Localization.ReadJson<Dictionary<string, string>>( "/en/" + file );

			foreach ( var language in Sandbox.Localization.Languages.List )
			{
				if ( language.Abbreviation == "en" ) continue;

				await ProcessLanguageFile( file, json, language );
			}
		}
	}

	private static async Task ProcessLanguageFile( string file, Dictionary<string, string> json, LanguageInformation language, bool force = false )
	{
		// does this file already exist?
		var targetFileName = $"/{language.Abbreviation}/" + file;
		Dictionary<string, string> entries = new();

		if ( !force && FileSystem.Localization.FileExists( targetFileName ) )
		{
			entries = FileSystem.Localization.ReadJson<Dictionary<string, string>>( targetFileName );

			// remove all those not in json
			foreach ( var key in entries.Keys.ToList() )
			{
				if ( !json.ContainsKey( key ) )
				{
					entries.Remove( key );
				}
			}
		}

		await System.Threading.Tasks.Parallel.ForEachAsync( json, async ( entry, token ) =>
		{
			var translation = await Editor.EditorUtility.TranslateString( entry.Value, language.Title );

			lock ( entries )
			{
				entries[entry.Key] = translation;
			}

		} );

		FileSystem.Localization.CreateDirectory( $"/{language.Abbreviation}/" );
		FileSystem.Localization.WriteJson( targetFileName, entries );
		Log.Info( $"Updated {targetFileName}" );
	}
}
