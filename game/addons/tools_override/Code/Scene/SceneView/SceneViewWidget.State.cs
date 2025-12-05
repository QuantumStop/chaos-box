namespace Editor;

public partial class SceneViewWidget
{
	private class State
	{
		public ViewportLayoutMode Layout { get; set; } = ViewportLayoutMode.One;
		public List<string> SplitterState { get; set; } = new List<string>();
	}

	private string CookieName = "SceneView";

	public void SaveState( bool saveLayout = true )
	{
		if ( saveLayout ) // we don't want to do this when we're in a temporary state (eg. fullscreen)
		{
			ProjectCookie.Set( $"{CookieName}.Layout", new State()
			{
				Layout = ViewportLayout,
				SplitterState = _splitters.ConvertAll( ( LinkableSplitter splitter ) => splitter.SaveState() )
			} );
		}

		foreach ( var viewport in _viewports.Values )
		{
			viewport.SaveState();
		}
	}

	public void RestoreState()
	{
		State state = ProjectCookie.Get( $"{CookieName}.Layout", new State() );
		_viewportLayout = state.Layout;

		RebuildLayout();
		RestoreSplitterState();
	}

	public void RestoreSplitterState()
	{
		State state = ProjectCookie.Get( $"{CookieName}.Layout", new State() );

		for ( int i = 0; i < state.SplitterState.Count && i < _splitters.Count; i++ )
		{
			_splitters[i].RestoreState( state.SplitterState[i] );
		}
	}
}
