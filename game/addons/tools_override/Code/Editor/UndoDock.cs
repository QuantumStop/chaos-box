namespace Editor;


[Dock( "Editor", "Undo", "log" )]
public class UndoDock : Widget
{
	public UndoDock( Widget parent ) : base( parent )
	{
		Layout = Layout.Row();
		Layout.Margin = 4;
		Layout.Spacing = 4;

		Rebuild();
	}

	void Rebuild()
	{
		Layout.Clear( true );

		if ( SceneEditorSession.Active is not null )
		{
			Layout.Add( new UndoList( SceneEditorSession.Active.UndoSystem ) );
		}
	}

	[EditorEvent.Frame]
	public void CheckForChanges()
	{
		if ( !Visible )
			return;

		if ( SetContentHash( HashCode.Combine( SceneEditorSession.Active ), 0.1f ) )
		{
			Rebuild();
		}
	}
}

class UndoList : ListView
{
	Sandbox.Helpers.UndoSystem undoSystem;

	public UndoList( Sandbox.Helpers.UndoSystem undoSystem )
	{
		this.undoSystem = undoSystem;
	}

	[EditorEvent.Frame]
	public void CheckForChanges()
	{
		ItemSize = new Vector2( -1, 17 );

		if ( SetContentHash( HashCode.Combine( undoSystem.Back.Count ), 0.1f ) )
		{
			SetItems( undoSystem.Back );
		}
	}

	protected override void PaintItem( VirtualWidget item )
	{
		if ( item.Object is not Sandbox.Helpers.UndoSystem.Entry entry )
			return;

		Paint.DrawText( item.Rect, $"{entry.Name}", TextFlag.LeftCenter );
	}
}

