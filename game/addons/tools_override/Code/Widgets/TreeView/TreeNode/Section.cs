using Sandbox.UI;

namespace Editor;

public partial class TreeNode
{
	public class Section : TreeNode
	{
		public string Icon { get; set; }
		public string Title { get; set; }
		public Color IconColor { get; set; } = Color.White.WithAlpha( 0.5f );
		public bool ShowCounts { get; set; }
		public string CountOverride { get; set; }
		public override bool ExpanderFills => true;
		public override bool ExpanderHidden => true;

		public Section( string icon, string name, bool showCounts = false )
		{
			Icon = icon;
			Title = name;
			Height = 40;
			ShowCounts = showCounts;
		}

		public override void OnPaint( VirtualWidget item )
		{



			var open = item.IsOpen;

			var backgroundRect = item.Rect;
			backgroundRect.Left -= item.Indent;
			backgroundRect.Bottom -= 1;

			if ( item.Selected )
			{
				Paint.SetPen( Theme.Primary.WithAlpha( 0.9f ) );
				Paint.SetBrush( Theme.Primary.WithAlpha( 0.1f ) );
				Paint.DrawRect( backgroundRect.Shrink( 2 ) );
			}
			else if ( item.Hovered )
			{
				Paint.SetPen( Theme.Primary.WithAlpha( 0.9f ) );
				Paint.SetBrush( Color.Black.WithAlpha( open ? 0.7f : 0.6f ) );
				Paint.DrawRect( backgroundRect.Shrink( 1 ) );
			}
			else
			{
				Paint.ClearPen();
				Paint.SetBrush( Color.Black.WithAlpha( open ? 0.7f : 0.6f ) );
				Paint.DrawRect( backgroundRect );
			}

			var rect = backgroundRect.Shrink( 8 );

			if ( !string.IsNullOrWhiteSpace( Icon ) )
			{
				Paint.SetPen( IconColor.WithAlphaMultiplied( open ? 1.0f : 0.4f ) );
				var i = Paint.DrawIcon( rect, Icon, 22, TextFlag.LeftCenter );
				rect.Left = i.Right + 8;
			}


			Paint.SetPen( Theme.Text.WithAlpha( open ? 1.0f : 0.6f ) );
			Paint.SetHeadingFont( 12, 450 );

			var textRect = Paint.DrawText( rect, Title.ToUpper(), TextFlag.LeftCenter );

			if ( ShowCounts )
			{
				//Paint.SetDefaultFont( 7 );
				var r = item.Rect;
				r.Right -= 16;

				string count = CountOverride ?? $"{(children?.Count() ?? 0):n0}";

				Paint.SetHeadingFont( 8, 450 );
				Paint.SetBrush( Theme.SurfaceBackground.WithAlpha( open ? 0.2f : 0.1f ) );
				Paint.ClearPen();
				Paint.DrawTextBox( r, count, Theme.Border.WithAlphaMultiplied( open ? 1.0f : 0.5f ), new Margin( 5, 0 ), 3.0f, TextFlag.RightCenter );
			}
		}
	}
}
