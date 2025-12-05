using Sandbox.UI;

namespace Editor;

/// <summary>
/// Offers a popup menu with a number of "tags" when you can then select
/// </summary>
public partial class TagPicker : Widget
{
	public Layout TagsLayout { get; set; }

	/// <summary>
	/// Show a button to select all?
	/// </summary>
	public bool ShowSelectAll { get; set; } = false;

	/// <summary>
	/// Can we select multiple tags?
	/// </summary>
	public bool MultiSelect { get; set; } = true;

	public Action OnValueChanged { get; set; }

	public HashSet<string> ActiveTags { get; set; } = new HashSet<string>();
	public HashSet<string> ExcludedTags { get; set; } = new HashSet<string>();

	private List<TagOption> TagOptions = new();

	public string Icon
	{
		get => ToolButton.Icon;
		set => ToolButton.Icon = value;
	}

	public ToolButton ToolButton { get; protected set; }

	public TagPicker( Widget parent = null ) : base( parent )
	{
		MinimumHeight = Theme.RowHeight;
		MinimumWidth = Theme.RowHeight;

		Layout = Layout.Row();

		TagsLayout = Layout.AddRow();
		TagsLayout.Spacing = 0;
		TagsLayout.Margin = 0;

		Layout.Margin = 0;

		ToolButton = Layout.Add( new ToolButton( "Open Filters", "people", this ) );
		ToolButton.MouseLeftPress = () => OpenPopup();
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, Theme.ControlRadius );

		Paint.SetPen( Theme.TextControl.WithAlpha( Paint.HasMouseOver ? 1.0f : 0.7f ) );

		base.OnPaint();
	}

	PopupWidget popup;

	void OpenPopup()
	{
		popup = new PopupWidget( this );
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;
		popup.MinimumSize = new Vector2( 650, 480 );

		var topColumns = popup.Layout.AddRow( 1 );

		TagOptions.Clear();

		foreach ( var groups in Options.GroupBy( x => x.Column ) )
		{
			var scroller = new ScrollArea( popup );
			scroller.MinimumWidth = 300;

			var container = new Widget( scroller );
			scroller.Canvas = container;
			topColumns.Add( scroller );

			scroller.Canvas.Layout = Layout.Column();
			var layout = scroller.Canvas.Layout;
			scroller.Canvas.ContentMargins = new Margin( 0, 0, 16, 0 );

			layout.Spacing = 2;

			TagGroup currentGroup = null;

			foreach ( var entry in groups.OrderBy( x => x.Group ) )
			{
				if ( currentGroup == null || currentGroup.Title != entry.Group )
				{
					currentGroup = new TagGroup( entry.Group, MultiSelect );
					layout.Add( currentGroup );
				}

				var option = currentGroup.Add( new TagOption( container, entry ) );
				option.ToolTip = entry.Subtitle;

				option.MouseLeftPress = () =>
				{
					option.IsSelected = !option.IsSelected;
					option.IsExcluded = false;

					if ( !MultiSelect )
					{
						foreach ( var otherOption in TagOptions )
						{
							if ( otherOption == option )
								continue;

							otherOption.IsSelected = false;
							Set( otherOption.Option.Tag, false );
							otherOption.Update();
						}
					}

					Set( entry.Tag, option.IsSelected );
					option.Update();
					OnValueChanged?.Invoke();
				};

				option.MouseRightPress = () =>
				{
					option.IsExcluded = !option.IsExcluded;
					option.IsSelected = false;
					SetExcluded( entry.Tag, option.IsExcluded );
					option.Update();
					OnValueChanged?.Invoke();
				};

				option.MouseMiddlePress = () =>
				{
					ClearSelected();
					option.IsSelected = true;
					Set( entry.Tag, option.IsSelected );
					option.Update();
					OnValueChanged?.Invoke();
				};

				option.IsSelected = ActiveTags.Contains( entry.Tag );
				option.IsExcluded = ExcludedTags.Contains( entry.Tag );

				TagOptions.Add( option );
			}

			layout.AddStretchCell();

		}

		var footer = popup.Layout.AddRow();
		footer.Margin = new Margin( 0, 8, 0, 0 );
		footer.Spacing = 4;
		footer.AddStretchCell( 1 );

		if ( ShowSelectAll )
			footer.Add( new Button( "Select All" ) { Clicked = SelectAll } );

		if ( MultiSelect )
			footer.Add( new Button.Danger( "Clear" ) { Clicked = ClearSelected } );

		footer.Add( new Button( "Close" ) { Clicked = () => popup.Close() } );

		popup.Visible = true;
		popup.Position = ScreenRect.BottomRight;
		popup.Position -= new Vector2( popup.MinimumWidth * 0.5f, popup.Height + Height );
		popup.ConstrainToScreen();
	}

	void SelectAll()
	{
		foreach ( var option in Options )
		{
			Set( option.Tag, true );
		}

		if ( popup.IsValid() )
		{
			foreach ( var c in popup.Children.OfType<TagOption>() )
			{
				c.IsSelected = true;
			}

			popup.Update();
		}

		OnValueChanged?.Invoke();
	}

	void ClearSelected()
	{
		ActiveTags.Clear();
		ExcludedTags.Clear();
		Rebuild();

		if ( popup.IsValid() )
		{
			foreach ( var c in popup.Children.OfType<TagOption>() )
			{
				c.IsSelected = false;
			}

			popup.Update();
		}

		OnValueChanged?.Invoke();
	}

	public struct Option
	{
		public string Tag;
		public string Group;
		public string Icon;
		public Pixmap PixmapIcon;
		public string Title;
		public string Subtitle;
		public Color Color;
		public Func<int> Count;
		public int Column;

		public Option( string tag ) : this()
		{
			Tag = tag;
			Title = tag;
		}
	}



	public List<Option> Options { get; } = new List<Option>();

	public void Toggle( string incomingTag )
	{
		Set( incomingTag, !ActiveTags.Contains( incomingTag ) );
	}

	public void ToggleExcluded( string incomingTag )
	{
		SetExcluded( incomingTag, !ExcludedTags.Contains( incomingTag ) );
	}

	public void Set( string incomingTag, bool b )
	{
		if ( !b )
		{
			ActiveTags.Remove( incomingTag );
		}
		else
		{
			SetExcluded( incomingTag, false );
			ActiveTags.Add( incomingTag );
		}

		Rebuild();
	}

	public void SetExcluded( string incomingTag, bool b )
	{
		if ( !b )
		{
			ExcludedTags.Remove( incomingTag );
		}
		else
		{
			Set( incomingTag, false );
			ExcludedTags.Add( incomingTag );
		}

		Rebuild();
	}

	public void Rebuild()
	{
		TagsLayout.Clear( true );

		foreach ( var tag in ActiveTags )
		{
			var o = Options.FirstOrDefault( x => x.Tag == tag );
			if ( o.Tag != tag ) o = new Option( tag );

			var t = TagsLayout.Add( new TagEntry( this, o ) );
			t.MouseLeftPress = () =>
			{
				Toggle( o.Tag );
				OnValueChanged?.Invoke();
			};
		}

		foreach ( var tag in ExcludedTags )
		{
			var o = Options.FirstOrDefault( x => x.Tag == tag );
			if ( o.Tag != tag ) o = new Option( tag );

			var t = TagsLayout.Add( new TagEntry( this, o, true ) );
			t.MouseLeftPress = () =>
			{
				ToggleExcluded( o.Tag );
				OnValueChanged?.Invoke();
			};
		}
	}
}
