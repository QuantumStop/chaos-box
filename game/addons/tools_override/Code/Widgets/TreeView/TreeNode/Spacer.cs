using Sandbox.UI;

namespace Editor;

public partial class TreeNode
{

	/// <summary>
	/// A node that serves only to create a space
	/// </summary>
	public class Spacer : TreeNode
	{
		public Spacer( float height )
		{
			Height = height;
		}

		public override void OnPaint( VirtualWidget item ) { }
	}
}
