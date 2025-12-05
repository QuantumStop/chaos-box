namespace Editor;

public partial class TagPicker
{
	public class TagEntry : Widget
	{
		Option option;
		bool Excluded;

		public TagEntry( Widget parent, Option option, bool excluded = false ) : base( parent )
		{
			this.option = option;

			MinimumWidth = Theme.RowHeight;
			MinimumHeight = Theme.RowHeight;
			Cursor = CursorShape.Finger;
			ToolTip = option.Title;
			Excluded = excluded;
		}

		protected override void OnPaint()
		{
			float alpha = 0.6f;
			var r = LocalRect;

			if ( Enabled && Paint.HasMouseOver )
			{
				alpha = 1;
			}

			if ( Excluded )
			{
				alpha = 1;
				Paint.ClearBrush();
				Paint.SetPen( Theme.Red.WithAlpha( 0.5f ), 4, PenStyle.Dash );
				Paint.DrawRect( r, Theme.ControlRadius );
			}

			if ( option.PixmapIcon != null )
			{
				var iconRect = r.Align( Enabled && Paint.HasMouseOver ? 18 : 16, TextFlag.Center );
				Paint.Draw( iconRect, option.PixmapIcon, alpha );
			}
			else if ( option.Icon != null )
			{
				var iconRect = r.Align( Enabled && Paint.HasMouseOver ? 18 : 16, TextFlag.Center );
				Paint.SetPen( option.Color.WithAlpha( alpha ) );
				Paint.DrawIcon( iconRect, option.Icon, 16.0f );
			}
			else
			{
				Paint.SetPen( option.Color.WithAlpha( alpha ) );
				Paint.DrawText( r, option.Title, TextFlag.LeftCenter );
			}
		}

		protected override void OnMousePress( MouseEvent e )
		{
			base.OnMousePress( e );
			e.Accepted = true;
		}

	}
}
