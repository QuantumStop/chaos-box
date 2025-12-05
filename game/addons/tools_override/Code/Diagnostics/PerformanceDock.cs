using Sandbox.Diagnostics;
namespace Editor;

[Dock( "Editor", "Performance", "timer" )]
public class PerformanceDock : Widget
{
	RealtimeChart Chart;
	Button MenuButton;

	public float RefreshSpeed = 0.25f;

	public PerformanceDock( Widget parent ) : base( parent )
	{
		MinimumSize = 200;

		MenuButton = new Button.Clear( "", this );
		MenuButton.Icon = "settings";
		MenuButton.Clicked = OpenMenu;

		Chart = new RealtimeChart( this );
		Chart.BackgroundColor = Theme.SurfaceBackground.Darken( 0.7f );
		Chart.Visible = true;
		Chart.Lower();
	}

	private void OpenMenu()
	{
		var menu = new ContextMenu( this );

		menu.AddOption( new Option { Text = "Every Frame", Checkable = true, Checked = RefreshSpeed == 0, Triggered = () => RefreshSpeed = 0 } );
		menu.AddOption( new Option { Text = "Fast", Checkable = true, Checked = RefreshSpeed == 0.1f, Triggered = () => RefreshSpeed = 0.1f } );
		menu.AddOption( new Option { Text = "Medium", Checkable = true, Checked = RefreshSpeed == 0.25f, Triggered = () => RefreshSpeed = 0.25f } );
		menu.AddOption( new Option { Text = "Slow", Checkable = true, Checked = RefreshSpeed == 0.5f, Triggered = () => RefreshSpeed = 0.5f } );

		menu.Position = MenuButton.ScreenRect.BottomLeft;
		menu.Visible = true;
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		if ( Chart.IsValid() )
		{
			Chart.Position = 0;
			Chart.Size = Size;
		}

		MenuButton.Size = 32;
		MenuButton.Position = 8;
	}

	RealTimeSince timeSinceUpdate;
	int framesSinceUpdate;

	[EditorEvent.Frame]
	public void Frame()
	{
		framesSinceUpdate++;

		if ( timeSinceUpdate < RefreshSpeed )
			return;

		if ( !Chart.IsValid() )
			return;

		// TODO - add a toolbar with settings for all this shit
		// especially ScrollSize and the update speed above

		timeSinceUpdate = 0;

		Chart.ScrollSize = (int)(RefreshSpeed * 20) + 1;
		Chart.MinMax = new Vector2( 16, 0 );
		Chart.Stacked = true;
		Chart.ChartType = "bar";
		Chart.GridLineMajor = 4;
		Chart.GridLineMinor = 0;

		foreach ( var entry in PerformanceStats.Timings.GetMain() )
		{
			var renderColor = entry.Color;
			Chart.SetData( entry.Name, "image", renderColor, entry.GetMetric( framesSinceUpdate ) );
		}

		Chart.Draw();

		framesSinceUpdate = 0;
	}
}
