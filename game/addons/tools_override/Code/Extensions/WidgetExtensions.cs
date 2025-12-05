namespace Editor;

public static class WidgetExtensions
{
	/// <summary>
	/// Place a widget in a certain position relative to the screen position of another
	/// </summary>
	public static void Place( this Widget self, Widget parent, WidgetAnchor placement )
	{
		placement.Apply( self, parent );
	}

	/// <summary>
	/// Place a widget in a certain position relative to the screen position of another
	/// </summary>
	public static void OpenNextTo( this Menu self, Widget parent, WidgetAnchor placement )
	{
		self.OpenAt( 0, false );
		placement.Apply( self, parent );
	}

	/// <summary>
	/// Show a popup dialog warning about unsaved changes. The user can either:
	/// <list type="bullet">
	/// <item>
	/// <term>Save</term>
	/// <description>Call <paramref name="onSave"/>, then destroy the <paramref name="widget"/>.</description>
	/// </item>
	/// <item>
	/// <term>Don't Save</term>
	/// <description>Destroy the <paramref name="widget"/> without saving.</description>
	/// </item>
	/// <item>
	/// <term>Cancel</term>
	/// <description>Don't do anything, leaving the <paramref name="widget"/> open.</description>
	/// </item>
	/// </list>
	/// </summary>
	public static void ShowUnsavedChangesDialog( this Widget widget, string assetName, string assetType, Action onSave )
	{
		var popup = new PopupDialogWidget( "💾" )
		{
			FixedWidth = 462,
			WindowTitle = $"Closing {assetName}",
			MessageLabel = { Text = $"Do you want to save the changes you made to this {assetType}?" },
			ButtonLayout = { Spacing = 4 }
		};

		popup.ButtonLayout.AddStretchCell();
		popup.ButtonLayout.Add( new Button( "Save" )
		{
			Clicked = () =>
			{
				onSave();

				popup.Destroy();
				widget.Destroy();
			}
		} );

		popup.ButtonLayout.Add( new Button( "Don't Save" )
		{
			Clicked = () =>
			{
				popup.Destroy();
				widget.Destroy();
			}
		} );

		popup.ButtonLayout.Add( new Button( "Cancel" )
		{
			Clicked = () =>
			{
				popup.Destroy();
			}
		} );

		popup.SetModal( true, true );
		popup.Hide();
		popup.Show();
	}
}
