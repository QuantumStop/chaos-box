namespace Editor;

partial class ViewportTools
{
	private void BuildToolbarRight( Layout layout )
	{
		{
			var group = AddGroup();

			group.Layout.Add( new ViewportButton( "wifi", OpenNetworkSettings ) { ToolTip = "Network settings" } );

			layout.Add( group );
		}

		AddSeparator( layout );

		{
			var group = AddGroup();

			group.Layout.Add( new ViewportButton( "grid_view", OpenSceneViewModeMenu ) { ToolTip = "Layout" } );
			group.Layout.Add( new ViewportButton( "crop_free", ToggleFullscreen ) { ToolTip = "Toggle Fullscreen" } );

			layout.Add( group );
		}
	}

	private static readonly Pixmap LayoutOne = Pixmap.FromFile( "toolimages:qcontrols/split_single.png" );
	private static readonly Pixmap LayoutTwoH = Pixmap.FromFile( "toolimages:qcontrols/split_two_horizontal.png" );
	private static readonly Pixmap LayoutTwoV = Pixmap.FromFile( "toolimages:qcontrols/split_two_vertical.png" );
	private static readonly Pixmap LayoutThreeLeft = Pixmap.FromFile( "toolimages:qcontrols/split_two_right_one_left.png" );
	private static readonly Pixmap LayoutThreeRight = Pixmap.FromFile( "toolimages:qcontrols/split_two_left_one_right.png" );
	private static readonly Pixmap LayoutThreeTop = Pixmap.FromFile( "toolimages:qcontrols/split_two_bottom_one_top.png" );
	private static readonly Pixmap LayoutThreeBottom = Pixmap.FromFile( "toolimages:qcontrols/split_two_top_one_bottom.png" );
	private static readonly Pixmap LayoutFour = Pixmap.FromFile( "toolimages:qcontrols/split_four_way.png" );

	void OpenSceneViewModeMenu()
	{
		var menu = new ContextMenu( null );

		foreach ( var entry in EditorTypeLibrary.GetEnumDescription( typeof( SceneViewWidget.ViewportLayoutMode ) ) )
		{
			var layout = (SceneViewWidget.ViewportLayoutMode)entry.ObjectValue;
			var icon = layout switch
			{
				SceneViewWidget.ViewportLayoutMode.One => LayoutOne,
				SceneViewWidget.ViewportLayoutMode.TwoHorizontal => LayoutTwoH,
				SceneViewWidget.ViewportLayoutMode.TwoVertical => LayoutTwoV,
				SceneViewWidget.ViewportLayoutMode.ThreeLeft => LayoutThreeLeft,
				SceneViewWidget.ViewportLayoutMode.ThreeRight => LayoutThreeRight,
				SceneViewWidget.ViewportLayoutMode.ThreeTop => LayoutThreeTop,
				SceneViewWidget.ViewportLayoutMode.ThreeBottom => LayoutThreeBottom,
				SceneViewWidget.ViewportLayoutMode.Four => LayoutFour,
				_ => null
			};

			var o = menu.AddOption( entry.Title, null, () => sceneViewWidget.ViewportLayout = layout );
			o.SetIcon( icon );
			o.Checkable = true;
			o.Checked = sceneViewWidget.ViewportLayout == layout;
			o.Enabled = !sceneViewWidget.Session.IsPlaying; // Not great, but this stuff needs straightening out overall
		}

		menu.OpenAtCursor();
	}
}
