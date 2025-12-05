namespace Editor;

[Icon( "access_time" )]
public partial class DateTimeEditorWidget : Widget
{
	public Action<DateTime> ValueChanged { get; set; }

	Layout HeaderLayout;
	GridLayout CalendarLayout;
	Label HeaderLabel;
	LineEdit HeaderEntry;

	IconButton buttonLeft;
	IconButton buttonRight;

	DateTime _value;
	DateTime _viewing;

	/// <summary>
	/// The current color value
	/// </summary>
	public DateTime Value
	{
		get => _value;
		set
		{
			_value = value;
			_viewing = value;
			Rebuild();
			ValueChanged?.Invoke( _value );
		}
	}

	int Hour
	{
		get => _value.Hour;
		set
		{
			var hour = Math.Clamp( value, 0, 23 );
			_value = new DateTime( _value.Year, _value.Month, _value.Day, hour, _value.Minute, _value.Second );
			ValueChanged?.Invoke( _value );
		}
	}

	int Minute
	{
		get => _value.Minute;
		set
		{
			var minute = Math.Clamp( value, 0, 59 );
			_value = new DateTime( _value.Year, _value.Month, _value.Day, _value.Hour, minute, _value.Second );
			ValueChanged?.Invoke( _value );
		}
	}

	int Second
	{
		get => _value.Second;
		set
		{
			var second = Math.Clamp( value, 0, 59 );
			_value = new DateTime( _value.Year, _value.Month, _value.Day, _value.Hour, _value.Minute, second );
			ValueChanged?.Invoke( _value );
		}
	}


	public DateTimeEditorWidget( Widget parent = null ) : base( parent )
	{
		_value = DateTime.Now;

		Layout = Layout.Column();
		FocusMode = FocusMode.Click;

		HeaderLayout = Layout.Row();
		HeaderLayout.Margin = 8;
		HeaderLayout.Spacing = 2;
		buttonLeft = HeaderLayout.Add( new IconButton( "arrow_back", ButtonLeft, this ) );

		HeaderLabel = HeaderLayout.Add( new Label( this ) );
		HeaderLabel.HorizontalSizeMode = SizeMode.CanGrow;
		HeaderLabel.OnPaintOverride = () =>
		{
			if ( Paint.HasMouseOver || Paint.HasPressed )
			{
				Paint.ClearPen();
				Paint.SetBrush( (Theme.TextControl).WithAlpha( Paint.HasPressed ? 0.05f : 0.1f ) );
				Paint.DrawRect( HeaderLabel.LocalRect, 2 );
			}

			Paint.SetPen( Theme.TextControl );
			Paint.DrawText( HeaderLabel.LocalRect.Shrink( 4 ), HeaderLabel.Text, TextFlag.Center );
			return true;
		};
		HeaderLabel.MouseClick = OpenHeaderMenu;
		HeaderLabel.MouseRightClick = OpenHeaderMenu;

		HeaderEntry = HeaderLayout.Add( new LineEdit( this ) );
		HeaderEntry.HorizontalSizeMode = SizeMode.CanGrow;
		HeaderEntry.FixedHeight = Theme.RowHeight;
		HeaderEntry.Visible = false;

		buttonRight = HeaderLayout.Add( new IconButton( "arrow_forward", ButtonRight, this ) );
		Layout.Add( HeaderLayout );

		CalendarLayout = Layout.Grid();
		CalendarLayout.Spacing = 2;
		Layout.Add( CalendarLayout );

		Layout.AddStretchCell( 1 );

		var timeLayout = Layout.Add( Layout.Row() );
		timeLayout.Margin = new Sandbox.UI.Margin( 0, 3, 0, 0 );
		timeLayout.Spacing = 2;
		timeLayout.Add( CreateTimeWidget( "Hour", "h", Theme.Yellow ) );
		timeLayout.Add( CreateTimeWidget( "Minute", "m", Theme.Green ) );
		timeLayout.Add( CreateTimeWidget( "Second", "s", Theme.Blue ) );

		Rebuild();
	}

