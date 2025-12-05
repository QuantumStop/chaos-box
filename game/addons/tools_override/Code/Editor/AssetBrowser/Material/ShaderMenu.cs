namespace Editor;

internal static class ShaderMenu
{
	[Event( "asset.contextmenu", Priority = 50 )]
	public static void OnShaderFileAssetContext( AssetContextMenu e )
	{
		// Are all the files we have selected image assets?
		if ( !e.SelectedList.All( x => x.AssetType == AssetType.Shader ) )
			return;

		if ( e.SelectedList.Count == 1 )
			e.Menu.AddOption( $"Create Material", "image", action: () => CreateMaterialUsingShader( e.SelectedList.FirstOrDefault() ) );
	}

	private static void CreateMaterialUsingShader( AssetEntry entry )
	{
		var asset = entry.Asset;
		var assetName = asset.Name;

		var fd = new FileDialog( null );
		fd.Title = "Create Material from Shader..";
		fd.Directory = System.IO.Path.GetDirectoryName( asset.AbsolutePath );
		fd.DefaultSuffix = ".vmat";
		fd.SelectFile( $"{assetName}.vmat" );
		fd.SetFindFile();
		fd.SetModeSave();
		fd.SetNameFilter( "Material File (*.vmat)" );

		if ( !fd.Execute() )
			return;

		var shaderPath = asset.GetCompiledFile();

		var file = $@"
Layer0
{{
	shader ""{shaderPath}""

}}
";
		System.IO.File.WriteAllText( fd.SelectedFile, file );

		var resultAsset = AssetSystem.RegisterFile( fd.SelectedFile );

		// These 3 lines are gonna be quite common I think.
		MainAssetBrowser.Instance?.Local.UpdateAssetList();
		MainAssetBrowser.Instance?.Local.FocusOnAsset( resultAsset );
		EditorUtility.InspectorObject = resultAsset;
	}
}
