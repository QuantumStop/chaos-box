using System.IO;
namespace Editor;

internal static class MapMenu
{
	public static Option PlayMapOption { get; private set; }

	[Event( "asset.contextmenu", Priority = 50 )]
	public static void OnMapFileAssetContext( AssetContextMenu e )
	{
		if ( e.SelectedList.Count != 1 )
			return;

		var entry = e.SelectedList[0];

		if ( entry == null ) return;
		if ( entry.AssetType != AssetType.MapFile ) return;

		bool isCompiled = File.Exists( Path.ChangeExtension( entry.Asset.AbsolutePath, ".vpk" ) );

		PlayMapOption = e.Menu.AddOption( "Play Map", "play_arrow", action: () => EditorScene.PlayMap( entry.Asset ) );
		PlayMapOption.Text = isCompiled ? "Play Map" : "Map is not compiled";
		PlayMapOption.Icon = isCompiled ? "play_arrow" : "error";

		if ( !isCompiled )
		{
			PlayMapOption.Enabled = false;
		}
	}
}
