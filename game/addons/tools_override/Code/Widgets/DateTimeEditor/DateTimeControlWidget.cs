namespace Editor;

[CustomEditor( typeof( DateTime ) )]
public class DateTimeControlWidget : ControlWidget
{
	public DateTimeControlWidget( SerializedProperty property ) : base( property )
	{
		SetSizeMode( SizeMode.Default, SizeMode.Default );

		Layout = Layout.Column();
		Layout.Spacing = 2;
		Cursor = CursorShape.Finger;
	}

	protected override void PaintOver()
	{
		DateTime dateTime = SerializedProperty.GetValue<DateTime>();

		Vector2 iconSize = Theme.RowHeight - 4;
		var iconRect = Rect.FromPoints( 2, iconSize + 2 );
		Paint.SetBrush( Theme.Yellow.WithAlpha( 0.1f ) );
		Paint.DrawRect( iconRect, 2 );
		Paint.SetPen( Theme.Yellow );
		Paint.DrawIcon( iconRect, "access_time", 12 );

		Paint.SetPen( Theme.TextControl );
		Paint.DrawText( LocalRect.Shrink( iconRect.Right + 4, 0, 4, 0 ), dateTime.ToString(), TextFlag.LeftCenter );
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		if ( e.LeftMouseButton )
		{
			DateTime dateTime = SerializedProperty.GetValue<DateTime>();
			DateTimeEditorWidget.OpenPopup( this, dateTime, x => { SerializedProperty.SetValue( x ); Update(); } );
		}
	}
	protected override void OnContextMenu( ContextMenuEvent e )
	{
		base.OnContextMenu( e );

		var menu = new Menu( this );

		List<DateTime> days = new();
		for ( int i = 0; i < 12; i++ )
		{
			DateTime dateTime = SerializedProperty.GetValue<DateTime>();
			var date = new DateTime( dateTime.Year, i + 1, 1 );
			days.Add( date );
		}
		menu.AddOptions( days, x => "Set Month/" + x.ToString( "MM MMMM" ), x =>
		{
			SerializedProperty.SetValue( x );
		}, false, true, "edit_calendar" );

		menu.AddSeparator();

		menu.AddOption( "Set Current Date", "calendar_today", () =>
		{
			DateTime dateTime = SerializedProperty.GetValue<DateTime>();
			var now = DateTime.Now;
			var val = new DateTime( now.Year, now.Month, now.Day, dateTime.Hour, dateTime.Minute, dateTime.Second );
			SerializedProperty.SetValue( val );
		} );
		menu.AddOption( "Set Current Time", "access_time", () =>
		{
			DateTime dateTime = SerializedProperty.GetValue<DateTime>();
			var now = DateTime.Now;
			var val = new DateTime( dateTime.Year, dateTime.Month, dateTime.Day, now.Hour, now.Minute, now.Second );
			SerializedProperty.SetValue( val );
		} );
		menu.AddOption( "Set Current Date + Time", "today", () =>
		{
			var now = DateTime.Now;
			var val = new DateTime( now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second );
			SerializedProperty.SetValue( val );
		} );

		menu.OpenAtCursor();

		e.Accepted = true;
	}
}

[CustomEditor( typeof( DateTimeOffset ) )]
public class DateTimeOffsetControlWidget : ControlWidget
{
	DateTime Date
	{
		get
		{
			var offset = SerializedProperty.GetValue<DateTimeOffset>();
			return offset.DateTime;
		}
		set
		{
			SerializedProperty.SetValue( new DateTimeOffset( value ) );
		}
	}

	public DateTimeOffsetControlWidget( SerializedProperty property ) : base( property )
	{
		SetSizeMode( SizeMode.Default, SizeMode.Default );

		Layout = Layout.Row();
		Layout.Add( ControlWidget.Create( this.GetSerialized().GetProperty( "Date" ) ) );
	}
}
