namespace Editor.TextureEditor;

public class TextureRect : SceneCustomObject
{
	public Texture Texture { get; set; }

	public TextureRect( SceneWorld sceneWorld, Texture texture ) : base( sceneWorld )
	{
		Texture = texture;
	}

	public override void RenderSceneObject()
	{
		base.RenderSceneObject();

		if ( Texture == null )
			return;

		var textureSize = Texture.Size;
		var viewportSize = Graphics.Viewport.Size;
		textureSize *= Math.Min( viewportSize.x / textureSize.x, viewportSize.y / textureSize.y );

		Graphics.Attributes.SetComboEnum( "D_BLENDMODE", BlendMode.Normal );
		Graphics.Attributes.Set( "Texture", Texture );
		Graphics.Attributes.Set( "LayerMat", Matrix.Identity );
		Graphics.DrawQuad( new Rect( (viewportSize - textureSize) * 0.5f, textureSize ), Material.UI.Basic, Color.White );
	}
}

public class Preview : Widget
{
	private readonly RenderingWidget Rendering;

	public Texture Texture { set => Rendering.TextureRect.Texture = value; }

	public Preview( Widget parent ) : base( parent )
	{
		Name = "Preview";
		WindowTitle = "Preview";
		SetWindowIcon( "photo" );

		Layout = Layout.Column();

		Rendering = new RenderingWidget( this );
		Layout.Add( Rendering );
	}

	private class RenderingWidget : SceneRenderingWidget
	{
		public TextureRect TextureRect { get; private set; }

		public RenderingWidget( Widget parent ) : base( parent )
		{
			MouseTracking = true;
			FocusMode = FocusMode.Click;

			Scene = Scene.CreateEditorScene();

			using ( Scene.Push() )
			{
				{
					Camera = new GameObject( true, "camera" ).GetOrAddComponent<CameraComponent>( false );
					Camera.ZNear = 0.1f;
					Camera.ZFar = 4000;
					Camera.LocalRotation = new Angles( 0, 180, 0 );
					Camera.FieldOfView = 10;
					Camera.BackgroundColor = Color.Transparent;
					Camera.Enabled = true;
				}
			}

			TextureRect = new TextureRect( Scene.SceneWorld, null );
		}

		public override void OnDestroyed()
		{
			base.OnDestroyed();

			Scene?.Destroy();
			Scene = null;
		}
	}
}
