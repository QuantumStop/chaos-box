namespace Editor;

public class TabWidget : Widget
{
	Dictionary<string, Widget> pages = new();

	Widget _currentPage;

	public Widget CurrentPage
	{
		get => _currentPage;
		set
		{
			if ( _currentPage == value )
				return;

			SetPage( value );

			if ( _currentPage.IsValid() )
				_currentPage.Visible = false;

			_currentPage = value;

			if ( _currentPage.IsValid() )
				_currentPage.Visible = true;

			Update();
		}
	}

	SegmentedControl TabBar;


	public TabWidget( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();
		Layout.Margin = 8.0f;
		TabBar = Layout.Add( new SegmentedControl( this ) );
		Layout.AddSpacingCell( 8.0f );
	}

	public void AddStretchCell()
	{
		TabBar.Layout.AddStretchCell();
	}

	public bool ShowText
	{
		get => TabBar.ShowText;
		set => TabBar.ShowText = value;
	}

	public void SetPage( Widget page )
	{
		var entry = pages.FirstOrDefault( x => x.Value == page );
		if ( !entry.Value.IsValid() ) return;
		if ( _currentPage == entry.Value ) return;

		foreach ( var p in pages )
		{
			p.Value.Visible = false;
		}

		entry.Value.Visible = true;
		_currentPage = entry.Value;
		Update();

		Save();
	}

	public void AddPage( string name, string icon = null, Widget page = null, int? count = null )
	{
		page ??= new Widget( null );

		page.Visible = false;
		pages[name] = page;

		TabBar.AddOption( name, icon, count );
		Layout.Add( page );

		TabBar.OnSelectedChanged += ( value ) =>
		{
			if ( value == name )
			{
				SetPage( page );
			}
		};

		if ( pages.Count == 1 )
		{
			CurrentPage = page;
		}
	}

	string _cookie;

	public string StateCookie
	{
		get => _cookie;

		set
		{
			if ( _cookie == value ) return;
			_cookie = value;
			Restore();
		}
	}

	private void Save()
	{
		if ( string.IsNullOrEmpty( StateCookie ) ) return;

		var pageName = "";
		var page = pages.FirstOrDefault( x => x.Value == CurrentPage );
		pageName = page.Key ?? "";

		EditorCookie.Set( $"tabwidget.{StateCookie}", pageName );
	}

	private void Restore()
	{
		if ( string.IsNullOrEmpty( StateCookie ) ) return;

		var pageName = EditorCookie.Get<string>( $"tabwidget.{StateCookie}", null );
		if ( string.IsNullOrWhiteSpace( pageName ) ) return;

		var page = pages.FirstOrDefault( x => x.Key == pageName );
		if ( page.Key == null ) return;

		SetPage( page.Value );

		TabBar.Selected = page.Key;
	}
}
