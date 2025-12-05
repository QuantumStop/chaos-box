namespace Editor.Preferences;

internal class PageNotifications : Widget
{
	public PageNotifications( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();
		Layout.Margin = 32;

		Layout.Add( new Label.Subtitle( "Notifications" ) );

		var sheet = new ControlSheet();

		sheet.AddProperty( () => EditorPreferences.NotificationPopups );
		sheet.AddProperty( () => EditorPreferences.NotificationSounds );
		sheet.AddProperty( () => EditorPreferences.ErrorNotificationTimeout );
		sheet.AddProperty( () => EditorPreferences.CompileNotifications );
		sheet.AddProperty( () => EditorPreferences.UndoSounds );

		Layout.Add( sheet );
		Layout.AddStretchCell();
	}
}
