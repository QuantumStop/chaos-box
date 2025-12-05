using Editor.AssetBrowsing.Nodes;
using Facepunch.ActionGraphs;
using System.IO;

namespace Editor;

public class CloudLocations : TreeView
{
	/// <summary>
	/// Called when a "filter" is selected, i.e. "@recent" or "type:model".
	/// </summary>
	public Action<string> OnFilterSelected;


	public CloudLocations( CloudAssetBrowser parent ) : base( parent )
	{
		MinimumSize = 200;
		ItemSelected = OnItemClicked;

		if ( Global.IsApiConnected )
		{
			//
			// Cloud
			//
			{
				var browse = new AssetFilterNode( "search", "Browse", "" );
				AddItem( browse );

				if ( parent.FilterAssetTypes is null )
				{
					AddItem( new AssetFilterNode( "chair", "Models", "type:model" ) );
					AddItem( new AssetFilterNode( "broken_image", "Materials", "type:material" ) );
					AddItem( new AssetFilterNode( "map", "Map", "type:map" ) );
					AddItem( new AssetFilterNode( "volume_up", "Sound", "type:sound" ) );
				}
			}

			AddItem( new TreeNode.Spacer( 10 ) );
		}

		{
			//
			// Project
			//
			AddItem( new CloudLocalNode() );

			AddItem( new TreeNode.Spacer( 10 ) );
		}

		if ( Global.IsApiConnected )
		{
			//
			// Collections
			//
			{
				var collections = new CloudCollectionsNode();

				AddItem( collections );
				Open( collections );

				AddItem( new TreeNode.Spacer( 10 ) );

				collections.OnLoaded = SetDefaultView;
			}

			//
			// Organisations
			//
			{
				var orgs = new CloudAccountNode();

				AddItem( orgs );
				Open( orgs );

				AddItem( new TreeNode.Spacer( 10 ) );
			}
		}
	}

	private void SetDefaultView()
	{
		bool foundDefaultView = false;

		void FindDefaultView( object item )
		{
			if ( item is AssetFilterNode { IsDefaultView: true } node )
			{
				SetSelected( node, true, false );
				foundDefaultView = true;
				return;
			}

			if ( item is TreeNode parent )
			{
				foreach ( var child in parent.Children )
				{
					if ( !foundDefaultView )
						FindDefaultView( child );
				}
			}
		}

		foreach ( var item in Items )
		{
			if ( !foundDefaultView )
				FindDefaultView( item );
		}

		if ( !foundDefaultView )
		{
			SetSelected( Items.First(), true, true );
		}
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
		if ( value is not AssetFilterNode filterNode )
			return;

		OnFilterSelected?.Invoke( filterNode.Filter );
	}
}
