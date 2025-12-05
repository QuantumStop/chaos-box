using Sandbox.Mounting;

namespace Editor;

public class MountsAssetLocations : AssetLocations, IMountEvents
{
	public MountsAssetLocations( MountsAssetBrowser parent ) : base( parent )
	{

	}

	protected override void BuildLocations()
	{
		Clear();

		foreach ( var entry in Directory.GetAll().OrderBy( x => x.Title ) )
		{
			var mount = Directory.Get( entry.Ident );

			AddItem( new MountSourceNode( new MountLocation( mount ), mount.IsMounted ) );
		}
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		var menu = new ContextMenu();

		foreach ( var entry in Directory.GetAll().OrderBy( x => x.Title ) )
		{
			var m = menu.AddOption( entry.Title );
			m.Checkable = true;
			m.Checked = entry.Mounted;
			m.Enabled = entry.Available;
			m.Toggled = ( b ) =>
			{
				// Do we need to show progress here, maybe?
				EditorUtility.Mounting.SetMounted( entry.Ident, b );
			};
		}

		menu.AddSeparator();

		menu.AddOption( "Refresh All", "refresh", async () =>
		{
			foreach ( var e in Directory.GetAll() )
			{
				await EditorUtility.Mounting.Refresh( e.Ident );
			}

			Rebuild();

		} );

		menu.OpenAtCursor();
		e.Accepted = true;
	}

	void IMountEvents.OnMountEnabled( BaseGameMount source )
	{
		BuildLocations();
	}

	void IMountEvents.OnMountDisabled( BaseGameMount source )
	{
		BuildLocations();
	}
}
