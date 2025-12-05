namespace Editor.TerrainEditor;

partial class TerrainComponentWidget
{
	Widget CreateTerrain()
	{
		var container = new Widget( null );

		var tabs = new TabWidget( this );
		tabs.AddPage( "Create New", "add_circle", CreateNewTerrain() );
		tabs.AddPage( "Import From Heightmap", "description", new Widget( this ) );

		container.Layout = Layout.Column();
		container.Layout.Add( tabs );

		return container;
	}

	public class CreateOptions
	{
		public enum HeightMapSizes
		{
			[Title( "512x512" )] S512 = 512,
			[Title( "1024x1024" )] S1024 = 1024,
			[Title( "2048x2048" )] S2048 = 2048,
			[Title( "4096x4096" )] S4096 = 4096,
			[Title( "8192x8192" )] S8192 = 8192,
		}

		[Property, Title( "Heightmap Size" )] public HeightMapSizes HeightMapSize { get; set; } = HeightMapSizes.S512;
		[Property, Title( "World Scale (inches)" )] public float SizeScale { get; set; } = 39;
		[Property, Title( "Max Height (inches)" )] public float MaxTerrainHeight { get; set; } = 10000;

		[Title( "Total Size (inches)" )] public float TotalWorldSize => (int)HeightMapSize * SizeScale;
	}

	public CreateOptions Options = new();

	Widget CreateNewTerrain()
	{
		var container = new Widget( null );

		var sheet = new ControlSheet();

		sheet.AddObject( Options.GetSerialized() );

		var done = new Button.Primary( "Create New Terrain" );
		done.ToolTip = "Create a new terrain asset file";
		done.Clicked += Create;

		var link = new Button.Clear( "Link Existing" );
		link.ToolTip = "Link to an existing terrain asset file";
		link.Clicked += LinkExistingTerrain;

		var hlayout = Layout.Row();
		hlayout.Spacing = 8;
		hlayout.AddStretchCell();
		hlayout.Add( link );
		hlayout.Add( done );

		container.Layout = Layout.Column();
		container.Layout.Spacing = 8;
		container.Layout.Add( sheet );
		container.Layout.Add( hlayout );

		return container;
	}

	async void Create()
	{
		var saveLocation = EditorUtility.SaveFileDialog( $"Save Terrain As..", "terrain", $"{Project.Current.GetAssetsPath()}/untitled.terrain" );
		if ( saveLocation == null ) return;

		var asset = AssetSystem.CreateResource( "terrain", saveLocation );
		await asset.CompileIfNeededAsync();
		if ( !asset.TryLoadResource<TerrainStorage>( out var storage ) )
		{
			Log.Error( "Couldn't load terrain storage resource - this can happen if the resource itself doesn't exist" );
			return;
		}

		storage.SetResolution( (int)Options.HeightMapSize );
		storage.TerrainSize = Options.TotalWorldSize;
		storage.TerrainHeight = Options.MaxTerrainHeight;

		asset.SaveToDisk( storage );

		var terrain = SerializedObject.Targets.FirstOrDefault() as Terrain;
		if ( !terrain.IsValid() ) return;

		terrain.Storage = storage;

		// Rebuild UI, there is now storage
		BuildUI();
	}

	void LinkExistingTerrain()
	{
		var openLocation = EditorUtility.OpenFileDialog( $"Open Terrain", "terrain", $"{Project.Current.GetAssetsPath()}/" );
		if ( openLocation is null ) return;

		var terrain = SerializedObject.Targets.FirstOrDefault() as Terrain;
		if ( !terrain.IsValid() ) return;

		var asset = AssetSystem.FindByPath( openLocation );
		if ( asset is null || !asset.TryLoadResource<TerrainStorage>( out var storage ) )
		{
			Log.Error( "Fucked" );
			return;
		}

		terrain.Storage = storage;
		BuildUI();
	}

	Widget ImportNewHeightmap()
	{
		var container = new Widget( null );

		var sheet = new ControlSheet();

		sheet.AddObject( Options.GetSerialized() );

		var done = new Button.Primary( "Create New Terrain From Heightmap" );
		done.Clicked += Create;

		var hlayout = Layout.Row();
		hlayout.AddStretchCell();
		hlayout.Add( done );

		container.Layout = Layout.Column();
		container.Layout.Spacing = 8;
		container.Layout.Add( sheet );
		container.Layout.Add( hlayout );

		return container;
	}
}
