using Editor.Assets;

namespace Editor.TerrainEditor;

[AssetPreview( "tmat" )]
class PreviewTerrainMaterial : AssetPreview
{
	public override float PreviewWidgetCycleSpeed => 0.1f;

	public PreviewTerrainMaterial( Asset asset ) : base( asset ) { }

	public override async Task InitializeAsset()
	{
		using ( EditorUtility.DisableTextureStreaming() )
		{
			if ( !Asset.TryLoadResource<TerrainMaterial>( out var material ) )
				return;

			using ( Scene.Push() )
			{
				PrimaryObject = new GameObject();

				var mr = PrimaryObject.AddComponent<ModelRenderer>();
				mr.Model = Model.Sphere;
				mr.MaterialOverride = Material.FromShader( "shaders/terrain_preview.shader" );
				mr.SceneObject.Attributes.Set( "BCR", material.BCRTexture );
				mr.SceneObject.Attributes.Set( "NHO", material.NHOTexture );
			}

			SceneSize = PrimaryObject.GetBounds().Size * 0.6f;
			SceneCenter = PrimaryObject.GetBounds().Center;
		}

		await Task.CompletedTask;
	}
}
