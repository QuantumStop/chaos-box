
namespace Editor;

public class LocalAssetBrowser : AssetBrowser
{
	protected override string CookieKey => "LocalAssetBrowser";

	public LocalAssetBrowser( Widget parent, List<AssetType> assetTypeFilters ) : base( parent, assetTypeFilters )
	{
	}

	public override void AddPin( string folderPath )
	{
		if ( AssetLocations is LocalAssetLocations local )
			local.AddPinnedFolder( folderPath );
	}

	protected override void CreateLocations()
	{
		AssetLocations = new LocalAssetLocations( this );
		AssetLocations.Browser = this;
		AssetLocations.OnFolderSelected = ( directoryInfo ) => NavigateTo( directoryInfo );
	}

	protected override void SetInitialLocation()
	{
		CurrentLocation = new DiskLocation( Sandbox.Project.Current.GetAssetsPath() );
	}
}
