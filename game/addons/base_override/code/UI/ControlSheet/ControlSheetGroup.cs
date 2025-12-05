namespace Sandbox.UI;

/// <summary>
/// A group for ControlSheet, consists of a title and a body containing properties.
/// </summary>
public class ControlSheetGroup : Panel
{
	public Panel Header { get; set; }
	public Panel Body { get; set; }

	public ControlSheetGroup()
	{
		AddClass( "controlgroup" );

		Header = AddChild<Panel>( "header" );
		Body = AddChild<Panel>( "body" );
	}
}
