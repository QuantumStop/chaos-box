using Sandbox;
using System;

namespace Editor;

public class ToolbarGroup : Widget
{
	public string Title { get; set; }

	public ToolbarGroup( Widget parent, string title, string icon ) : base( parent )
	{
		MinimumWidth = 10;

		Title = title?.ToUpperInvariant() ?? "";
		Layout = Layout.Row();
		Layout.Margin = new Sandbox.UI.Margin( 0, 0, 0, 0 );

		if ( icon is not null )
		{
			Layout.Add( new ToolbarIcon( icon ) );
		}

		Layout.AddSpacingCell( 4 );
		Layout.Add( new Label( title ) );
		Layout.AddSpacingCell( 4 );

		Build();
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.Antialiasing = true;

		Paint.SetBrushAndPen( Theme.ControlBackground.WithAlpha( 0.5f ) );
		Paint.DrawRect( LocalRect.Shrink( 0, 0, 1, 1 ), 4 );
	}

	public virtual void Build()
	{

	}
}

file class ToolbarIcon : Widget
{
	private string icon;

	public ToolbarIcon( string icon )
	{
		this.icon = icon;
		FixedSize = Theme.ControlHeight;
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		Paint.SetPen( Theme.TextControl.WithAlpha( 0.2f ) );
		Paint.DrawIcon( LocalRect, icon, HeaderBarStyle.IconSize, TextFlag.Center );
	}
}
