using Sandbox.Diagnostics;
using System;
namespace Editor;

// TODO rename this
// TODO namespace this in a common charts thing?
internal class RealtimeChart : Widget
{

	public int ScrollSize { get; set; } = 10;
	public Vector2 MinMax { get; set; } = new Vector2( 0, 100 );
	public Color BackgroundColor { get; set; } = Color.Black;
	public string ChartType { get; set; } = "line";

	public bool Stacked { get; set; } = false;
	public float GridLineMajor { get; set; } = 10.0f;
	public float GridLineMinor { get; set; } = 2.0f;


	public Pixmap Pixmap { get; protected set; }

	internal class DataEntry
	{
		public string Name { get; set; }
		public string Icon { get; set; }
		public Color Color { get; set; }
		public PerformanceStats.PeriodMetric Value { get; set; }
		public PerformanceStats.PeriodMetric PreviousValue { get; set; }
		public ChartLabel Label { get; set; }
		public bool Disabled { get; set; }
	}

	Dictionary<string, DataEntry> Data { get; } = new Dictionary<string, DataEntry>();

	public RealtimeChart( Widget parent ) : base( parent )
	{

	}

	void UpdatePixmap()
	{
		if ( Pixmap != null && Pixmap.Width == Width && Pixmap.Height == Height )
			return;

		Pixmap = new Pixmap( (int)Width, (int)Height );
		Clear();
	}

	public virtual void Clear()
	{
		Pixmap.Clear( BackgroundColor );
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		var y = 16;
		foreach ( var e in Data )
		{
			e.Value.Label.Size = new Vector2( 200, 20 );
			e.Value.Label.Position = new Vector2( Width - 200 - 16, y );

			y += 22;
		}
	}


	protected override void OnPaint()
	{
		base.OnPaint();

		UpdatePixmap();

		Paint.SetBrush( Pixmap );
		Paint.ClearPen();
		Paint.DrawRect( new Rect( 0, 0, Width, Height ) );
	}

	float stackValue;

	public void Draw()
	{
		UpdatePixmap();

		Pixmap.Scroll( ScrollSize, 0 );

		using ( Paint.ToPixmap( Pixmap ) )
		{

			Paint.Antialiasing = false;

			//
			// Column background
			//
			{
				Paint.ClearPen();
				Paint.SetBrush( BackgroundColor );
				Paint.DrawRect( new Rect( 0, 0, ScrollSize, Pixmap.Height ) );
			}

			//
			// Column content
			//
			{
				stackValue = 0;

				foreach ( var e in Data )
				{
					if ( e.Value.Disabled ) continue;

					DrawEntry( e.Value );
				}
			}

			//
			// Column foreground
			//
			{
				stackValue = 0;
				var mn = MathF.Min( MinMax.x, MinMax.y );
				var mx = MathF.Max( MinMax.x, MinMax.y );

				// todo - these grid lines only work if min starts on the grid line !

				if ( GridLineMinor > 0 )
				{
					Paint.ClearPen();
					Paint.SetBrush( Color.White.WithAlpha( 0.2f ) );
					for ( float x = mn; x <= mx; x += GridLineMinor )
					{
						Paint.DrawRect( new Rect( 0, ValueToPixel( x ), ScrollSize, 1 ) );
					}
				}

				if ( GridLineMajor > 0 )
				{
					Paint.ClearPen();
					Paint.SetBrush( Color.White.WithAlpha( 0.3f ) );
					for ( float x = mn; x <= mx; x += GridLineMajor )
					{
						Paint.DrawRect( new Rect( 0, ValueToPixel( x ), ScrollSize, 1 ) );
					}
				}
			}

		}

		//
		// Tell qt to redraw this control
		//
		Update();
	}

	public float ValueToPixel( float value, bool addStack = false )
	{
		if ( addStack && Stacked )
			value += stackValue;

		value = value.LerpInverse( MinMax.x, MinMax.y, false );
		value *= Height;

		return value;
	}

	private void DrawEntry( in DataEntry e )
	{
		var prevStack = ValueToPixel( stackValue, false );
		var pixelValue = ValueToPixel( e.Value.Max, true );
		var pixelValuePrev = ValueToPixel( e.PreviousValue.Max, true );

		if ( ChartType == "line" )
		{
			Paint.ClearBrush();
			Paint.SetPen( e.Color, 1.0f );
			Paint.DrawLine( new Vector2( ScrollSize, pixelValuePrev ), new Vector2( 0, pixelValue ) );
		}

		if ( ChartType == "bar" )
		{
			Paint.SetBrush( e.Color );
			Paint.SetPen( e.Color.Darken( 0.1f ) );

			Paint.DrawRect( new Rect( 0, pixelValue.FloorToInt(), ScrollSize, prevStack.FloorToInt() - pixelValue.FloorToInt() ) );
		}

		stackValue += e.Value.Max;
		e.PreviousValue = e.Value;
	}

	internal void SetData( string title, string icon, Color color, in PerformanceStats.PeriodMetric metric, bool disabled = false )
	{
		if ( !Data.TryGetValue( title, out var data ) )
		{
			data = new DataEntry
			{
				Name = title,
				Icon = icon,
				Color = color,
				Value = metric,
				PreviousValue = metric,
				Disabled = disabled
			};

			data.Label = new ChartLabel( this, data );

			Data[title] = data;
		}

		data.Icon = icon;
		data.Color = color;
		data.PreviousValue = data.Value;
		data.Value = metric;
	}
}

/// TODO give this a real name
/// todo namespace it
/// todo Make it generic so any chart type can use it
class ChartLabel : Widget
{
	RealtimeChart.DataEntry Data;

	public ChartLabel( Widget parent, RealtimeChart.DataEntry data ) : base( parent )
	{
		Data = data;
		Visible = true;
		Cursor = CursorShape.Finger;
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		var color = Data.Color;
		if ( Data.Disabled ) color = color.Desaturate( 0.6f ).WithAlpha( 0.6f );
		if ( IsUnderMouse ) color = color.Lighten( 0.3f ).Saturate( 0.4f );

		Paint.ClearPen();
		Paint.SetBrush( color.Darken( 0.9f ).Desaturate( 0.4f ).WithAlpha( 0.8f ) );
		Paint.DrawRect( LocalRect, 3.0f );

		Paint.SetPen( color );
		Paint.DrawIcon( LocalRect.Shrink( 7, 2 ), Data.Disabled ? "do_not_disturb" : Data.Icon, 16, TextFlag.LeftCenter );
		Paint.SetDefaultFont();
		Paint.DrawText( LocalRect.Shrink( 30, 2 ), Data.Name, TextFlag.LeftCenter );
		var r = Paint.DrawText( LocalRect.Shrink( 10, 2 ), $"{Data.Value.Max:0.00}", TextFlag.Right | TextFlag.Center );

		Paint.SetPen( color.WithAlpha( 0.5f ) );
		Paint.DrawText( LocalRect.Shrink( 10 + r.Width + 4, 2 ), $"{Data.Value.Avg:0.00}", TextFlag.Right | TextFlag.Center );
	}

	protected override void OnMouseEnter()
	{
		base.OnMouseEnter();
		Update();
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();
		Update();
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		Data.Disabled = !Data.Disabled;
		Update();
	}
}
