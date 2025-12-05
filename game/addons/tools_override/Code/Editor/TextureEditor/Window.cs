
namespace Editor.TextureEditor;

[EditorForAssetType( "vtex" )]
public class Window : DockWindow, IAssetEditor
{
	public bool CanOpenMultipleAssets => true;

	private string DefaultDockState;
	private Asset Asset;
	private Texture Texture;
	private TextureFile TextureFile;
	private string TextureFileData;
	private bool Modified;

	private Preview Preview;
	private Properties Properties;
	private Option ToolBarSaveOption;
	private Option MenuSaveOption;

	public Window()
	{
		DeleteOnClose = true;

		Title = "Texture Editor";
		Size = new Vector2( 1000, 800 );

		CreateToolBar();
		CreateUI();
		Show();
	}

	public void AssetOpen( Asset asset )
	{
		Raise();

		if ( asset == null )
			return;

		var json = System.IO.File.ReadAllText( asset.AbsolutePath );
		if ( string.IsNullOrWhiteSpace( json ) )
			return;

		try
		{
			TextureFile = Json.Deserialize<TextureFile>( json );
			TextureFile.Upgrade();
		}
		catch
		{
			// We currently don't support editing old kv files here,
			// trying to do so will reset the vtex -> warn the user
			if ( json.StartsWith( "<!-- dmx" ) )
			{
				var popup = new PopupDialogWidget( "⚠️" );
				popup.FixedWidth = 500;
				popup.WindowTitle = "Unsupported .vtex file";
				popup.MessageLabel.Text = $"Using this editor will reset the .vtex contents.\nDo you still want to open the editor?";

				popup.ButtonLayout.Spacing = 4;
				popup.ButtonLayout.AddStretchCell();

				popup.ButtonLayout.Add( new Button.Primary( "No, nevermind" )
				{
					Clicked = () =>
					{
						popup.Destroy();
						Close();
					}
				} );

				popup.ButtonLayout.Add( new Button( "Yes" )
				{
					Clicked = popup.Destroy
				} );

				popup.SetModal( true, true );
				popup.Hide();
				popup.Show();
			}
			TextureFile = TextureFile.CreateDefault( Enumerable.Empty<string>() );
		}

		Asset = asset;
		TextureFileData = json;
		Texture = Asset.LoadResource<Texture>();
		Preview.Texture = Texture;
		Properties.SetTextureFile( TextureFile );

		UpdateTitle();
	}

	private void UpdateTitle()
	{
		var title = "Texture Editor";
		Title = Asset != null ? $"{title} - {Asset.Name}{(Modified ? "*" : "")}" : title;

		if ( MenuSaveOption.IsValid() )
			MenuSaveOption.Enabled = Modified;

		if ( ToolBarSaveOption.IsValid() )
			ToolBarSaveOption.Enabled = Modified;
	}

	public void CreateUI()
	{
		UpdateTitle();
		BuildMenuBar();

		DockManager.RegisterDockType( "Preview", "photo", null, false );
		Preview = new Preview( this );
		Preview.Texture = Texture;
		DockManager.AddDock( null, Preview, DockArea.Left, DockManager.DockProperty.HideOnClose );

		var assetBrowser = new LocalAssetBrowser( this, new List<AssetType>() { AssetType.Texture } )
		{
			Name = "Asset Browser",
			WindowTitle = "Asset Browser"
		};
		assetBrowser.OnAssetSelected += ( a ) => AssetOpen( a );

		DockManager.RegisterDockType( "Asset Browser", "", null, false );
		DockManager.AddDock( null, assetBrowser, DockArea.Bottom, DockManager.DockProperty.HideOnClose, 0.2f );

		DockManager.RegisterDockType( "Properties", "edit", null, false );
		Properties = new Properties( this );
		Properties.SetTextureFile( TextureFile );
		Properties.OnChildValuesChanged += ( w ) => OnPropertiesChanged();
		DockManager.AddDock( null, Properties, DockArea.Left, DockManager.DockProperty.HideOnClose, 0.2f );

		DockManager.Update();

		DefaultDockState = DockManager.State;

		if ( StateCookie != "TextureEditor" )
		{
			StateCookie = "TextureEditor";
		}
		else
		{
			RestoreFromStateCookie();
		}
	}

