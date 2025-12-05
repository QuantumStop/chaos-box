using Sandbox.UI;

namespace Editor;

public class WarningBox : Widget
{
	Color _bgColor;

	public Color BackgroundColor
	{
		get => _bgColor;
		set
		{
			_bgColor = value;
			Label.Color = _bgColor;
		}

	}
	public Label Label;

	string _icon;
	public string Icon
	{
		get => _icon;
		set
		{
			_icon = value;

			SetProperty( "hasIcon", string.IsNullOrEmpty( _icon ) ? "1" : "0" );
			Layout.Margin = new Margin( 32, 8, 8, 8 );
		}
	}

	public WarningBox( Widget parent = null ) : this( null, parent ) { }

	public WarningBox( string title, Widget parent = null ) : base( parent )
	{
		Layout = Layout.Column();

		Label = new Label( title, this );
		Label.WordWrap = true;
		Label.Alignment = TextFlag.LeftTop;

		Layout.Add( Label );

		Icon = "warning";
		BackgroundColor = Theme.Yellow;
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.ClearPen();

		Paint.SetBrushRadial( LocalRect.TopLeft + new Vector2( 32, Height * 0.5f ), 400, BackgroundColor.Darken( 0.7f ), BackgroundColor.Darken( 0.7f ) );
		Paint.DrawRect( LocalRect, 2 );

		Paint.SetBrushRadial( LocalRect.TopLeft + new Vector2( 32, Height * 0.5f ), 400, BackgroundColor.Darken( 0.5f ), BackgroundColor.Darken( 0.6f ) );
		Paint.DrawRect( LocalRect.Shrink( 1 ), 2 );

		if ( !string.IsNullOrEmpty( _icon ) )
		{
			Paint.SetPen( BackgroundColor );
			Paint.DrawIcon( LocalRect.Shrink( 8 ), _icon, 18, TextFlag.LeftTop );
		}
	}

}

public class InformationBox : WarningBox
{
	public InformationBox( Widget parent = null ) : this( null, parent ) { }

	public InformationBox( string title, Widget parent = null ) : base( title, parent )
	{
		BackgroundColor = Theme.Blue;
		Icon = "info";
	}
}
