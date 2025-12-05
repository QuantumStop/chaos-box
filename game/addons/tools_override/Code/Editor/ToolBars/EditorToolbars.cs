namespace Editor;

using Editor;
using Sandbox;
using Sandbox.UI;
using System;

public static partial class EditorToolBars
{
	public static ToolBar MainTools;
	public static ToolBar SelectionModes;
	public static ToolBar EditingSettings;
	public static ToolBar ViewSettings;

	private static DockWindow MainWindow;

	/// <summary>
	/// On the first creation of the editor - initializes all the necessary toolbars,
	/// builds shortcut cache and stores MainWindow ref within this class.
	/// </summary>
	[Event( "editor.created" )]
    public static void InitToolbars( EditorMainWindow window )
    {		
		MainWindow = window;

		BuildShortcutCache();
		BuildAllToolbars();
	}

	[Menu( "Editor", "HL2K/Editor/Debug/Force Rebuild Toolbars", "hammer/appicon.ico" ), Order( 100 )]
	static void ForceRebuildToolbars()
    {
       	TryClearToolbars();
 		BuildAllToolbars();
    }

	/// <summary>
	/// Initializes and configures all toolbars for the main application window.
	/// </summary>
	/// <remarks>This method builds the main tools, selection modes, editing settings, and view settings toolbars. </remarks>
	internal static void BuildAllToolbars()
    {
		if ( MainWindow == null )
			return;

   		BuildMainTools( MainWindow );
		BuildSelectionModes( MainWindow );
		BuildEditingSettings( MainWindow );
		BuildViewSettings( MainWindow );
    }

	/// <summary>
	/// Attempts to close and clear all active toolbars, releasing their resources and resetting their references.
	/// </summary>
	/// <remarks>This method is typically used during application shutdown or when resetting the user interface to
	/// ensure that all toolbars are properly disposed of. After calling this method, the toolbar references will be set to
	/// null and should not be used unless reinitialized.</remarks>
	static void TryClearToolbars()
	{
		static void CloseIfExists( ToolBar tb )
		{
			if ( tb != null )
			{
				tb.Close();
				tb.Clear();
			}
		}

		CloseIfExists( MainTools );
		CloseIfExists( SelectionModes );
		CloseIfExists( EditingSettings );
		CloseIfExists( ViewSettings );

		MainTools 		= null;
		SelectionModes 	= null;
		EditingSettings = null;
		ViewSettings 	= null;
    }

	[Event( "tools.gamedata.refresh" )]
	private static void InitializeShortcutCache()
	{
		BuildShortcutCache();
	}

	// Important TODO: Registering toolbars as dockables is vital, however it creats a problem
	// that if you were to try and access them in the view dropdown you'll get a window, not a toolbar!
	// We'll fix this eventually, just need to do other stuff first.

	// MAIN TOOLS
	private static void BuildMainTools( DockWindow window )
	{
		var dock = window.DockManager;
		MainTools = new ToolBar( window, "Main Tools" );
		MainTools.SetIconSize( new Vector2( 32, 32 ) );

		AddDefs( MainTools, MainToolDefs, singleSelect: true );

		dock.RegisterDockType( "Editor - Main Tools", "hammer/appicon.ico", () => MainTools );
		window.AddToolBar( MainTools, ToolbarPosition.Left );
		RegisterToolBar( "ViewSettings", ViewSettings, window );
	}

	// SELECTION MODES
	private static void BuildSelectionModes( DockWindow window )
	{
		var dock = window.DockManager;
		SelectionModes = new ToolBar( window, "Selection Modes" );
		SelectionModes.SetIconSize( 24 );
		SelectionModes.ButtonStyle = ToolButtonStyle.TextBesideIcon;

		Label label = new( SelectionModes )
		{
			Text = "Select: ",
			Color = Theme.Text
		};
		SelectionModes.AddWidget( label );

		AddDefs( SelectionModes, SelectionModeDefs, singleSelect: true );

		dock.RegisterDockType( "Editor - Selection Modes", "hammer/appicon.ico", () => SelectionModes );
		window.AddToolBar( SelectionModes, ToolbarPosition.Top );
		RegisterToolBar( "SelectionModes", SelectionModes, window );
	}

	// EDITING SETTINGS
	private static void BuildEditingSettings( DockWindow window )
	{
		var dock = window.DockManager;
		EditingSettings = new ToolBar( window, "Editing Settings" );
		SelectionModes.SetIconSize( 22 );

		Label label = new( EditingSettings )
		{
			Text = "Editing: ",
			Color = Theme.Text
		};
		EditingSettings.AddWidget( label );

		AddDefs( EditingSettings, EditingSettingDefs );

		dock.RegisterDockType( "Editor - Editing Settings", "hammer/appicon.ico", () => EditingSettings );
		window.AddToolBar( EditingSettings, ToolbarPosition.Top );
		RegisterToolBar( "EditingSettings", EditingSettings, window );
	}

	// VIEW SETTINGS
	private static void BuildViewSettings( DockWindow window )
	{
		var dock = window.DockManager;
		ViewSettings = new ToolBar( window, "View Settings" );
		SelectionModes.SetIconSize( 22 );

		Label label = new( ViewSettings )
		{
			Text = "View: ",
			Color = Theme.Text
		};
		ViewSettings.AddWidget( label );

		AddDefs( ViewSettings, ViewSettingDefs );

		dock.RegisterDockType( "Editor - View Settings", "hammer/appicon.ico", () => ViewSettings );
		window.AddToolBar( ViewSettings, ToolbarPosition.Top );
		RegisterToolBar( "ViewSettings", ViewSettings, window );
	}
}

