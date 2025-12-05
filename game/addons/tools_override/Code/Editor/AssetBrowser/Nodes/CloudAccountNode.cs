namespace Editor.AssetBrowsing.Nodes;

partial class CloudAccountNode : TreeNode.Header
{
	public CloudAccountNode() : base( "person", "My Organisations" )
	{
		EditorEvent.Register( this );
	}

	~CloudAccountNode()
	{
		EditorEvent.Unregister( this );
	}

	public override bool HasChildren => EditorUtility.Account.Memberships.Any();
	public override string GetTooltip() => "The organisations you're a member of";

	protected override void BuildChildren()
	{
		Clear();

		if ( !HasChildren ) return;

		foreach ( var x in EditorUtility.Account.Memberships.OrderBy( x => x.Title ) )
		{
			AddItem( new CloudAssetNode( x.Thumb, x.Title, $"org:{x.Ident}" ) );
		}
	}

	public override bool OnContextMenu()
	{
		var menu = new ContextMenu( null );
		menu.AddOption( "Refresh", "refresh", () => { Dirty(); } );

		if ( !menu.HasOptions )
			return true;

		menu.OpenAtCursor();

		return true;
	}
}
