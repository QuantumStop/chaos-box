namespace Editor.Assets;

[AssetPreview( "jpg" )]
[AssetPreview( "vtex" )]
class PreviewImage : AssetPreview
{
	internal Texture texture;

	public override bool IsAnimatedPreview => false;

	public PreviewImage( Asset asset ) : base( asset )
	{
		if ( asset.AssetType == AssetType.ImageFile )
		{
			texture = Texture.Load( Asset.GetSourceFile() );
		}
		else
		{
			texture = Texture.Load( Asset.Path );
		}
	}

	public override Task InitializeAsset()
	{
		using ( Scene.Push() )
		{
			PrimaryObject = new GameObject();
			PrimaryObject.WorldTransform = Transform.Zero;

			var sprite = PrimaryObject.AddComponent<SpriteRenderer>();
			sprite.Sprite = new Sprite()
			{
				Animations = [new()
				{
					Name = "Default",
					Frames = [ new Sprite.Frame { Texture = texture }]
				}]
			};
			sprite.Size = new Vector2( 16, 16 );

			Camera.Orthographic = true;
			Camera.OrthographicHeight = 16;
		}

		return Task.CompletedTask;
	}

	public override void UpdateScene( float cycle, float timeStep )
	{
		base.UpdateScene( cycle, timeStep );

		Camera.Orthographic = true;
		Camera.OrthographicHeight = 16;
		Camera.WorldPosition = Vector3.Forward * -200;
		Camera.WorldRotation = Rotation.LookAt( Vector3.Forward );
	}
}
