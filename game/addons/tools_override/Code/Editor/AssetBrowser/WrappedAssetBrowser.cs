namespace Editor;

public class WrappedAssetBrowser : Widget
{
	public LocalAssetBrowser Local { get; private set; }
	public MountsAssetBrowser Mounts { get; private set; }
	public CloudAssetBrowser Cloud { get; private set; }

	private VerticalTabWidget Tabs;

	public WrappedAssetBrowser( Widget parent, List<AssetType> assetTypeFilters ) : base( parent )
	{
		MinimumSize = new( 100, 100 );

		Layout = Layout.Row();

		Local = new LocalAssetBrowser( this, assetTypeFilters );
		Mounts = new MountsAssetBrowser( this, assetTypeFilters );
		Cloud = new CloudAssetBrowser( this, assetTypeFilters );

		Tabs = Layout.Add( new VerticalTabWidget( this ) );
		Tabs.AddPage( "Local", "folder", Local, "Local" );
		Tabs.AddPage( "Cloud", "cloud", Cloud, "Cloud" );
		Tabs.AddPage( "Mounts", "museum", Mounts, "Mounts" );
	}
}
