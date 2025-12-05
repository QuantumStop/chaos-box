namespace Editor;

/// <summary>
/// Used to store and manipulate a collection of colors.
/// </summary>
public class ColorPalette : Widget
{

	/// <summary>
	/// The selected color in this palette
	/// </summary>
	public Color Value { get; set; }

	/// <summary>
	/// The colors available in this palette
	/// </summary>
	public List<Color> Options
	{
		get => options;
		set
		{
			options = value;

			RefreshList();
		}
	}

	ListView listView;
	List<Color> options;

	public ColorPalette( Widget parent = null ) : base( parent )
	{
		Layout = Layout.Column();

		listView = Layout.Add( new ListView( this ) );
		listView.ItemSize = new Vector2( 18, 18 );
		listView.ItemSpacing = 4;
		listView.Margin = 0;
		listView.ItemPaint = PaintColorEntry;
		listView.ItemSelected = ItemSelected;
		listView.ItemContextMenu = OpenContextMenu;
	}

	private void RefreshList()
	{
		if ( options == null )
		{
			listView.Clear();
			return;
		}

		listView.SetItems( options.Select( x => (object)x ) );

		if ( Parent is ColorPicker picker )
		{
			listView.AddItem( picker );
		}

		DoLayout();
	}

	private void ItemSelected( object obj )
	{
		if ( obj is ColorPicker picker )
		{
			options.Add( picker.Value );

			MakeSignal( "save_colorpalette" );
			RefreshList();
		}

		if ( obj is Color color )
		{
			Value = color;
			SignalValuesChanged();
		}
	}

	private void OpenContextMenu( object obj )
	{
		if ( obj is not Color color ) return;

		var m = new ContextMenu();
		m.AddOption( "Remove", "delete", () =>
		{
			options.Remove( color );

			MakeSignal( "save_colorpalette" );
			RefreshList();
		} );
		m.OpenAtCursor();
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		var rect = listView.CanvasRect;
		var itemSize = listView.ItemSize;
		var itemSpacing = listView.ItemSpacing;
		var itemsPerRow = 1;
		var itemCount = listView.Items.Count();

		if ( itemSize.x > 0 ) itemsPerRow = ((rect.Width + itemSpacing.x) / (itemSize.x + itemSpacing.x)).FloorToInt();
		itemsPerRow = Math.Max( 1, itemsPerRow );

		var rowCount = MathX.CeilToInt( itemCount / (float)itemsPerRow );
		listView.FixedHeight = rowCount * (itemSize.y + itemSpacing.y);
	}

	private void PaintColorEntry( VirtualWidget w )
	{
		Paint.ClearBrush();
		Paint.ClearPen();
		Paint.Antialiasing = true;

		var swatchColor = Theme.Pink;

		if ( w.Object is ColorPicker picker )
		{
			swatchColor = picker.Value;
		}

		if ( w.Object is Color c )
		{
			swatchColor = c;
		}

		PaintSwatch( swatchColor, w.Rect, w.Hovered || w.Pressed );

		if ( w.Object is ColorPicker )
		{
			var penColor = swatchColor.Luminance > .5f ? Color.Black : Color.White;

			Paint.SetPen( penColor );
			Paint.DrawIcon( w.Rect, "add", 13, TextFlag.RightTop );
		}
	}

	public static void PaintSwatch( Color swatchColor, Rect rect, bool highlight, float radius = 4, bool disabled = false )
	{
		Paint.ClearBrush();
		Paint.ClearPen();
		Paint.Antialiasing = true;

		if ( swatchColor.a < 1 && !disabled )
		{
			Paint.SetBrush( "/image/transparent-small.png" );
			Paint.DrawRect( rect, radius );
		}

		float intensity = 1;

		var max = MathF.Max( swatchColor.r, swatchColor.g );
		max = MathF.Max( max, swatchColor.b );

		if ( max > 1 )
		{
			intensity = max;

			var div = 1.0f / max;
			swatchColor.r *= div;
			swatchColor.g *= div;
			swatchColor.b *= div;
		}

		if ( highlight )
		{
			Paint.SetPen( Theme.Text, 2f );
			rect = rect.Shrink( 1 );
		}

		Paint.SetBrush( swatchColor );

		if ( intensity > 1 )
		{
			float offset = (intensity / 1000.0f).Clamp( 0, 1 );
			offset = 20 + offset * rect.Height - 20.0f;
			Paint.SetBrushLinear( rect.BottomLeft - new Vector2( 0.0f, offset ), rect.BottomLeft - new Vector2( 0.0f, offset + 2 ), swatchColor.Lighten( 0.5f ).Desaturate( 0.5f ), swatchColor );
		}

		Paint.DrawRect( rect, radius );

		if ( intensity > 1 )
		{
			var pct = (intensity / 1000.0f).Clamp( 0, 1 ) * 100.0f;
			Paint.SetPen( swatchColor.Desaturate( 0.4f ).Darken( 0.5f ) );
			Paint.SetDefaultFont( 6, 500 );
			Paint.DrawText( rect, $"{pct:n0}%" );
		}
	}

}
