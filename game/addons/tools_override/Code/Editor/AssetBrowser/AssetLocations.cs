using System.IO;

namespace Editor;

public class AssetLocations : TreeView
{
	/// <summary>
	/// Called when a folder is selected.
	/// </summary>
	public Action<LocalAssetBrowser.Location> OnFolderSelected;

	/// <summary>
	/// Called when a "filter" is selected, i.e. "@recent" or "t:vmdl".
	/// </summary>
	public Action<string> OnFilterSelected;

	public static bool IncludePathNames
	{
		get => ProjectCookie.Get( "AssetLocations.IncludePathNames", false );
		set => ProjectCookie.Set( "AssetLocations.IncludePathNames", value );
	}

	public AssetBrowser Browser;

	public AssetLocations( AssetBrowser parent ) : base( parent )
	{
		// Sorry logic
		SetStyles( $"AssetLocations{{ border: 8px solid {Theme.ControlBackground.Hex}; }}" );

		MinimumSize = 200;
		ItemSelected = OnItemClicked;

		BuildLocations();
	}

	[Event( "assetsystem.newfolder" )]
	void Refresh()
	{
		Clear();
		BuildLocations();
	}

	protected virtual void BuildLocations()
	{

	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect );

		base.OnPaint();
	}

	protected void OnItemClicked( object value )
	{
		if ( value is not AssetBrowser.Location location )
			return;

		OnFolderSelected?.Invoke( location );
	}

	protected override void OnDoubleClick( MouseEvent e )
	{
		// Avoid calling OnItemActivated if we double click the expand button
		var item = GetItemAt( e.LocalPosition );
		if ( e.LeftMouseButton && item is not null && item.HasChildren )
		{
			var expandRect = item.Rect;
			expandRect.Left += IndentWidth * item.Column;
			expandRect.Width = ExpandWidth;

			if ( expandRect.IsInside( e.LocalPosition ) )
			{
				e.Accepted = true;
				return;
			}
		}

		base.OnDoubleClick( e );
	}

	protected override void OnDragHoverItem( DragEvent ev, VirtualWidget item )
	{
		base.OnDragHoverItem( ev, item );
		ev.Action = DropAction.Move;
	}

	protected override void OnDropOnItem( DragEvent ev, VirtualWidget item )
	{
		if ( !ev.Data.HasFileOrFolder )
			return;

		if ( item.Object is not TreeNode node )
			return;

		if ( node.Value is not DirectoryInfo dirInfo )
			return;

		var directory = dirInfo.FullName;

		foreach ( var file in ev.Data.Files )
		{
			var asset = AssetSystem.FindByPath( file );

			if ( asset == null )
				continue;

			ev.Action = ev.HasCtrl ? DropAction.Copy : DropAction.Move;

			if ( ev.HasCtrl )
				EditorUtility.CopyAssetToDirectory( asset, directory );
			else
				EditorUtility.MoveAssetToDirectory( asset, directory );
		}
	}

	/// <summary>
	/// Finds a node with a path and toggles it open
	/// </summary>
	public void SelectFolder( string path )
	{
		if ( string.IsNullOrEmpty( path ) )
			throw new ArgumentNullException( nameof( path ) );

		var queue = new Queue<TreeNode>( _items.OfType<TreeNode>() );

		UnselectAll( true );

		while ( queue.Count > 0 )
		{
			var node = queue.Dequeue();

			if ( node is FolderNode folderNode )
			{
				if ( folderNode.Value.Path.Equals( path, StringComparison.OrdinalIgnoreCase ) )
				{
					SetSelected( folderNode, true, true );
					ScrollTo( folderNode.Value );
					return;
				}
			}

			foreach ( var child in node.Children )
			{
				queue.Enqueue( child );
			}
		}
	}

	public void SetIncludePathNames( bool enabled )
	{
		IncludePathNames = enabled;
	}
}
