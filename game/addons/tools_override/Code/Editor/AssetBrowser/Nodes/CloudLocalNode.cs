namespace Editor.AssetBrowsing.Nodes;

partial class CloudLocalNode : AssetFilterNode, ResourceLibrary.IEventListener
{
	public CloudLocalNode() : base( "attach_file", "Referenced", "@referenced" )
	{
		EditorEvent.Register( this );
		UpdateCount();
	}

	~CloudLocalNode()
	{
		EditorEvent.Unregister( this );
	}

	void ResourceLibrary.IEventListener.OnSave( GameResource resource ) => UpdateCount();
	void ResourceLibrary.IEventListener.OnExternalChanges( GameResource resource ) => UpdateCount();

	void UpdateCount()
	{
		var packages = CloudAsset.GetAssetReferences( true );
		Count = packages.Count;
		Dirty();
	}
}
