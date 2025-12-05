
namespace Editor;

public class MountsAssetBrowser : AssetBrowser
{
	protected override string CookieKey => "MountsAssetBrowser";

	public MountsAssetBrowser( Widget parent, List<AssetType> assetTypeFilters ) : base( parent, assetTypeFilters )
	{
	}

	protected override void CreateLocations()
	{
		AssetLocations = new MountsAssetLocations( this );
		AssetLocations.Browser = this;
		AssetLocations.OnFolderSelected = ( directoryInfo ) => NavigateTo( directoryInfo );
	}
}
