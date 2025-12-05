namespace Editor;

[Dock( "Editor", "VR Stats", "view_in_ar" )]
public partial class VRStats : Widget
{
	Sheet StatSheet;
	public string Group;

	public float RefreshSpeed = 0.25f;

	public VRStats( Widget parent ) : base( parent )
	{
		MinimumSize = 200;

		SwitchGroup( "default" );
	}

	public void SwitchGroup( string group )
	{
		if ( Group == group ) return;

		Group = group;

		StatSheet?.Destroy();

		StatSheet = new Sheet( this );
		StatSheet.BackgroundColor = Theme.SurfaceBackground.Darken( 0.7f );
		StatSheet.Visible = true;
		StatSheet.Lower();
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		if ( StatSheet.IsValid() )
		{
			StatSheet.Position = 0;
			StatSheet.Size = Size;
		}
	}

	RealTimeSince timeSinceUpdate;

	[EditorEvent.Frame]
	public void Frame()
	{
		if ( timeSinceUpdate < RefreshSpeed )
			return;

		if ( !StatSheet.IsValid() )
			return;

		timeSinceUpdate = 0;

		StatSheet.Draw();
	}
}
