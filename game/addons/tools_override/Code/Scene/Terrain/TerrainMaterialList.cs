using Sandbox;

namespace Editor.TerrainEditor;

public class TerrainMaterialList : ListView
{
	Terrain Terrain;

	public TerrainMaterialList( Widget parent, Terrain terrain ) : base( parent )
	{
		ItemContextMenu = ShowItemContext;
		ItemSelected = OnItemClicked;
		ItemActivated = OnItemDoubleClicked;
		Margin = 8;
		ItemSpacing = 4;
		AcceptDrops = true;
		MinimumHeight = 100;

		ItemSize = new Vector2( 68, 68 + 16 );
		ItemAlign = Sandbox.UI.Align.FlexStart;

		Terrain = terrain;

		BuildItems();
	}

	protected void OnItemClicked( object value )
	{
		if ( value is not TerrainMaterial material )
			return;

		PaintTextureTool.SplatChannel = Terrain.Storage.Materials.IndexOf( material );
	}

	protected void OnItemDoubleClicked( object obj )
	{
		if ( obj is not TerrainMaterial entry ) return;
		var asset = AssetSystem.FindByPath( entry.ResourcePath );
		asset?.OpenInEditor();
	}

	public override void OnDragHover( DragEvent ev )
	{
		base.OnDragHover( ev );

		// accept terrain materials
		foreach ( var dragAsset in ev.Data.Assets )
		{
			if ( dragAsset.AssetPath?.EndsWith( ".tmat" ) ?? false )
				continue;

			ev.Action = DropAction.Link;
			break;
		}
	}

	public override void OnDragDrop( DragEvent ev )
	{
		base.OnDragDrop( ev );

		AddMaterials( ev.Data.Assets );
	}

	private async void AddMaterials( IEnumerable<DragAssetData> draggedAssets )
	{
		foreach ( var dragAsset in draggedAssets )
		{
			var asset = await dragAsset.GetAssetAsync();
			if ( asset.TryLoadResource<TerrainMaterial>( out var material ) )
			{
				if ( Terrain.Storage.Materials.Contains( material ) ) continue;
				Terrain.Storage.Materials.Add( material );
				Terrain.UpdateMaterialsBuffer();
			}
		}

		BuildItems();
	}

	private void ShowItemContext( object obj )
	{
		if ( obj is not TerrainMaterial entry ) return;

		var m = new ContextMenu( this );
		m.AddOption( "Open In Editor", "edit", () =>
		{
			var asset = AssetSystem.FindByPath( entry.ResourcePath );
			asset?.OpenInEditor();
		} );

		m.AddOption( "Remove", "delete", () =>
		{
			Terrain.Storage.Materials.Remove( entry );
			Terrain.UpdateMaterialsBuffer();
			BuildItems();
		} );

		m.OpenAtCursor();
	}

	public void BuildItems()
	{
		SetItems( Terrain.Storage.Materials.Cast<object>() );
	}

	protected override void PaintItem( VirtualWidget item )
	{
		var rect = item.Rect.Shrink( 0, 0, 0, 16 );

		if ( item.Object is not TerrainMaterial material )
			return;

		var asset = AssetSystem.FindByPath( material.ResourcePath );

		// Should never happen if I'm not a noob
		if ( asset is null )
		{
			Paint.SetDefaultFont();
			Paint.SetPen( Color.Red );
			Paint.DrawText( item.Rect.Shrink( 2 ), "<ERROR>", TextFlag.Center );
			return;
		}

		if ( item.Selected || Paint.HasMouseOver )
		{
			Paint.SetBrush( Theme.Blue.WithAlpha( item.Selected ? 0.5f : 0.2f ) );
			Paint.ClearPen();
			Paint.DrawRect( item.Rect, 4 );
		}

		var pixmap = asset.GetAssetThumb();

		Paint.Draw( rect.Shrink( 2 ), pixmap );

		Paint.SetDefaultFont();
		Paint.SetPen( Theme.Text );
		Paint.DrawText( item.Rect.Shrink( 2 ), material.ResourceName, TextFlag.CenterBottom );
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, 4 );

		base.OnPaint();
	}
}
