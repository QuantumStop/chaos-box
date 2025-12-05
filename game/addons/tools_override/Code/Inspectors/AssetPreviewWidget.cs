using Editor.Assets;

namespace Editor.Inspectors;

public class AssetPreviewWidget : Widget
{
	SceneRenderingWidget renderWidget;
	AssetPreview preview;
	Widget toolbar;

	bool initializing;

	public AssetPreviewWidget( AssetPreview p ) : base( null )
	{
		preview = p;

		VerticalSizeMode = SizeMode.CanGrow | SizeMode.Expand;
		HorizontalSizeMode = SizeMode.Flexible;

		Layout = Layout.Row();

		initializing = true;
		_ = InitAsync();
	}

	async Task InitAsync()
	{
		await preview.InitializeScene();
		await preview.InitializeAsset();

		for ( int i = 0; i < 4; i++ )
		{
			preview.TickScene( 0.5f );
		}

		using ( preview.Scene.Push() )
		{
			preview.UpdateScene( 0, RealTime.Delta );
		}

		if ( preview.CreateWidget( this ) is { } widget )
		{
			//
			// use a custom widget
			//

			Layout.Add( widget );
		}
		else if ( preview.Camera is not null )
		{
			//
			// set up rendering
			//

			renderWidget = Layout.Add( new SceneRenderingWidget() );
			renderWidget.Layout = Layout.Row();
			renderWidget.Scene = preview.Scene;

			preview.Camera.BackgroundColor = Theme.ControlBackground;
			renderWidget.OnPreFrame += PreFrame;
		}

		//
		// previews can create a little toolbar that is only visible on hover
		//
		toolbar = preview.CreateToolbar();
		if ( toolbar.IsValid() )
		{
			// need to parent to the render widget (a native window) if there's one, otherwise it won't be visible!
			toolbar.Parent = renderWidget.IsValid() ? renderWidget : this;
			toolbar.Visible = false;
		}

		if ( preview.Camera is null )
		{
			await UpdatePixmap();
		}

		initializing = false;
		Update();
	}

	protected override Vector2 SizeHint() => 400;


	protected override void OnMouseEnter()
	{
		base.OnMouseEnter();

		if ( toolbar.IsValid() )
		{
			toolbar.Visible = true;
		}
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();

		if ( toolbar.IsValid() )
		{
			toolbar.Visible = false;
		}
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		preview?.Dispose();
		preview = null;
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		if ( renderWidget.IsValid() )
		{
			renderWidget.Position = 0;
			renderWidget.Size = Size;
		}

		if ( toolbar.IsValid() )
		{
			toolbar.AdjustSize();
			toolbar.AlignToParent( TextFlag.LeftBottom, 8 );
		}
	}


	Pixmap pixmap;

	public async Task UpdatePixmap()
	{
		var w = new Pixmap( Size * DpiScale );
		await preview.RenderToPixmap( w );
		pixmap = w;

		Update();
	}

	protected override void OnPaint()
	{
		if ( initializing )
			return;

		if ( preview.Camera is not null )
			return;

		if ( pixmap is not null )
		{
			Paint.Draw( LocalRect, pixmap );
		}
	}

	void PreFrame()
	{
		if ( initializing || preview?.Camera is null )
			return;

		using ( preview.Scene.Push() )
		{
			preview.ScreenSize = (Vector2Int)Size;
			preview.UpdateScene( RealTime.Now * preview.PreviewWidgetCycleSpeed, RealTime.Delta );
		}
	}
}
