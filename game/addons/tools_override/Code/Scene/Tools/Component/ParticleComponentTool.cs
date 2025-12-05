namespace Editor;

public class ParticleEditorTool : EditorTool<ParticleEffect>
{
	ParticleToolWindow window;

	public override void OnEnabled()
	{
		window = new ParticleToolWindow();
		AddOverlay( window, TextFlag.RightBottom, 10 );
	}

	public override void OnUpdate()
	{
		window.ToolUpdate();
	}

	public override void OnDisabled()
	{

	}

	public override void OnSelectionChanged()
	{
		var effect = GetSelectedComponent<ParticleEffect>();
		window.OnSelectionChanged( effect );
	}
}

class ParticleToolWindow : WidgetWindow
{
	ParticleEffect targetComponent;
	Button PauseButton;

	static bool IsClosed = false;

	public ParticleToolWindow()
	{
		ContentMargins = 0;
		Layout = Layout.Column();
	}

	private float PlaybackTime
	{
		get
		{
			if ( !targetComponent.IsValid() )
				return 0.0f;

			var emitter = targetComponent.Components.GetInAncestorsOrSelf<ParticleEmitter>();
			if ( !emitter.IsValid() )
				return 0.0f;

			return emitter.time;
		}
	}

	private int ParticleCount
	{
		get
		{
			if ( !targetComponent.IsValid() )
				return 0;

			return targetComponent.ParticleCount;
		}
	}

	void Restart()
	{
		if ( !targetComponent.IsValid() )
			return;

		targetComponent.Clear();
		targetComponent.ResetEmitters();

		if ( targetComponent.Paused )
		{
			PauseToggle();
		}
	}

	void PauseToggle()
	{
		if ( !targetComponent.IsValid() )
			return;

		targetComponent.Paused = !targetComponent.Paused;

		if ( PauseButton.IsValid() )
			PauseButton.Text = targetComponent.Paused ? "Play" : "Pause";
	}

	void Rebuild()
	{
		Layout.Clear( true );
		Layout.Margin = 0;
		Icon = IsClosed ? "" : "shower";
		IsGrabbable = !IsClosed;
		MaximumWidth = 200;

		UpdateTitle();

		if ( IsClosed )
		{
			var closedRow = Layout.AddRow();
			closedRow.Add( new IconButton( "shower", () => { IsClosed = false; Rebuild(); } ) { ToolTip = "Open", FixedHeight = HeaderHeight, FixedWidth = HeaderHeight, Background = Color.Transparent } );
			return;
		}

		var headerRow = Layout.AddRow();
		headerRow.AddStretchCell();
		headerRow.Add( new IconButton( "close", CloseWindow ) { ToolTip = "Close", FixedHeight = HeaderHeight, FixedWidth = HeaderHeight, Background = Color.Transparent } );

		var buttonRow = Layout.AddRow();
		buttonRow.Spacing = 2;
		buttonRow.AddStretchCell();

		PauseButton = new Button( "Pause" )
		{
			Clicked = PauseToggle,
			FixedWidth = 40
		};

		buttonRow.Add( PauseButton );
		buttonRow.Add( new Button( "Restart" ) { Clicked = Restart } );
		buttonRow.AddStretchCell();

		if ( targetComponent.IsValid() )
		{
			PauseButton.Text = targetComponent.Paused ? "Play" : "Pause";

			var sheet = new ControlSheet();
			var so = targetComponent.GetSerialized();
			sheet.AddProperty( this, x => x.ParticleCount );
			sheet.AddProperty( this, x => x.PlaybackTime );
			Layout.Add( sheet );
		}

		Layout.Margin = 4;
	}

	void CloseWindow()
	{
		IsClosed = true;
		Release();
		Rebuild();
		Position = Parent.Size - 32;
	}

	public void ToolUpdate()
	{
		if ( !targetComponent.IsValid() )
			return;
	}

	internal void OnSelectionChanged( ParticleEffect effect )
	{
		targetComponent = effect;

		Rebuild();
	}

	void UpdateTitle()
	{
		if ( !IsClosed )
		{
			if ( targetComponent.IsValid() && targetComponent.GameObject.IsValid() )
			{
				WindowTitle = $"Particles - {targetComponent.GameObject.Name}";
			}
			else
			{
				WindowTitle = "Particles";
			}
		}
		else
		{
			WindowTitle = "";
		}
	}
}
