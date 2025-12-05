using Sandbox.Internal;

namespace Editor.Inspectors;

[CanEdit( typeof( IPanel ) )]
partial class PanelInspector : Widget
{
	public IPanel Panel { get; set; }

	public PanelInspector( Widget parent, IPanel target ) : base( parent )
	{
		if ( !target.IsValid() )
			return;

		Panel = target;
		Layout = Layout.Column();

		var tabs = new TabWidget( this );
		tabs.AddPage( "Styles", "style", new StyleEditor( this, Panel ) );
		tabs.StateCookie = "PanelInspector";

		Layout.Add( tabs, 1 );

		Enabled = true;
	}

}
