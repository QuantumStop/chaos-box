namespace Editor;

public partial class TreeView
{
	class FilesystemTreeNode : TreeNode
	{
		public System.IO.FileSystemInfo Info;

		bool IsFolder => Info is System.IO.DirectoryInfo;

		public FilesystemTreeNode( string path )
		{
			if ( System.IO.Directory.Exists( path ) ) Info = new System.IO.DirectoryInfo( path );
			else if ( System.IO.File.Exists( path ) ) Info = new System.IO.FileInfo( path );
			else throw new Exception( "Invalid path" );
		}

		public override void OnPaint( VirtualWidget item )
		{
			PaintSelection( item );

			Paint.SetPen( IsFolder ? Theme.Yellow : Color.White );
			Paint.DrawIcon( item.Rect, IsFolder ? "folder" : "description", 18, TextFlag.LeftCenter );

			Paint.SetPen( Theme.Text );
			Paint.DrawText( item.Rect.Shrink( 24, 0, 0, 0 ), $"{Info.Name}", TextFlag.LeftCenter );
		}

		public int Order => Info is System.IO.DirectoryInfo ? 0 : 1;

		protected override void BuildChildren()
		{
			if ( Info is not System.IO.DirectoryInfo dirInfo )
				return;

			Clear();

			var infos = dirInfo.GetFileSystemInfos().Select( x => new FilesystemTreeNode( x.FullName ) );
			infos = infos.OrderBy( x => x.Order ).ThenBy( x => x.Info.Name );

			AddItems( infos );
		}
	}


	[WidgetGallery]
	[Title( "TreeView" )]
	[Icon( "account_tree" )]
	internal static Widget WidgetGallery()
	{
		var canvas = new Widget( null );
		canvas.Layout = Layout.Row();
		canvas.Layout.Spacing = 32;

		var view = new TreeView( canvas );
		view.HorizontalSizeMode = SizeMode.CanGrow;
		view.MinimumHeight = 700;
		var a = view.AddItem( new FilesystemTreeNode( FileSystem.Root.GetFullPath( "/" ) ) );
		var b = view.AddItem( new FilesystemTreeNode( AppContext.BaseDirectory ) );

		view.Open( a );
		view.Open( b );

		var property = new ControlSheet();
		property.AddProperty( view, x => x.ItemSpacing );
		property.AddProperty( view, x => x.IndentWidth );
		property.AddProperty( view, x => x.ExpandWidth );
		property.AddProperty( view, x => x.Margin );
		property.AddProperty( view, x => x.SmoothScrolling );
		property.AddProperty( view, x => x.MultiSelect );

		var filter = new LineEdit( canvas );
		filter.TextEdited += ( t ) =>
		{
			if ( string.IsNullOrWhiteSpace( t ) )
			{
				view.SetItems( [a, b] );
				return;
			}
			view.SetItems( AssetSystem.All.Where( x => x.Name.Contains( t ) ).OrderBy( x => x.Name ).Select( x => new FilesystemTreeNode( x.AbsolutePath ) ) );
		};

		canvas.Layout.Add( view, 1 );

		var rightCol = canvas.Layout.AddColumn();

		rightCol.Add( new Label.Subtitle( "Config" ) );
		rightCol.Add( property );
		rightCol.AddSpacingCell( 20 );
		rightCol.Add( new Label.Subtitle( "Filter" ) );
		rightCol.Add( filter );

		rightCol.AddStretchCell();

		return canvas;
	}
}
