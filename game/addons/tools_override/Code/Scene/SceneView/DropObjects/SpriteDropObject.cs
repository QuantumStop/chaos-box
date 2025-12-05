using System.Threading;

namespace Editor;

[DropObject( "sprite", "sprite" )]
partial class SpriteDropObject : BaseDropObject
{
	Sprite sprite;
	Texture texture;
	float aspect = 1f;

	protected override async Task Initialize( string dragData, CancellationToken token )
	{
		Asset asset = await InstallAsset( dragData, token );

		if ( asset is null )
			return;

		if ( token.IsCancellationRequested )
			return;

		PackageStatus = "Loading Sprite";
		sprite = asset.LoadResource<Sprite>();
		if ( sprite is null )
		{
			PackageStatus = "Failed to load Sprite";
			return;
		}

		PackageStatus = "";
		if ( token.IsCancellationRequested )
			return;

		texture = sprite.Animations.FirstOrDefault()?.Frames.FirstOrDefault()?.Texture;
		if ( texture is null )
		{
			PackageStatus = "Sprite has no textures";
			return;
		}

		if ( texture.Height != 0 && texture.Width != 0 )
		{
			aspect = (float)texture.Height / texture.Width;
		}
	}

	public override void OnUpdate()
	{
		using var scope = Gizmo.Scope( "DropObject", traceTransform );

		Gizmo.Draw.Color = Color.White;
		if ( texture is not null && aspect != 0 )
		{
			Gizmo.Draw.Sprite( Vector3.Zero, new Vector2( 10f, 10f * aspect ), texture, true );
		}
		else
		{
			Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
			Gizmo.Draw.Sprite( Bounds.Center, 16, "materials/gizmo/downloads.png" );
		}

		if ( !string.IsNullOrWhiteSpace( PackageStatus ) )
		{
			Gizmo.Draw.Text( PackageStatus, new Transform( Bounds.Center ), "Inter", 14 * Application.DpiScale );
		}
	}

	public override async Task OnDrop()
	{
		await WaitForLoad();

		if ( texture is null )
			return;

		using var scene = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Drop Texture" ).WithGameObjectCreations().Push() )
		{
			GameObject = new GameObject();
			GameObject.Name = texture.ResourceName;
			GameObject.WorldTransform = traceTransform;

			var spriteComponent = GameObject.Components.GetOrCreate<SpriteRenderer>();
			spriteComponent.Sprite = sprite;

			EditorScene.Selection.Clear();
			EditorScene.Selection.Add( GameObject );
		}
	}
}
