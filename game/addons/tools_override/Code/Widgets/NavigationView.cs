using System;
namespace Editor;

public class NavigationView : Widget
{
	HashSet<Option> pages = new();

	Widget _currentPage;

	public Widget CurrentPage
	{
		get => _currentPage;
		set
		{
			if ( _currentPage == value )
				return;

			if ( _currentPage.IsValid() )
				_currentPage.Visible = false;

			_currentPage = value;

			if ( _currentPage.IsValid() )
			{
				PageContents.Add( _currentPage );
				_currentPage.Visible = true;
			}

			// select the right option
			foreach ( var e in pages )
			{
				if ( e.Page == _currentPage )
					CurrentOption = e;
			}

			Update();
		}
	}

	Option currentButton;

	public Option CurrentOption
	{
		get => currentButton;
		set
		{
			if ( currentButton == value )
				return;

			if ( currentButton.IsValid() )
				currentButton.IsSelected = false;

			currentButton = value;

			if ( currentButton.IsValid() )
			{
				currentButton.IsSelected = true;
				CurrentPage = CurrentOption.GetOrCreatePage();
			}
			else
			{
				CurrentPage = null;
			}

			Update();
		}
	}

	/// <summary>
	/// Top of the menu on the left
	/// </summary>
	public Layout MenuTop { get; private set; }

	/// <summary>
	/// Bottom of the menu
	/// </summary>
	public Layout MenuBottom { get; private set; }

	/// <summary>
	/// The menu
	/// </summary>
	public Layout MenuContents { get; private set; }

	/// <summary>
	/// The main content panel
	/// </summary>
	public Layout PageContents { get; private set; }

	Widget Menu;
	Widget Page;

	public void ClearPages()
	{
		pages.Clear();

		MenuContents.Clear( true );
		PageContents.Clear( true );
	}

	public NavigationView( Widget parent = null ) : base( parent )
	{
		Layout = Layout.Row();

		Menu = new Widget( this );
		Menu.Layout = Layout.Column();

		Page = new Widget( this );
		Page.Layout = Layout.Column();

		PageContents = Page.Layout.AddColumn( 1 );
		PageContents.Margin = 0;

		Menu.MaximumWidth = 300;
		Menu.MinimumWidth = 200;

		Layout.Add( Menu );
		Layout.Add( Page, 1 );

		Menu.Layout.Margin = 8;
		MenuTop = Menu.Layout.AddColumn();
		MenuContents = Menu.Layout.AddColumn();
		MenuContents.Spacing = 0;
		Menu.Layout.AddStretchCell();
		MenuBottom = Menu.Layout.AddColumn();
	}

	public Option AddPage( string name, string icon, Widget page = null )
	{
		if ( page.IsValid() )
		{
			page.Parent = this;
			page.Visible = false;
		}

		return AddPage( new Option( name, icon, this ) { Page = page } );
	}

	public Option AddPage( DisplayInfo displayInfo, Widget page = null )
	{
		if ( page.IsValid() )
		{
			page.Parent = this;
			page.Visible = false;
		}

		return AddPage( new Option( displayInfo.Name, displayInfo.Icon, this ) { Page = page } );
	}

	public Layout AddSectionHeader( string name )
	{
		var labelLayout = MenuContents.AddRow();
		labelLayout.Margin = new Sandbox.UI.Margin( 16, 16, 0, 8 );

		var text = new Label( name );
		labelLayout.Add( text );

		return labelLayout;
	}


	public Option AddPage( Option tab )
	{
		tab.NavigationView = this;

		bool isFirst = pages.Count == 0;

		pages.Add( tab );

		if ( tab.Page.IsValid() )
		{
			tab.Page.Visible = false;
			Layout.Add( tab.Page );
		}

		tab.MouseLeftPress += () =>
		{
			CurrentOption = tab;
		};

		MenuContents.Add( tab );

		if ( isFirst )
		{
			CurrentOption = tab;
		}

		return tab;
	}

	float selectY = -100;

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.ClearPen();
		Paint.SetBrush( Theme.SurfaceBackground );
		Paint.DrawRect( LocalRect.Shrink( 0 ), 4 );

