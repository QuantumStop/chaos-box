using System.IO;

namespace Editor;

public record PackageLocation : LocalAssetBrowser.Location
{
	Package ParentPackage { get; init; }

	public PackageLocation( Package package ) : base( package.Title, "supervisor_account" )
	{
		ParentPackage = package;
	}

	public override bool CanGoUp() => false;

	public override List<LocalAssetBrowser.Location> GetDirectories() => new List<LocalAssetBrowser.Location>();

	public override IEnumerable<FileInfo> GetFiles()
	{
		return AssetSystem.GetPackageFiles( ParentPackage ).Select( x => new FileInfo( FileSystem.Cloud.GetFullPath( x ) ) );
	}
}
