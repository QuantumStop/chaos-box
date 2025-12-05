using Sandbox;
using System.Threading;

namespace Editor;

[DropObject( "material", "vmat", "vmat_c" )]
partial class MaterialDropObject : BaseDropObject
{
	Material material;
	Component currentPreviewComponent;
	Material previewComponentOriginalMaterial;
	int currentPreviewTriangleIndex;

	protected override async Task Initialize( string dragData, CancellationToken token )
	{
		Asset asset = await InstallAsset( dragData, token );

		if ( asset is null )
			return;

		if ( token.IsCancellationRequested )
			return;

		material = asset.LoadResource<Material>();
	}

	public override void OnUpdate()
	{
		ResetPreviewMaterial();

		if ( material is not null )
		{
			if ( trace.GameObject.IsValid() )
			{
				var c = trace.GameObject.Components.Get<Component.IMaterialSetter>();

				if ( c is not null )
				{
					currentPreviewTriangleIndex = trace.Triangle;
					previewComponentOriginalMaterial = c.GetMaterial( currentPreviewTriangleIndex );
					currentPreviewComponent = (Component)c;

					c.SetMaterial( material, currentPreviewTriangleIndex );
				}
			}

		}

		if ( !string.IsNullOrWhiteSpace( PackageStatus ) )
		{
			var rot = Rotation.LookAt( trace.Normal, Vector3.Up ) * Rotation.From( 90, 0, 0 );
			var pos = trace.EndPosition + trace.Normal * PivotPosition.Length;

			using var scope = Gizmo.Scope( "DropObject", new Transform( pos, rot ) );
			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.Text( PackageStatus, new Transform( Bounds.Center ), "Inter", 14 * Application.DpiScale );

			Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
			Gizmo.Draw.Sprite( Bounds.Center + Vector3.Up * 12, 16, "materials/gizmo/downloads.png" );
		}
	}

	private void ResetPreviewMaterial()
	{
		if ( currentPreviewComponent.IsValid() )
		{
			Log.Info( $"Resetting material on {currentPreviewComponent}, {currentPreviewTriangleIndex}" );
			((Component.IMaterialSetter)currentPreviewComponent).SetMaterial( previewComponentOriginalMaterial, currentPreviewTriangleIndex );
		}
	}

	public override async Task OnDrop()
	{
		await WaitForLoad();

		ResetPreviewMaterial();

		if ( trace.GameObject.IsValid() )
		{
			var c = trace.GameObject.Components.Get<Component.IMaterialSetter>();
			if ( c is not null )
			{
				using ( SceneEditorSession.Active.UndoScope( "Drop Material" ).WithComponentChanges( c as Component ).Push() )
				{
					c.SetMaterial( material, trace.Triangle );
				}
			}
		}
	}

	public override void OnDestroy()
	{
		if ( !Dropped )
		{
			ResetPreviewMaterial();
		}
	}
}
