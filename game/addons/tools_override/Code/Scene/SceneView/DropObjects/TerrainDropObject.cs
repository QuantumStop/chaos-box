using Sandbox;
using System.Threading;

namespace Editor;

[DropObject( "terrain", "terrain" )]
partial class TerrainDropObject : BaseDropObject
{
	Asset Asset;

	protected override async Task Initialize( string dragData, CancellationToken token )
	{
		Asset = await InstallAsset( dragData, token );

		if ( token.IsCancellationRequested )
			return;
	}

	public override async Task OnDrop()
	{
		await WaitForLoad();

		if ( Asset is null ) return;

		var storage = Asset.LoadResource<TerrainStorage>();
		if ( storage is null ) return;

		using var scene = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Drop Terrain" ).WithGameObjectCreations().Push() )
		{
			GameObject = new GameObject( true, storage.ResourceName );
			var terrain = GameObject.Components.Create<Terrain>( false );
			terrain.Storage = storage;
			terrain.Enabled = true;

			EditorScene.Selection.Clear();
			EditorScene.Selection.Add( GameObject );
		}

		await Task.CompletedTask;
	}
}

[DropObject( "terrainmaterial", "tmat" )]
partial class TerrainMaterialDropObject : BaseDropObject
{
	TerrainMaterial material;

	protected override async Task Initialize( string dragData, CancellationToken token )
	{
		Asset asset = await InstallAsset( dragData, token );

		if ( asset is null )
			return;

		if ( token.IsCancellationRequested )
			return;

		material = asset.LoadResource<TerrainMaterial>();
	}

	public override async Task OnDrop()
	{
		await WaitForLoad();

		if ( material is null ) return;
		if ( !trace.GameObject.IsValid() ) return;

		var c = trace.GameObject.Components.Get<Terrain>();
		if ( c.IsValid() )
		{
			c.Storage.Materials.Add( material );
			c.UpdateMaterialsBuffer();

			EditorScene.Selection.Clear();
			EditorScene.Selection.Add( trace.GameObject );
		}

		await Task.CompletedTask;
	}
}
