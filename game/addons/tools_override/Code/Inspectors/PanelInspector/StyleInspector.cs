
using Sandbox.Internal;
using System.Reflection;

namespace Editor.Inspectors;

partial class StyleInspector : ScrollArea
{
	public IPanel Panel { get; set; }

	public StyleInspector( Widget parent, IPanel target ) : base( parent )
	{
		Canvas = new Widget( this );

		Panel = target;
		Canvas.Layout = Layout.Column();

		Rebuild();
	}

	public void Rebuild()
	{
		Canvas.Layout.Clear( true );
		SetStyles( $"background-color: {Theme.ControlBackground.Hex};" );

		var seen = new HashSet<string>( System.StringComparer.OrdinalIgnoreCase );

		foreach ( var block in Panel.ActiveStyleBlocks.Reverse() )
		{
			var section = Canvas.Layout.AddColumn();
			section.Margin = 4;
			section.Spacing = 4;

			//
			// Selectors
			//
			{
				var row = Layout.Grid();
				var label = row.AddCell( 0, 0, new Label( string.Join( ", ", block.SelectorStrings.Select( x => $"<span style=\"color: {Theme.Yellow.Hex}\">{x}</span>" ) ) ) );
				label.WordWrap = true;

				var link = row.AddCell( 1, 0, new Label( $"<a href=\"{block.AbsolutePath}\" style=\"color: {Color.White.WithAlpha( 0.6f ).Hex}\">{block.FileName.Trim( '/' )}:{block.FileLine}</a>" ) { Color = Theme.Green.WithAlpha( 0.5f ) }, alignment: TextFlag.RightCenter );
				link.MouseClick = () => CodeEditor.OpenFile( block.AbsolutePath, block.FileLine );

				section.AddLayout( row );
			}

			section.Add( new Label( "{" ) );

			foreach ( var entry in block.GetRawValues() )
			{
				var row = new StyleRow( this, block, entry );

				section.Add( row );
				seen.Add( entry.Name );
			}

			section.Add( new Label( "}" ) );

			Canvas.Layout.AddSeparator();
		}

		Canvas.Layout.AddStretchCell();
	}
}