	protected override void RestoreDefaultDockLayout()
	{
		DockManager.State = DefaultDockState;

		SaveToStateCookie();
	}

	[EditorEvent.Hotload]
	public void OnHotload()
	{
		SaveToStateCookie();

		DockManager.Clear();
		MenuBar.Clear();

		CreateUI();
	}

	[Shortcut( "editor.save", "CTRL+S" )]
	private void Save()
	{
		if ( !Modified )
			return;

		if ( Asset == null )
			return;

		if ( TextureFile == null )
			return;

		var json = Json.Serialize( TextureFile );
		if ( string.IsNullOrWhiteSpace( json ) )
			return;

		System.IO.File.WriteAllText( Asset.AbsolutePath, json );

		TextureFileData = json;
		Modified = false;

		UpdateTitle();
	}

	private void Modify()
	{
		if ( Asset == null )
			return;

		if ( TextureFile == null )
			return;

		var json = Json.Serialize( TextureFile );
		if ( string.IsNullOrWhiteSpace( json ) )
			return;

		System.IO.File.WriteAllText( Asset.AbsolutePath, json );
		Modified = json != TextureFileData;

		UpdateTitle();
	}

	private void CreateToolBar()
	{
		var toolBar = new ToolBar( this, "TextureEditorToolbar" );
		AddToolBar( toolBar, ToolbarPosition.Top );

		ToolBarSaveOption = toolBar.AddOption( "Save", "common/save.png", Save );
		ToolBarSaveOption.StatusTip = "Save";
		ToolBarSaveOption.Enabled = Modified;
	}

	public void BuildMenuBar()
	{
		var file = MenuBar.AddMenu( "File" );
		MenuSaveOption = file.AddOption( "Save", "common/save.png", Save, "editor.save" );
		MenuSaveOption.StatusTip = "Save";
		MenuSaveOption.Enabled = Modified;
		file.AddSeparator();
		file.AddOption( "Open Asset Location", "folder", () => EditorUtility.OpenFileFolder( Asset.AbsolutePath ) ).StatusTip = "Open Asset Location";
		file.AddSeparator();
		file.AddOption( "Quit", null, Quit, "editor.quit" ).StatusTip = "Quit";

		var view = MenuBar.AddMenu( "View" );
		view.AboutToShow += () => OnViewMenu( view );
	}

	[Shortcut( "editor.quit", "CTRL+Q" )]
	void Quit()
	{
		Close();
	}

	private void OnViewMenu( Menu view )
	{
		view.Clear();
		view.AddOption( "Restore To Default", "settings_backup_restore", RestoreDefaultDockLayout );
		view.AddSeparator();

		foreach ( var dock in DockManager.DockTypes )
		{
			var o = view.AddOption( dock.Title, dock.Icon );
			o.Checkable = true;
			o.Checked = DockManager.IsDockOpen( dock.Title );
			o.Toggled += ( b ) => DockManager.SetDockState( dock.Title, b );
		}
	}

	private void OnPropertiesChanged()
	{
		BindSystem.Flush();

		Modify();
	}

	protected override bool OnClose()
	{
		if ( Modified )
		{
			var confirm = new PopupWindow(
				"Save Current Texture", "The open texture has unsaved changes. Would you like to save now?", "Cancel",
				new Dictionary<string, System.Action>()
				{
					{ "No", () => { Restore(); Close(); } },
					{ "Yes", () => { Save(); Close(); } }
				}
			);

			confirm.Show();

			return false;
		}

		return true;
	}

	private void Restore()
	{
		if ( string.IsNullOrWhiteSpace( TextureFileData ) )
			return;

		System.IO.File.WriteAllText( Asset.AbsolutePath, TextureFileData );

		Modified = false;
	}

	void IAssetEditor.SelectMember( string memberName )
	{
		throw new System.NotImplementedException();
	}
}
