namespace Editor.Assets;

[AssetPreview( "vmdl" )]
class PreviewModel : AssetPreview
{
	public override float PreviewWidgetCycleSpeed => 0.2f;

	SkinnedModelRenderer modelRenderer;
	SkinnedModelRenderer arms;

	public PreviewModel( Asset asset ) : base( asset )
	{

	}

	/// <summary>
	/// Create the model or whatever needs to be viewed
	/// </summary>
	public override async Task InitializeAsset()
	{
		var model = await Model.LoadAsync( Asset.Path );
		if ( model is null ) return;

		using ( EditorUtility.DisableTextureStreaming() )
		{
			using ( Scene.Push() )
			{
				SceneCenter = model.RenderBounds.Center;
				SceneSize = Vector3.Zero;

				if ( model.MeshCount == 0 )
					return;

				PrimaryObject = new GameObject( true, "preview model" );
				PrimaryObject.WorldTransform = Transform.Zero;

				modelRenderer = PrimaryObject.AddComponent<SkinnedModelRenderer>();
				modelRenderer.PlayAnimationsInEditorScene = true;
				modelRenderer.Model = model;

				bool isViewModel = System.IO.Path.GetFileName( model.Name ).StartsWith( "v_" );

				if ( isViewModel )
				{
					var armsgo = new GameObject();
					armsgo.Parent = PrimaryObject;

					arms = armsgo.AddComponent<SkinnedModelRenderer>();
					arms.Model = Model.Load( "models/first_person/first_person_arms_preview.vmdl" );
					arms.BoneMergeTarget = modelRenderer;
				}

				SceneSize = model.Bounds.Size;
				SceneCenter = model.Bounds.Center;
			}
		}
	}

	public override void UpdateScene( float cycle, float timeStep )
	{
		base.UpdateScene( cycle, timeStep );

		UpdateViewModelScene( timeStep );
	}

	private bool UpdateViewModelScene( float timeStep )
	{
		if ( !arms.IsValid() )
			return false;

		modelRenderer.WorldTransform = Transform.Zero;

		var r_upperarm = modelRenderer.Model.Bones.GetBone( "r_upperarm" );
		var l_upperarm = modelRenderer.Model.Bones.GetBone( "l_upperarm" );

		var camera = modelRenderer.Model.Bones.GetBone( "camera" );
		if ( camera is not null && modelRenderer.TryGetBoneTransform( "camera", out var tx ) )
		{
			Camera.WorldPosition = tx.Position;
			Camera.WorldRotation = tx.Rotation;
			Camera.FieldOfView = 85;
			Camera.ZNear = 0.1f;
			Camera.ZFar = 2000;
		}

		return true;
	}

	public override Widget CreateToolbar()
	{
		var info = new IconButton( "settings" );
		info.Layout = Layout.Row();
		info.MinimumSize = 16;
		info.MouseLeftPress = () => OpenSettings( info );

		return info;
	}

	public void OpenSettings( Widget parent )
	{
		var popup = new PopupWidget( parent );
		popup.IsPopup = true;


		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;

		var ps = new ControlSheet();

		ps.AddProperty( Camera, x => x.BackgroundColor );
		//	ps.AddProperty( PrimarySceneObject, x => x.ColorTint );
		//	ps.AddProperty( Camera, x => x.EnablePostProcessing );

		popup.Layout.Add( ps );
		popup.MaximumWidth = 300;
		popup.Show();
		popup.Position = parent.ScreenRect.TopRight - popup.Size;
		popup.ConstrainToScreen();

	}

}
