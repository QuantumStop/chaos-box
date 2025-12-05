namespace Editor;

public partial class TagPicker
{
	public class TagOption : Widget
	{
		public bool IsSelected { get; set; }
		public bool IsExcluded { get; set; }
		public Option Option;

		int FoundCount;

		public TagOption( Widget parent, Option option ) : base( parent )
		{
			Option = option;
			Cursor = CursorShape.Finger;
			FoundCount = option.Count?.Invoke() ?? 0;
			VerticalSizeMode = SizeMode.CanGrow;
			HorizontalSizeMode = SizeMode.CanGrow;
		}

		protected override Vector2 SizeHint() => new Vector2( 200, 24 );

		protected override void OnPaint()
		{
			base.OnPaint();

			float alpha = 0.5f;
			if ( IsSelected || IsExcluded ) alpha = 1f;

			if ( Paint.HasMouseOver ) alpha += 0.2f;

			var r = LocalRect;

			if ( Paint.HasPressed )
			{
				r.Position += 2;
			}

			if ( Paint.HasMouseOver )
			{
				Paint.ClearPen();
				Paint.SetBrush( Theme.Primary.WithAlpha( 0.5f ) );
				Paint.DrawRect( r, Theme.ControlRadius );
			}

			if ( IsExcluded )
			{
				Paint.ClearBrush();
				Paint.SetPen( Theme.Red.WithAlpha( 0.5f ), 4, PenStyle.Dash );
				Paint.DrawRect( r, Theme.ControlRadius );
			}
			r = r.Shrink( 4, 2 );

			if ( Option.Color.a > 0 )
			{
				var colorRect = r;
				colorRect.Width = 2;
				Paint.ClearPen();
				Paint.SetBrush( Option.Color.WithAlpha( alpha ) );
				Paint.DrawRect( colorRect );

				Paint.ClearBrush();

				r.Left += 4;
			}

			if ( Option.PixmapIcon != null )
			{
				var size = Option.PixmapIcon.Size.ComponentMin( 16 );

				Paint.Draw( r.Align( size, TextFlag.LeftCenter ), Option.PixmapIcon, IsSelected || IsExcluded ? 1.0f : 0.3f );
				r.Left += 16 + 4;
			}
			else if ( Option.Icon != null )
			{
				var size = new Vector2( 16 );

				Paint.SetPen( Option.Color.WithAlpha( IsSelected || IsExcluded ? 1.0f : 0.3f ) );
				Paint.DrawIcon( r.Align( size, TextFlag.LeftCenter ), Option.Icon, 16.0f );
				r.Left += 16 + 4;
			}

			if ( FoundCount > 0 )
			{
				Paint.SetDefaultFont( 6 );
				Paint.SetPen( Theme.TextControl.WithAlpha( alpha * 0.6f ) );
				var drawn_rect = Paint.DrawText( r, $"{FoundCount:n0}", TextFlag.RightCenter );
				r.Right = drawn_rect.Left - 8.0f;
			}

			Paint.SetDefaultFont( 8 );
			Paint.SetPen( Theme.TextControl.WithAlpha( alpha ) );
			Paint.DrawText( r, Option.Title, TextFlag.LeftCenter );
		}

		protected override void OnMousePress( MouseEvent e )
		{
			base.OnMousePress( e );
			e.Accepted = true;
		}
	}
}