		var sideMenurect = new Rect( 0, 0, Menu.Width, Height );

		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( sideMenurect, 0 );

		if ( CurrentOption.IsValid() )
		{
			if ( selectY == -100 )
			{
				selectY = CurrentOption.Position.y;
			}
			else
			{
				selectY = MathX.Lerp( selectY, CurrentOption.Position.y, 80.0f * RealTime.Delta );

				// redraw again next frame if we're not there yet
				if ( !selectY.AlmostEqual( CurrentOption.Position.y ) ) Update();
			}

			var activeRect = new Rect( sideMenurect.Left + 12, selectY, sideMenurect.Width - 12 * 2, CurrentOption.Height );

			Paint.ClearPen();
			Paint.SetBrush( Theme.Primary );
			Paint.DrawRect( activeRect, 4 );
		}
	}

	internal void SwitchPage<T>() where T : Widget
	{
		var p = pages.Where( x => x.Page is T ).FirstOrDefault();
		if ( !p.IsValid() ) return;

		CurrentPage = p.Page;
	}

	public class Option : Widget
	{
		public Widget Page { get; set; }
		public Action OpenContextMenu { get; set; }
		public Func<Widget> CreatePage { get; set; }

		public string Title { get; set; }
		public string Icon { get; set; }

		internal NavigationView NavigationView;

		public Option( string title, string icon, NavigationView parent = null ) : base( parent )
		{
			NavigationView = parent;
			Title = title;
			Icon = icon;

			MinimumSize = 24;
			Cursor = CursorShape.Finger;
		}

		public bool IsSelected { get; set; }

		protected override void OnPaint()
		{
			base.OnPaint();

			var fg = Color.White.WithAlpha( 0.5f );

			if ( IsSelected )
			{
				fg = Color.White;
			}

			Paint.ClearPen();
			Paint.SetBrush( Theme.SurfaceBackground.WithAlpha( 0.0f ) );

			if ( Paint.HasMouseOver )
			{
				fg = Color.White.WithAlpha( 0.8f );
			}

			Paint.TextAntialiasing = true;
			Paint.Antialiasing = true;


			Paint.DrawRect( LocalRect.Shrink( 0 ) );

			var inner = LocalRect.Shrink( 8, 0, 0, 0 );
			var iconRect = inner;
			iconRect.Width = iconRect.Height;

			Paint.SetPen( fg );
			Paint.DrawIcon( iconRect, Icon, 14, TextFlag.Center );

			inner.Left += iconRect.Width + 4;

			Paint.SetPen( fg.WithAlphaMultiplied( 0.8f ) );
			Paint.SetHeadingFont( 8, 440 );

			Paint.DrawText( inner, Title, TextFlag.LeftCenter );
		}

		protected override void OnMousePress( MouseEvent e )
		{
			base.OnMousePress( e );

			if ( e.RightMouseButton )
			{
				OpenContextMenu?.Invoke();
				e.Accepted = true;
				return;
			}

			if ( e.LeftMouseButton )
			{
				NavigationView.CurrentOption = this;
				e.Accepted = true;
				return;
			}

		}

		public Widget GetOrCreatePage()
		{
			if ( !Page.IsValid() && CreatePage != null )
			{
				Page = CreatePage();
			}

			return Page;
		}


		[Description( @"<p>A widget with a sidebar down the left, allowing you to switch between pages.</p>
<p>Pages can be added by callback when selected, so they're created on demand instead of everything being created at once.</p>" )]
		[WidgetGallery]
		[Title( "Navigation View" )]
		[Icon( "view_sidebar" )]
		internal static Widget WidgetGallery()
		{
			var view = new NavigationView( null );

			view.AddPage( "Page One", "campaign", new Button( "Page One Contents" ) );
			view.AddPage( "Page Two", "people", new Button.Primary( "Page Two Contents" ) );

			view.AddSectionHeader( "Section Header" );
			view.AddPage( "Another Option", "auto_mode", new Button( "More Contents" ) );

			return view;
		}
	}

}
