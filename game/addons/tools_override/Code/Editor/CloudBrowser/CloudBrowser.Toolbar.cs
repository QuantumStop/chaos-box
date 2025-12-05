
namespace Editor;

public partial class CloudAssetBrowser : Widget
{
	void BuildToolbar( Layout toolbar )
	{
		toolbar.Spacing = 2;
		toolbar.Margin = 2;

		toolbar.Add( Search, 1 );

		toolbar.AddSpacingCell( 2 );

		var facets = toolbar.Add( new Widget() );
		facets.Layout = Layout.Row();
		facets.Layout.Spacing = 4;

		FacetLayout = facets.Layout;

		ViewMode = toolbar.Add( new ToolButton( "View Mode\n(ctrl + mouse wheel)", "grid_view", this ) );
		ViewMode.MouseLeftPress = () =>
		{
			var menu = new ContextMenu( this );

			menu.AddOption( "List View", "view_headline", () => ViewModeType = AssetListViewMode.List );
			menu.AddOption( "Small Icons", "apps", () => ViewModeType = AssetListViewMode.SmallIcons );
			menu.AddOption( "Medium Icons", "grid_on", () => ViewModeType = AssetListViewMode.MediumIcons );
			menu.AddOption( "Large Icons", "grid_view", () => ViewModeType = AssetListViewMode.LargeIcons );

			menu.OpenAt( ViewMode.ScreenRect.BottomLeft, false );
		};

		OrderMode = toolbar.Add( new ToolButton( "Order Mode", "emoji_events", this ) );
	}
}
