using Sandbox.Diagnostics;

namespace Editor;

partial class VRStats
{
	class Sheet : Widget
	{
		/// <summary>
		/// Represents something we can display
		/// </summary>
		record struct MeasurementPair( string Name, string Value )
		{
			public bool IsSeparator;

			public static MeasurementPair Separator => new( "", "" ) { IsSeparator = true };
		}

		public Pixmap Pixmap { get; protected set; }
		public Color BackgroundColor { get; set; } = Color.Black;

		public Sheet( Widget parent ) : base( parent )
		{
		}

		private List<MeasurementPair> Pairs = new();

		void UpdatePairs()
		{
			Pairs = new()
			{
				new( "Per-eye Resolution", $"{PerformanceStats.VR.Resolution.x}x{PerformanceStats.VR.Resolution.y}"),
				new( "Interpupillary Distance", $"{PerformanceStats.VR.InterpupillaryDistance} mm" ),
			};
		}

		public virtual void Clear()
		{
			Pixmap.Clear( BackgroundColor );
		}

		void UpdatePixmap()
		{
			if ( Pixmap != null && Pixmap.Width == Width && Pixmap.Height == Height )
				return;

			Pixmap = new Pixmap( (int)Width, (int)Height );
			Clear();
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			UpdatePixmap();

			Paint.SetBrush( Pixmap );
			Paint.ClearPen();
			Paint.DrawRect( new Rect( 0, 0, Width, Height ) );
		}

		public void Draw()
		{
			UpdatePairs();
			UpdatePixmap();

			Clear();

			using ( Paint.ToPixmap( Pixmap ) )
			{
				Paint.Antialiasing = false;

				var margin = 8;
				var valueWidth = 150;

				var cursor = Vector2.Zero + margin;
				var sizeWithMargin = Size - (margin * 2);

				foreach ( var item in Pairs )
				{
					Paint.ClearBrush();
					Paint.ClearPen();

					Paint.SetPen( Theme.TextControl );

					var rect = new Rect( cursor, new Vector2( sizeWithMargin.x, 16 ) );

					var nameRect = rect;
					var valueRect = rect + new Vector2( sizeWithMargin.x - valueWidth, 0 );

					valueRect.Size = new Vector2( valueWidth, rect.Height );

					Paint.DrawText( nameRect, item.Name, TextFlag.LeftCenter );
					Paint.DrawText( valueRect, item.Value, TextFlag.RightCenter );

					cursor += new Vector2( 0, rect.Height );

					if ( item.IsSeparator )
					{
						var from = cursor.WithY( rect.Center.y );
						var to = sizeWithMargin.WithY( from.y );

						Paint.SetPen( Theme.Border );
						Paint.DrawLine( from, to );
					}
				}

			}
			Update();
		}
	}
}
