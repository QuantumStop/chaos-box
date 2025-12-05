using Sandbox.Internal;

namespace Editor.PanelInspector;


public class PanelTreeNode : TreeNode<IPanel>
{
	public PanelTreeNode( IPanel panel )
	{
		Value = panel;
	}

	public override int ValueHash
	{
		get
		{
			if ( !Value.IsValid() ) return 0;

			var childHash = new HashCode();

			foreach ( var child in Value.Children )
			{
				childHash.Add( child.GetHashCode() );
			}

			return HashCode.Combine( Value.Classes, Value.IsVisible, Value.PseudoClass, childHash.ToHashCode() );
		}
	}

	protected override void BuildChildren()
	{
		SetChildren( Value.Children, x => new PanelTreeNode( x ) );
	}

	protected override bool HasDescendant( object obj )
	{
		if ( obj is not IPanel iPanel ) return false;
		return iPanel.IsAncestor( Value );
	}

	public override void OnPaint( VirtualWidget item )
	{
		PaintSelection( item );

		var hoveredInGame = Value.PseudoClass.HasFlag( Sandbox.UI.PseudoClass.Hover );
		var a = Value.IsVisible ? 1.0f : 0.5f;
		var r = item.Rect;

		void Write( Color color, string text, ref Rect r )
		{
			Paint.SetPen( color.WithAlphaMultiplied( a ) );
			var size = Paint.DrawText( r, text, TextFlag.LeftCenter );
			r.Left += size.Width;
		}

		{
			//	Paint.SetPen( Theme.Yellow.WithAlpha( alpha ) );
			//	Paint.DrawIcon( r, info.Icon ?? "window", 18, TextFlag.LeftCenter );
			//r.Left += Theme.RowHeight;
		}

		var brackets = Theme.Yellow.WithAlpha( 0.7f );
		var element = Color.White.WithAlpha( 0.9f );
		var keyword = Color.White.WithAlpha( 0.7f );

		if ( hoveredInGame )
		{
			element = Theme.Green.WithAlpha( 0.9f );
			keyword = Theme.Green.WithAlpha( 0.6f );
		}

		Paint.SetDefaultFont();

		var name = Value.ElementName;

		if ( Value.PseudoClass.Contains( Sandbox.UI.PseudoClass.Before ) ) name = "::before";
		if ( Value.PseudoClass.Contains( Sandbox.UI.PseudoClass.After ) ) name = "::after";

		{
			Write( brackets, $"<", ref r );
			Paint.SetDefaultFont( 8, 500 );
			Write( element, $"{name}", ref r );
			Paint.SetDefaultFont();
		}

		if ( !string.IsNullOrEmpty( Value?.Id ) )
		{
			Write( keyword, $" id=\"", ref r );
			Paint.SetDefaultFont( 8, 500 );
			Write( Theme.Blue, Value?.Id, ref r );
			Paint.SetDefaultFont();
			Write( keyword, $"\"", ref r );
		}

		if ( !string.IsNullOrEmpty( Value?.Classes ) )
		{
			Write( keyword, $" class=\"", ref r );
			Write( Theme.Blue, Value?.Classes, ref r );
			Write( keyword, $"\"", ref r );
		}

		Write( brackets, $">", ref r );

		if ( !string.IsNullOrEmpty( Value.SourceFile ) )
		{
			var localFile = System.IO.Path.GetFileName( Value.SourceFile );
			Write( Theme.Green.WithAlpha( 0.5f ), $" {localFile}:{Value.SourceLine} ", ref r );
		}
	}

	public override bool OnContextMenu()
	{
		var menu = new ContextMenu( null );

		var o = menu.AddOption( "Go To Source", action: () => CodeEditor.OpenFile( Value.SourceFile, Value.SourceLine ) );
		o.Enabled = !string.IsNullOrWhiteSpace( Value.SourceFile );

		menu.OpenAtCursor();
		return true;
	}
}
