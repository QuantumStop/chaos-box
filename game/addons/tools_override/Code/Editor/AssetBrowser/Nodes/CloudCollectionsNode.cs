namespace Editor.AssetBrowsing.Nodes;

partial class CloudCollectionsNode : TreeNode.Header
{
	Package.FindResult LastResult;

	public Action OnLoaded { get; set; }

	public CloudCollectionsNode() : base( "grading", "My Collections" )
	{
		EditorEvent.Register( this );

		_ = Update();
	}

	~CloudCollectionsNode()
	{
		EditorEvent.Unregister( this );
	}

	public override string GetTooltip() => "The collections you've favourited";

	async Task Update()
	{
		LastResult = await Package.FindAsync( "sort:favourite type:collection" );

		Dirty();
	}

	protected override void BuildChildren()
	{
		Clear();

		if ( LastResult?.Packages == null )
			return;

		foreach ( var x in LastResult.Packages )
		{
			AddItem( new CloudPackageNode( x ) );
		}

		OnLoaded?.Invoke();
	}

	public override bool OnContextMenu()
	{
		var menu = new ContextMenu( null );
		menu.AddOption( "Refresh", "refresh", () => { _ = Update(); } );
		menu.OpenAtCursor();

		return true;
	}

	[Event( "package.changed.favourite" )]
	public void OnCollectionFavouriteChanged( Package package )
	{
		if ( package.TypeName != "collection" )
			return;

		_ = Update();
	}
}
