namespace Editor.Assets;

[AssetPreview( "decal" )]
class PreviewDecal : AssetPreview
{
	public override float PreviewWidgetCycleSpeed => 0.2f;

	public PreviewDecal( Asset asset ) : base( asset )
	{

	}

	public override Task InitializeAsset()
	{
		var decal = Asset.LoadResource<DecalDefinition>();
		if ( decal is null ) return Task.CompletedTask;

		using ( Scene.Push() )
		{
			PrimaryObject = new GameObject();
			PrimaryObject.WorldTransform = Transform.Zero;

			var plane = PrimaryObject.AddComponent<ModelRenderer>();
			plane.Model = Model.Plane;
			plane.LocalScale = new Vector3( 10, 10, 10 );
			plane.MaterialOverride = Material.Load( "materials/dev/reflectivity_30.vmat" );
			plane.Tint = new Color( 0.02f, 0.04f, 0.03f ); // greenish hue, let the boy watch

			var decalgo = new GameObject( false );
			decalgo.Parent = PrimaryObject;

			var decal_component = decalgo.AddComponent<Decal>();
			decal_component.WorldRotation = new Angles( 90, 0, 0 );
			decal_component.WorldScale = 1;
			decal_component.Decals = [decal];
			decal_component.Rotation = 0;
			decal_component.Depth = 3;

			decalgo.Enabled = true;

			SceneCenter = decal_component.WorldBounds.Center;
			SceneSize = decal_component.WorldBounds.Size;
		}

		return Task.CompletedTask;
	}

	public override void UpdateScene( float cycle, float timeStep )
	{
		base.UpdateScene( cycle, timeStep );

		Camera.WorldPosition = Vector3.Up * 300;
		Camera.WorldRotation = new Angles( 80, -5, 0 );

		Scene.Get<DirectionalLight>().WorldRotation = new Angles( 60, 180, 0 );

		FrameScene();
	}

}
