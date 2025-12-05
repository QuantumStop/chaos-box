namespace Editor;

public interface IBrowser
{
	LocalAssetBrowser.Location CurrentLocation { get; }

	bool ShowRecursiveFiles { get; set; }

	void UpdateAssetList();
	void AddPin( string filter );
	void LoadMore() { }
}
