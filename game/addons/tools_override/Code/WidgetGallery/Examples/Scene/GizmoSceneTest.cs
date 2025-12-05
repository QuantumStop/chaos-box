namespace Editor.Widgets;

public interface ISceneTest
{
	public void Initialize( CameraComponent camera ) { }
	public void Frame();
}

public class GizmoSceneTest : Widget
{
	private readonly SceneRenderingWidget RenderCanvas;
	private readonly CameraComponent Camera;
	private readonly Gizmo.Instance GizmoInstance;

	public GizmoSceneTest( Widget parent ) : base( parent )
	{
		MinimumSize = 50;

		Layout = Layout.Row();

		RenderCanvas = new SceneRenderingWidget( this );
		RenderCanvas.OnPreFrame += OnPreFrame;
		RenderCanvas.FocusMode = FocusMode.Click;
		RenderCanvas.Scene = Scene.CreateEditorScene();

		using ( RenderCanvas.Scene.Push() )
		{
			Camera = new GameObject( true, "camera" ).GetOrAddComponent<CameraComponent>( false );
			Camera.BackgroundColor = Theme.Blue;
			Camera.ZFar = 4096;
			Camera.Enabled = true;

			{
				var light = new GameObject( true, "light" ).GetOrAddComponent<AmbientLight>( false );
				light.Color = Theme.Blue * 0.4f;
				light.Enabled = true;
			}
			{
				var light = new GameObject( true, "light" ).GetOrAddComponent<DirectionalLight>( false );
				light.WorldRotation = Rotation.From( 45, 45, 0 );
				light.LightColor = Color.White;
				light.Enabled = true;
			}

			RenderCanvas.Camera = Camera;
		}

		GizmoInstance = RenderCanvas.GizmoInstance;

		var itemList = new ListView( this );
		itemList.MinimumSize = 200;
		itemList.ItemSize = new Vector2( -1, 32 );
		itemList.ItemSelected = OnSwitchScene;
		itemList.ItemPaint = w =>
		{
			if ( w.Object is not TypeDescription type )
				return;

			w.PaintBackground( Theme.WidgetBackground, Theme.ControlRadius );

			Paint.SetPen( Theme.Text );
			Paint.DrawIcon( w.Rect.Shrink( 8, 0 ), type.Icon, 22, TextFlag.LeftCenter );
			Paint.DrawText( w.Rect.Shrink( 38, 0 ), type.Title, TextFlag.LeftCenter );
		};

		var column = Layout.AddColumn( 1 );

		column.Add( RenderCanvas, 1 );

		Layout.Add( itemList );

		var types = EditorTypeLibrary.GetTypes<ISceneTest>()
			.Where( x => !x.IsInterface )
			.OrderBy( x => x.Title )
			.ToArray();

		foreach ( var type in types )
		{
			if ( type.IsInterface ) continue;

			itemList.AddItem( type );
		}

		itemList.SelectItem( types.FirstOrDefault() );
	}

	ISceneTest current;

	void OnSwitchScene( object o )
	{
		if ( o is not TypeDescription type )
			return;

		current = type.Create<ISceneTest>();

		Camera.FieldOfView = 70;

		current.Initialize( Camera );
	}

	private void OnPreFrame()
	{
		GizmoInstance.Input.IsHovered = IsActiveWindow && RenderCanvas.IsUnderMouse;

		if ( GizmoInstance.FirstPersonCamera( Camera, RenderCanvas ) )
		{
			GizmoInstance.Input.IsHovered = false;
		}

		RenderCanvas.UpdateGizmoInputs( GizmoInstance.Input.IsHovered );

		if ( Gizmo.ControlMode == "firstperson" )
		{
			Gizmo.Draw.Color = Gizmo.HasHovered ? Color.White : Color.Black.WithAlpha( 0.3f );
			Gizmo.Draw.LineSphere( new Sphere( Camera.WorldPosition + Camera.WorldRotation.Forward * 50.0f, 0.1f ) );
		}

		if ( current != null )
		{
			current?.Frame();

			var info = DisplayInfo.For( current );

			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.ScreenText( info.Description, new Vector2( 30, Gizmo.Camera.Size.y - 30.0f ), flags: TextFlag.LeftBottom );
		}

		Cursor = Gizmo.HasHovered ? CursorShape.Finger : CursorShape.Arrow;
	}

	[WidgetGallery]
	[Title( "Gizmo Tests" )]
	[Icon( "web" )]
	internal static Widget WidgetGallery()
	{
		var canvas = new GizmoSceneTest( null );

		return canvas;
	}
}
