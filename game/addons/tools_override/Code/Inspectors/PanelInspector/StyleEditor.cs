
using Sandbox.Internal;
using Sandbox.UI;
using System;

namespace Editor.Inspectors;

public class StyleEditor : Widget
{
	public IPanel Panel { get; set; }

	public StyleEditor( Widget parent, IPanel target ) : base( parent )
	{
		ArgumentNullException.ThrowIfNull( target );

		Panel = target;
		Layout = Layout.Column();

		Think();
	}

	public void Rebuild()
	{
		Layout.Clear( true );

		var toolbar = new ToolBar( this );

		{
			var o = toolbar.AddOption( ":hover", null, () => { ToggleClass( Sandbox.UI.PseudoClass.Hover ); } );
			o.Checkable = true;
			o.Checked = (Panel.PseudoClass & Sandbox.UI.PseudoClass.Hover) != 0;
		}

		{
			var o = toolbar.AddOption( ":active", null, () => { ToggleClass( Sandbox.UI.PseudoClass.Active ); } );
			o.Checkable = true;
			o.Checked = (Panel.PseudoClass & Sandbox.UI.PseudoClass.Active) != 0;
		}

		{
			var o = toolbar.AddOption( ":focus", null, () => { ToggleClass( Sandbox.UI.PseudoClass.Focus ); } );
			o.Checkable = true;
			o.Checked = (Panel.PseudoClass & Sandbox.UI.PseudoClass.Focus) != 0;
		}

		Layout.Add( toolbar );

		var r = Layout.AddColumn( 1 );
		r.Margin = 8;
		r.Add( new StyleInspector( this, Panel ) );
	}

	private void ToggleClass( PseudoClass c )
	{
		if ( (Panel.PseudoClass & c) != 0 )
		{
			Panel.PseudoClass &= ~c;
		}
		else
		{
			Panel.PseudoClass |= c;
		}
	}

	[EditorEvent.Frame]
	void Think()
	{
		if ( !Panel.IsValid() )
			return;

		var h = HashCode.Combine( Panel.PseudoClass );

		foreach ( var b in Panel?.ActiveStyleBlocks )
		{
			h = HashCode.Combine( h, b );
		}

		if ( SetContentHash( HashCode.Combine( h ), 0.1f ) )
		{
			Rebuild();
		}
	}
}
