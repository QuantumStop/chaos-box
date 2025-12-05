namespace Editor.TerrainEditor;

partial class TerrainComponentWidget : ComponentEditorWidget
{
	TerrainMaterialList MaterialList;

	bool FilterProperties( SerializedProperty o )
	{
		if ( o.PropertyType.IsAssignableTo( typeof( Delegate ) ) ) return false;
		if ( !o.HasAttribute<PropertyAttribute>() ) return false;

		// Stupid stuff, we shouldn't be forced to inherit this on Terrain @layla
		// But just hide it since it's useless and confusing right now
		if ( o.Name == nameof( Collider.Static ) ) return false;
		if ( o.Name == nameof( Collider.Surface ) ) return false;
		if ( o.Name == nameof( Collider.IsTrigger ) ) return false;

		return true;
	}

	Widget PaintPage()
	{
		var container = new Widget( null );

		container.Layout = Layout.Column();

		{
			var hmilayout = container.Layout.AddColumn();
			hmilayout.Spacing = 8;
			hmilayout.Margin = 16;

			var header = new Label( "Heightmap" );
			header.SetStyles( "font-weight: bold" );
			hmilayout.Add( header );

			var layout = hmilayout.AddRow();
			layout.Spacing = 8;
			layout.AddStretchCell();
			layout.Add( new Button( "Import Splatmap..." ) { Clicked = ImportSplatmap } );
			layout.Add( new Button( "Import..." ) { Clicked = ImportHeightmap } );
			layout.Add( new Button( "Export..." ) { Enabled = false } );
		}

		{
			var header = new Label( "Terrain Materials" );
			header.SetStyles( "font-weight: bold" );

			var terrain = SerializedObject.Targets.FirstOrDefault() as Terrain;

			MaterialList = new TerrainMaterialList( null, terrain );

			var tlayout = container.Layout.AddColumn();
			tlayout.Spacing = 8;
			tlayout.Margin = 16;
			tlayout.Add( header );
			tlayout.Add( MaterialList );

			var hlayout = tlayout.AddRow();
			hlayout.Spacing = 8;
			hlayout.AddStretchCell();

			var newTerrainMat = new Button( "New Terrain Material..." );
			newTerrainMat.Clicked += NewTerrainMaterial;

			var cloudMats = new Button( "Browse...", "cloud" );
			cloudMats.Clicked += () =>
			{
				var picker = AssetPicker.Create( null, AssetType.FromExtension( "tmat" ) );
				picker.OnAssetPicked = x =>
				{
					var material = x.First().LoadResource<TerrainMaterial>();
					terrain.Storage.Materials.Add( material );
					terrain.UpdateMaterialsBuffer();
					MaterialList?.BuildItems();
				};
				picker.Show();
			};

			hlayout.Add( cloudMats );
			hlayout.Add( newTerrainMat );

			var cs = new ControlSheet();
			cs.AddObject( terrain.Storage.MaterialSettings.GetSerialized() );
			tlayout.Add( cs );
		}

		return container;
	}

	void NewTerrainMaterial()
	{
		var filepath = EditorUtility.SaveFileDialog( "Create Terrain Material", "tmat", $"{Project.Current.GetAssetsPath()}/" );
		if ( filepath is null ) return;

		var asset = AssetSystem.CreateResource( "tmat", filepath );

		if ( !asset.TryLoadResource<TerrainMaterial>( out var material ) )
			return;

		asset.Compile( true );
		MainAssetBrowser.Instance?.Local.UpdateAssetList();

		var terrain = SerializedObject.Targets.FirstOrDefault() as Terrain;
		terrain.Storage.Materials.Add( material );
		terrain.UpdateMaterialsBuffer();
		MaterialList?.BuildItems();

		asset.OpenInEditor();
	}

	Widget ActualSettingsPage()
	{
		var container = new Widget( null );

		var sheet = new ControlSheet();
		sheet.AddObject( SerializedObject, FilterProperties );

		container.Layout = Layout.Column();
		container.Layout.Add( sheet );

		return container;
	}

	Widget SettingsPage()
	{
		var container = new Widget( null );

		var tabs = new TabWidget( this );
		tabs.AddPage( "Edit Terrain", "add_circle", PaintPage() );
		tabs.AddPage( "Settings", "settings", ActualSettingsPage() );

		container.Layout = Layout.Column();
		container.Layout.Add( tabs );

		return container;
	}

	public static Color32 BalanceWeights( Color32 color )
	{
		var sum = color.r + color.g + color.b;
		var a = (byte)Math.Max( 0, 255 - sum );
		return new Color32( color.r, color.g, color.b, a );
	}

	void ImportSplatmap()
	{
		if ( SerializedObject.Targets.FirstOrDefault() is not Terrain terrain )
			return;

		var fd = new FileDialog( null ) { Title = "Import Splatmap from image file..." };
		fd.SetFindFile();
		fd.SetModeOpen();
		fd.SetNameFilter( "Image File (*.png *.tga *.jpg *.psd)" );

		if ( !fd.Execute() )
			return;

		var storage = terrain.Storage;

		using ( var bitmap = EditorUtility.LoadBitmap( fd.SelectedFile ) )
		{
			Log.Info( storage.Resolution );
			bitmap.Resize( storage.Resolution, storage.Resolution );

			var data = bitmap.EncodeTo( ImageFormat.RGBA8888 );
			var numPixels = storage.Resolution * storage.Resolution;
			var controlmap = new Color32[numPixels];

			for ( var i = 0; i < numPixels; i++ )
			{
				var r = data[(i * 4) + 0];
				var g = data[(i * 4) + 1];
				var b = data[(i * 4) + 2];
				var a = data[(i * 4) + 3];

				controlmap[i] = BalanceWeights( new Color32( r, g, b, a ) );
			}

			storage.ControlMap = controlmap;
		}

		terrain.SyncGPUTexture();
	}

	public static (byte r, byte g, byte b, byte a) NormalizeRGBA( byte r, byte g, byte b, byte a )
	{
		// Calculate the total sum of the RGBA values
		int total = r + g + b + a;

		// Calculate the normalization factor
		double factor = 255.0 / total;

		// Normalize each component by multiplying with the factor and rounding to the nearest integer
		byte rNormalized = (byte)Math.Round( r * factor );
		byte gNormalized = (byte)Math.Round( g * factor );
		byte bNormalized = (byte)Math.Round( b * factor );
		byte aNormalized = (byte)Math.Round( a * factor );

		return (rNormalized, gNormalized, bNormalized, aNormalized);
	}

	void ImportHeightmap()
	{
		var terrain = SerializedObject.Targets.FirstOrDefault() as Terrain;
		if ( !terrain.IsValid() ) return;

		var fd = new FileDialog( null );
		fd.Title = "Import Heightmap from image file...";
		// fd.Directory = System.IO.Path.GetDirectoryName( assets.First().AbsolutePath );
		fd.SetFindFile();
		fd.SetModeOpen();
		fd.SetNameFilter( "Image File (*.raw *.r8 *.r16)" );

		if ( !fd.Execute() )
			return;

		new ImportHeightmapPopup( this, terrain, fd.SelectedFile );
	}
}