	void Rebuild()
	{
		CalendarLayout.Clear( true );

		// Days of the week
		var firstSunday = new DateTime( 1970, 1, 4 );
		for ( int i = 0; i < 7; i++ )
		{
			var day = firstSunday.AddDays( i );
			var monthAbbrev = day.DayOfWeek.ToString().Substring( 0, 2 );
			var headerLabel = CalendarLayout.AddCell( i, 0, new Label( monthAbbrev, this ) );
			headerLabel.Margin = 8;
			headerLabel.Alignment = TextFlag.Center;
		}

		// Days of the month
		var currentDay = new DateTime( _viewing.Year, _viewing.Month, 1 );
		int currentHeight = 1;
		bool hasPlaced = false;
		while ( currentDay.Month == _viewing.Month )
		{
			if ( currentDay.DayOfWeek == DayOfWeek.Sunday && hasPlaced )
				currentHeight++;

			var dayIndex = (int)currentDay.DayOfWeek;
			var btn = CalendarLayout.AddCell( dayIndex, currentHeight, new Button( currentDay.Day.ToString(), this ) );
			var today = new DateTime( currentDay.Year, currentDay.Month, currentDay.Day );
			btn.Clicked = () =>
			{
				Value = new DateTime( today.Year, today.Month, today.Day, _value.Hour, _value.Minute, _value.Second );
			};
			if ( currentDay.Year == _value.Year && currentDay.Month == _value.Month && currentDay.Day == _value.Day )
			{
				btn.Tint = Theme.Blue;
			}

			currentDay = currentDay.AddDays( 1 );
			hasPlaced = true;
		}

		buttonLeft.Enabled = !(_viewing.Month == 1 && _viewing.Year == 1);
		UpdateHeader();
	}

	void UpdateHeader()
	{
		HeaderLabel.Text = $"{_viewing.ToString( "MMMM yyyy" )}";
		ResetHeader();
	}

	void ButtonLeft()
	{
		if ( _viewing.Month == 1 && _viewing.Year == 1970 ) return;
		_viewing = _viewing.AddMonths( -1 );

		Rebuild();
	}

	void ButtonRight()
	{
		_viewing = _viewing.AddMonths( 1 );

		Rebuild();
	}

	void ResetHeader()
	{
		HeaderEntry.Text = _value.ToString();
		HeaderEntry.Visible = false;
		HeaderLabel.Visible = true;
	}

	void OpenHeaderMenu()
	{
		var menu = new Menu( this );
		List<DateTime> days = new();
		for ( int i = 0; i < 12; i++ )
		{
			var date = new DateTime( _viewing.Year, i + 1, 1 );
			days.Add( date );
		}
		menu.AddOptions( days, x => "Set Month/" + x.ToString( "MM MMMM" ), x =>
		{
			_viewing = x;
			Rebuild();
		}, false, true, "edit_calendar" );

		menu.AddSeparator();

		menu.AddOption( "Edit Value", "edit", () =>
		{
			HeaderLabel.Visible = false;
			HeaderEntry.Visible = true;
			HeaderEntry.Text = _value.ToString();
			HeaderEntry.Focus();
			HeaderEntry.SelectAll();
			HeaderEntry.EditingFinished += () =>
			{
				if ( DateTime.TryParse( HeaderEntry.Text, out var result ) )
				{
					Value = result;
				}
				HeaderEntry.Visible = false;
				HeaderLabel.Visible = true;
			};
		} );
		menu.AddOption( "Set Current Date", "calendar_today", () =>
		{
			var now = DateTime.Now;
			Value = new DateTime( now.Year, now.Month, now.Day, _value.Hour, _value.Minute, _value.Second );
		} );
		menu.AddOption( "Set Current Time", "access_time", () =>
		{
			var now = DateTime.Now;
			Value = new DateTime( _value.Year, _value.Month, _value.Day, now.Hour, now.Minute, now.Second );
		} );
		menu.AddOption( "Set Current Date + Time", "today", () =>
		{
			var now = DateTime.Now;
			Value = new DateTime( now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second );
		} );

		menu.OpenAtCursor();
	}

	IntegerControlWidget CreateTimeWidget( string propertyName, string label, Color labelColor )
	{
		var target = this.GetSerialized();
		var hourControl = ControlWidget.Create( target.GetProperty( propertyName ) );
		if ( hourControl is not IntegerControlWidget hourFloat ) return null;

		hourFloat.Label = label;
		hourFloat.HighlightColor = labelColor;
		hourControl.MaximumWidth = 80;

		return hourFloat;
	}

	protected override void OnPaint()
	{
		Paint.SetBrushAndPen( Theme.ControlBackground.WithAlpha( 0.5f ) );
		Paint.DrawRect( HeaderLayout.OuterRect );
	}

	[WidgetGallery]
	[Title( "Date Editor" )]
	[Icon( "access_time" )]
	internal static Widget WidgetGallery()
	{
		var canvas = new Widget( null );
		canvas.Layout = Layout.Column();

		var ged = new DateTimeEditorWidget( canvas );
		ged.Value = new DateTime( 2006, 11, 29 );

		canvas.Layout.Add( ged );
		canvas.Layout.AddStretchCell();
		return canvas;
	}

	/// <summary>
	/// Open a gradient editor popup
	/// </summary>
	public static void OpenPopup( Widget parent, DateTime input, Action<DateTime> onChange )
	{
		var popup = new PopupWidget( parent );
		popup.Visible = false;
		popup.FixedWidth = 250;
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 8;

		var editor = popup.Layout.Add( new DateTimeEditorWidget( popup ), 1 );
		editor.Value = input;
		editor.ValueChanged = onChange;

		popup.OpenAtCursor();
	}

}
