using Sandbox.Diagnostics;
using static Editor.Inspectors.AssetInspector;

namespace Editor.Inspectors;

[CanEdit( "asset:prefab" )]
public class PrefabFileInspector : Widget, IAssetInspector
{
	Asset asset;

	public PrefabFileInspector( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();
	}

	PrefabFile resource;

	public void SetAsset( Asset asset )
	{
		ArgumentNullException.ThrowIfNull( asset, nameof( asset ) );

		this.asset = asset;

		resource = this.asset.LoadResource<PrefabFile>();
		Assert.NotNull( resource, $"We couldn't load a resource from {asset.RelativePath} - this indicates that the path is monted in the editor, but not the game." );

		Rebuild();
	}

	void Rebuild()
	{
		SerializedObject so = TypeLibrary.GetSerializedObject( resource );
		so.OnPropertyChanged += x =>
		{
			resource.StateHasChanged();
		};

		Layout.Clear( true );

		var cs = new ControlSheet();

		cs.AddRow( so.GetProperty( nameof( PrefabFile.ShowInMenu ) ) );
		cs.AddRow( so.GetProperty( nameof( PrefabFile.MenuPath ) ) );
		cs.AddRow( so.GetProperty( nameof( PrefabFile.MenuIcon ) ) );
		cs.AddRow( so.GetProperty( nameof( PrefabFile.DontBreakAsTemplate ) ) );

		Layout.Add( cs );
	}
}
