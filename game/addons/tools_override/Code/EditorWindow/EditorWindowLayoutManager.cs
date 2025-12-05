namespace Editor;

/// <summary>
/// An isolated manager class to handle saving and loading your own editor window layouts.
/// </summary>
static class EditorWindowLayoutManager
{
	record struct LayoutFile( string Name, string Json );

	/// <summary>
	/// Hold a reference to the Load and Delete Layout menu so we can update it wherever. 
	/// </summary>
	static Menu LoadLayoutMenu;
	static Menu DeleteLayoutMenu;

	static void TrySaveToName( string name )
	{
		// make sure filder exists
		FileSystem.Config.CreateDirectory( "editor/layout" );

		var filename = name.GetFilenameSafe();
		filename = $"editor/layout/{filename}.json";

		var layout = new LayoutFile( name, EditorWindow.DockManager.State );

		if ( FileSystem.Config.FileExists( filename ) )
		{
			Dialog.AskConfirm( () =>
			{
				SaveToFilename( filename, layout );

			}, "Do you want to overwrite existing layout?", "Overwrite Layout" );
		}
		else
		{
			SaveToFilename( filename, layout );
		}
	}

	static void SaveToFilename( string filename, LayoutFile content )
	{
		Editor.FileSystem.Config.CreateDirectory( "editor/layout" );
		Editor.FileSystem.Config.WriteJson( filename, content );
	}

	/// <summary>
	/// Opens a dialog to save the current state of the editor as a layout
	/// </summary>
	static void SaveWindowLayout()
	{
		Dialog.AskString( TrySaveToName, "What do you want to call this layout?", okay: "Save", title: "Save Layout" );
	}

	/// <summary>
	/// Opens a dialog to delete a layout.
	/// </summary>
	static void DeleteWindowLayout( string name )
	{
		var filename = name.GetFilenameSafe();
		filename = $"editor/layout/{filename}.json";

		Dialog.AskConfirm( () =>
		{
			Editor.FileSystem.Config.DeleteFile( filename );

		}, $"Are you sure you want to delete layout '{name}'?", "Delete Layout" );
	}

	/// <summary>
	/// If the user has layouts saved in cookies, convert them to file based and delete the cookie
	/// </summary>
	static void UpgradeFromCookies()
	{
		var layouts = EditorCookie?.Get<List<LayoutFile>>( $"window.{EditorWindow.StateCookie}.dock.layouts", null );
		if ( layouts is null )
			return;

		foreach ( var layout in layouts )
		{
			var filename = layout.Name;
			Editor.FileSystem.Config.CreateDirectory( "editor/layout" );
			Editor.FileSystem.Config.WriteJson( $"editor/layout/{filename}.json", new LayoutFile( layout.Name, layout.Json ) );
		}

		// clear old cookies
		EditorCookie.Remove( $"window.{EditorWindow.StateCookie}.dock.layouts" );
	}

	/// <summary>
	/// Create our options on the "View" tab in the Editor Window
	/// </summary>
	/// <param name="menu"></param>
	[Event( "tools.editorwindow.createview" )]
	static void CreateDynamicViewMenu( Menu menu )
	{
		UpgradeFromCookies();

		menu.AddOption( "Save Layout", "playlist_add", SaveWindowLayout );

		LoadLayoutMenu = menu.AddMenu( "Load Layout", "playlist_add_check" );
		LoadLayoutMenu.AboutToShow += FillLoadMenu;

		DeleteLayoutMenu = menu.AddMenu( "Delete Layout", "playlist_remove" );
		DeleteLayoutMenu.AboutToShow += FillDeleteMenu;
	}

	private static void FillLoadMenu()
	{
		LoadLayoutMenu.Clear();

		foreach ( var file in Editor.FileSystem.Config.FindFile( "/editor/layout/", "*.json" ) )
		{
			var layout = FileSystem.Config.ReadJsonOrDefault<LayoutFile>( $"/editor/layout/{file}", default );
			if ( layout.Name is null ) continue;
			if ( layout.Json is null ) continue;

			LoadLayoutMenu.AddOption( layout.Name, "category", () =>
			{
				EditorWindow.DockManager.State = layout.Json;
			} );
		}
	}

	private static void FillDeleteMenu()
	{
		DeleteLayoutMenu.Clear();

		foreach ( var file in Editor.FileSystem.Config.FindFile( "/editor/layout/", "*.json" ) )
		{
			if ( file == "Default.json" )
				continue;

			var layout = FileSystem.Config.ReadJsonOrDefault<LayoutFile>( $"/editor/layout/{file}", default );
			if ( layout.Name is null ) continue;
			if ( layout.Json is null ) continue;

			DeleteLayoutMenu.AddOption( layout.Name, "category", () =>
			{
				DeleteWindowLayout( layout.Name );
			} );
		}
	}
}
