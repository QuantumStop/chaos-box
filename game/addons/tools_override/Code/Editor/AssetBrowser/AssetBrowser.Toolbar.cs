
using Sandbox;

namespace Editor;

public partial class AssetBrowser : Widget
{
	void BuildToolbar( Layout toolbar )
	{
		toolbar.Spacing = 2;
		toolbar.Margin = 2;

		var newButton = toolbar.Add( new AddButton( "New", "add" ) );
		newButton.MouseLeftPress = OpenCreateMenu;
		newButton.Bind( "Enabled" ).ReadOnly().From( () => CurrentLocation is DiskLocation && CurrentLocation.Type is LocationType.Assets or LocationType.Code, null );

		toolbar.AddSpacingCell( 8.0f );

		var history = toolbar.Add( new Widget() );
		history.Layout = Layout.Row();
		history.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground );
			Paint.DrawRect( history.LocalRect, Theme.ControlRadius );
			return true;
		};

		var back = history.Layout.Add( new ToolButton( "Back", "arrow_back", this )
		{
			MouseLeftPress = GoBack,
			Enabled = false
		} );
		back.Bind( "Enabled" ).ReadOnly().From( History.CanGoBack, null );

		var fwd = history.Layout.Add( new ToolButton( "Forward", "arrow_forward", this )
		{
			MouseLeftPress = GoForward,
			Enabled = false
		} );
		fwd.Bind( "Enabled" ).ReadOnly().From( History.CanGoForward, null );

		history.Layout.Add( new ToolButton( "Recent locations", "expand_more", this )
		{
			MouseLeftPress = () =>
			{
				var menu = new ContextMenu();
				var history = History.TakeLast( 10 ).ToArray();
				for ( int i = history.Length - 1; i >= 0; --i )
				{
					var index = History.Count - history.Length + i;
					var item = history[i];
					var isCurrent = index == History.CurrentIndex;
					var option = menu.AddOption( new Option( $"{item}" ) { Checkable = true, Checked = isCurrent } );
					option.Triggered = () =>
					{
						NavigateTo( History.GoTo( index ), false );
					};
				}
				menu.OpenAtCursor();
			},
			FixedWidth = 8
		} );

		history.Layout.AddSpacingCell( 8.0f );

		toolbar.AddSpacingCell( 6.0f );

		var location = toolbar.Add( new Widget() );
		location.Layout = Layout.Row();
		location.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground );
			Paint.DrawRect( location.LocalRect, Theme.ControlRadius );
			return true;
		};

		var up = location.Layout.Add( new ToolButton( "Parent folder", "arrow_upward", this ) );
		up.Bind( "Enabled" ).ReadOnly().From( () => CurrentLocation?.CanGoUp() ?? false, null );
		up.MouseLeftPress = OpenParentFolder;

		location.Layout.Add( Path );

		var splitter = new Splitter( this );
		splitter.IsHorizontal = true;
		splitter.AddWidget( location );
		splitter.AddWidget( Search );
		splitter.SetStretch( 0, 9 );
		splitter.SetStretch( 1, 1 );

		toolbar.Add( splitter, 1 );

		toolbar.AddSpacingCell( 8.0f );

		var filters = toolbar.Add( new Widget() );
		filters.Layout = Layout.Row();
		filters.OnPaintOverride = () =>
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground );
			Paint.DrawRect( filters.LocalRect, Theme.ControlRadius );
			return true;
		};

		var flat = filters.Layout.Add( new ToolButton( "Folder View / Flat View", "folder", this ) { IconChecked = "folder_copy" } );
		flat.IsToggle = true;
		flat.Bind( "Checked" ).From( this, nameof( ShowRecursiveFiles ) );
		flat.Bind( "Enabled" ).ReadOnly().From( () => CurrentLocation is DiskLocation or MountLocation && Search.IsEmpty, null );

		ViewMode = filters.Layout.Add( new ToolButton( "View Mode\n(ctrl + mouse wheel)", "grid_view", this ) );
		ViewMode.MouseLeftPress = () =>
		{
			var menu = new ContextMenu( this );

			menu.AddOption( "List View", "view_headline", () => ViewModeType = AssetListViewMode.List );
			menu.AddOption( "Small Icons", "apps", () => ViewModeType = AssetListViewMode.SmallIcons );
			menu.AddOption( "Medium Icons", "grid_on", () => ViewModeType = AssetListViewMode.MediumIcons );
			menu.AddOption( "Large Icons", "grid_view", () => ViewModeType = AssetListViewMode.LargeIcons );

			menu.OpenAt( ViewMode.ScreenRect.BottomLeft, false );
		};

		var settings = filters.Layout.Add( new ToolButton( "More Options", "more_vert", this ) );
		settings.MouseLeftPress = () => OpenSettingsMenu( settings.ScreenRect );
	}

	private void UpdateViewModeIcon()
	{
		if ( AssetList.ViewMode == AssetListViewMode.List ) ViewMode.Icon = "view_headline";
		if ( AssetList.ViewMode == AssetListViewMode.SmallIcons ) ViewMode.Icon = "apps";
		if ( AssetList.ViewMode == AssetListViewMode.MediumIcons ) ViewMode.Icon = "grid_on";
		if ( AssetList.ViewMode == AssetListViewMode.LargeIcons ) ViewMode.Icon = "grid_view";

		ViewMode.Update();
	}

	void OpenCreateMenu()
	{
		var menu = new ContextMenu();

		CreateAsset.AddOptions( menu, CurrentLocation );

		menu.OpenAt( Application.CursorPosition );
	}
}

file class AddButton : Widget
{
	public string Icon;
	public string Text;

	public AddButton( string text, string icon ) : base( null )
	{
		Icon = icon;
		Text = text;

		Cursor = CursorShape.Finger;
	}

	protected override Vector2 SizeHint()
	{
		var baseHeight = base.SizeHint().y;
		return new Vector2( 56, baseHeight );
	}

	protected override void OnPaint()
	{
		Paint.ClearBrush();
		Paint.ClearPen();

		var r = LocalRect;

		var color = Theme.ControlBackground;

		if ( Enabled && Paint.HasMouseOver )
		{
			color = color.Lighten( 0.1f );
		}

		Paint.ClearPen();
		Paint.SetBrush( color );
		Paint.DrawRect( LocalRect, Theme.ControlRadius );

		Paint.ClearBrush();
		Paint.ClearPen();
		Paint.SetPen( Enabled ? Theme.Primary : Theme.TextDisabled );

		var textRect = r.Shrink( 4, 0 );
		textRect.Left += 2;

		if ( !string.IsNullOrEmpty( Icon ) )
		{
			// Draw icon
			var iconRect = Paint.DrawIcon( textRect, Icon, 14, TextFlag.LeftCenter );
			textRect.Left += iconRect.Width + 4;
		}

		if ( !string.IsNullOrEmpty( Text ) )
		{
			// Draw text
			Paint.TextAntialiasing = true;
			Paint.SetDefaultFont();
			Paint.DrawText( textRect, Text, TextFlag.LeftCenter );
			Paint.TextAntialiasing = false;
		}
	}
}
