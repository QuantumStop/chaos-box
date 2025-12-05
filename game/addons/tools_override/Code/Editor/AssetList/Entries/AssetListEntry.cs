namespace Editor;

public interface IAssetListEntry
{
	//
	// Metadata
	//
	public string Name { get; }

	public string GetStatusText()
	{
		return "";
	}

	//
	// Events
	//
	public bool OnClicked( AssetList list )
	{
		return false;
	}

	public bool OnDoubleClicked( AssetList list )
	{
		return false;
	}

	public bool OnRightClicked( AssetList list )
	{
		return false;
	}

	// 
	// Scroll culling events
	//
	public virtual void OnScrollEnter()
	{

	}

	public virtual void OnScrollExit()
	{

	}

	//
	// List rendering
	//
	public void DrawOverlay( Rect rect )
	{

	}

	public void DrawIcon( Rect rect )
	{

	}

	public void DrawBackground( Rect rect )
	{

	}

	public void DrawText( Rect rect )
	{

	}

	//
	// Actions
	//
	public void Delete()
	{

	}

	public void Rename( string newName )
	{

	}

	public void Duplicate( string newName = null )
	{

	}
}
