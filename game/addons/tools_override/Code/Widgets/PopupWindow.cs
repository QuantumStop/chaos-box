using System;
namespace Editor;

/// <summary>
/// A simple popup window to quickly display a message, optionally with custom actions.
/// </summary>
public class PopupWindow : Dialog
{
	Label Label;

	public PopupWindow( string title, string text, string buttonTxt = "OK", IDictionary<string, Action> extraButtons = null )
	{
		Window.MinimumWidth = 500;
		Window.WindowTitle = title;
		Window.SetWindowIcon( "info" );
		Window.SetModal( true, true );

		Layout = Layout.Column();
		Layout.Margin = 20;
		Layout.Spacing = 20;

		Label = new Label( this );
		Label.Text = text;
		Layout.Add( Label );

		var okButton = new Button( this );
		okButton.Text = buttonTxt;
		okButton.MinimumWidth = 64;
		okButton.MouseLeftPress += () => Close();
		okButton.AdjustSize();

		var buttonLayout = Layout.Row( true );
		buttonLayout.Spacing = 5;
		Layout.Add( buttonLayout );
		buttonLayout.Add( okButton );

		if ( extraButtons != null )
		{
			foreach ( var KVs in extraButtons )
			{
				var customBtn = new Button( this );
				customBtn.Text = KVs.Key;
				customBtn.MinimumWidth = 64;
				customBtn.MouseLeftPress += KVs.Value;
				customBtn.MouseLeftPress += () => Close();
				customBtn.AdjustSize();

				buttonLayout.Add( customBtn );
			}
		}

		buttonLayout.AddStretchCell();

		Window.AdjustSize();
		Window.Size += 8; // HACK: Adjust for weird padding between Window and this, visible via the debugger
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.ClearPen();
		Paint.SetBrush( Theme.WidgetBackground );
		Paint.DrawRect( LocalRect, 0.0f );
	}
}
