namespace Editor;

[CustomEditor( typeof( Color ) )]
[CustomEditor( typeof( Color32 ) )]
[CustomEditor( typeof( ColorHsv ) )]
public class ColorControlWidget : ControlWidget
{
	private ColorStringWidget stringWidget;
	public override bool SupportsMultiEdit => true;

	ColorSwatchWidget colorSwatchWidget;

	public ColorControlWidget( SerializedProperty property ) : base( property )
	{
		var isColor32 = property.PropertyType == typeof( Color32 );
		var isColorHsv = property.PropertyType == typeof( ColorHsv );

		Layout = Layout.Row();
		Layout.Spacing = 2;

		colorSwatchWidget = Layout.Add( new ColorSwatchWidget( property ) { FixedWidth = Theme.RowHeight, FixedHeight = Theme.RowHeight } );

		stringWidget = new ColorStringWidget( property ) { MinimumWidth = 100, HorizontalSizeMode = SizeMode.Default };
		Layout.Add( stringWidget );

		if ( isColor32 || isColorHsv )
		{
			stringWidget.Visible = false;

			var vectorWidget = new ColorVectorWidget( property ) { HorizontalSizeMode = SizeMode.Expand };
			Layout.Add( vectorWidget );
		}
	}

	protected override void OnPaint()
	{
		// nothing
	}

	public override string ToClipboardString()
	{
		return stringWidget.ToClipboardString();
	}

	public override void FromClipboardString( string clipboard )
	{
		stringWidget.FromClipboardString( clipboard );
	}

	public void OpenColorPopup()
	{
		colorSwatchWidget?.OpenColorPopup();
	}
}


public class ColorSwatchWidget : ControlWidget
{
	public override bool SupportsMultiEdit => true;
	bool IsColor32 = false;
	bool IsColorHsv = false;

	public ColorSwatchWidget( SerializedProperty property ) : base( property )
	{
		Cursor = CursorShape.Finger;
		IsColor32 = property.PropertyType == typeof( Color32 );
		IsColorHsv = property.PropertyType == typeof( ColorHsv );
	}

	protected override void OnPaint()
	{
		var color = Color.Magenta;

		if ( IsColor32 )
			color = SerializedProperty.GetValue( Color32.White ).ToColor();
		else if ( IsColorHsv )
			color = SerializedProperty.GetValue( Color.Magenta.ToHsv() ).ToColor();
		else
			color = SerializedProperty.GetValue( Color.Magenta );

		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		PaintUnder();

		if ( SerializedProperty.IsMultipleDifferentValues )
			return;

		ColorPalette.PaintSwatch( color, LocalRect.Shrink( 3 ), false, radius: 2, disabled: IsControlDisabled );
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		OpenColorPopup();
	}

	public void OpenColorPopup()
	{
		var color = Color.Magenta;

		if ( IsColor32 ) color = SerializedProperty.GetValue( Color32.White ).ToColor();
		else if ( IsColorHsv ) color = SerializedProperty.GetValue( Color.Magenta.ToHsv() ).ToColor();
		else color = SerializedProperty.GetValue( Color.Magenta );

		var picker = ColorPicker.OpenColorPopup( color, c =>
		{
			SerializedProperty.SetValue( c );
		}, ScreenRect.BottomLeft );

		picker.EditingStarted += () => PropertyStartEdit();
		picker.EditingFinished += () => PropertyFinishEdit();

		if ( SerializedProperty.TryGetAttribute<ColorUsageAttribute>( out var colorUsage ) )
		{
			picker.HasAlpha = colorUsage.HasAlpha;
			picker.IsHDR = colorUsage.IsHDR;
			picker.Update();
		}
	}
}

public class ColorStringWidget : StringControlWidget
{
	bool IsColor32 = false;
	bool IsColorHsv = false;

	public ColorStringWidget( SerializedProperty property ) : base( property )
	{
		IsColor32 = property.PropertyType == typeof( Color32 );
		IsColorHsv = property.PropertyType == typeof( ColorHsv );
	}

	protected override void OnValueChanged()
	{
		base.OnValueChanged();

		if ( !LineEdit.IsFocused )
			return;

		if ( SerializedProperty.TryGetAttribute<ColorUsageAttribute>( out var colorUsage ) )
		{
			// Force alpha to 1 if not using alpha
			if ( !colorUsage.HasAlpha )
			{
				var col = SerializedProperty.GetValue<Color>();
				SerializedProperty.SetValue( col.WithAlpha( 1 ) );
			}
		}
	}

	protected override string ValueToString()
	{
		if ( IsColor32 )
		{
			var color32 = SerializedProperty.GetValue( Color.Magenta.ToColor32() );
			return color32.Hex;
		}
		else if ( IsColorHsv )
		{
			var colorHsv = SerializedProperty.GetValue( Color.Magenta.ToHsv() );
			return colorHsv.ToString();
		}

		var color = SerializedProperty.GetValue( Color.Magenta );
		return color.ToString( true, true );
	}

	protected override object StringToValue( string text )
	{
		ArgumentNullException.ThrowIfNull( text );

		var color = Color.White;

		if ( Color.TryParse( text, out var newColor ) )
		{
			color = newColor;
		}

		if ( IsColor32 )
		{
			return color.ToColor32();
		}
		else if ( IsColorHsv )
		{
			return color.ToHsv();
		}

		return color;
	}
}

public class ColorVectorWidget : ControlWidget
{
	public override bool SupportsMultiEdit => true;

	private bool _hasAlpha = true;
	public bool HasAlpha
	{
		get => _hasAlpha;
		set
		{
			_hasAlpha = value;
			if ( !_hasAlpha )
			{
				// Force alpha to 1 if not using alpha
				var col = Target.ParentProperty.GetValue<Color>();
				Target.ParentProperty.SetValue( col.WithAlpha( 1 ) );
			}
			Update();
		}
	}

	bool IsColorHsv = false;

	SerializedObject Target;
	FloatControlWidget FirstControl;

	public ColorVectorWidget( SerializedProperty property ) : base( property )
	{
		property.TryGetAsObject( out Target );
		if ( Target is null )
		{
			Log.Warning( $"Error when trying to get {property} as object" );
			return;
		}

		if ( Target is null )
			return;

		IsColorHsv = property.PropertyType == typeof( ColorHsv );

		if ( property.TryGetAttribute<ColorUsageAttribute>( out var colorUsage ) )
		{
			HasAlpha = colorUsage.HasAlpha;
		}

		Layout = Layout.Row();
		Layout.Spacing = 2;

		if ( IsColorHsv )
		{
			FirstControl = TryAddField( "Hue", Theme.Pink, "H" );
			TryAddField( "Saturation", Theme.Yellow, "S" );
			TryAddField( "Value", Theme.Blue.Lighten( 0.2f ), "V" );
			TryAddField( "Alpha", Color.White, "A" );
		}
		else
		{
			FirstControl = TryAddField( "r", Theme.Red, "R" );
			TryAddField( "g", Theme.Green, "G" );
			TryAddField( "b", Theme.Blue, "B" );
			TryAddField( "a", Color.White, "A" );
		}
	}

	private FloatControlWidget TryAddField( string propertyName, Color color, string text )
	{
		var prop = Target.GetProperty( propertyName );
		if ( prop is null ) return null;
		if ( !HasAlpha && text == "A" ) return null;

		FloatControlWidget control = null;

		if ( IsColorHsv )
			control = Layout.Add( new FloatControlWidget( prop ) { HighlightColor = color, Label = text } );
		else
			control = Layout.Add( new IntegerControlWidget( prop ) { HighlightColor = color, Label = text } );

		control.MinimumWidth = Theme.RowHeight;
		control.HorizontalSizeMode = SizeMode.CanGrow | SizeMode.Expand;

		control.HasRange = true;
		control.RangeClamped = true;

		if ( IsColorHsv )
		{
			if ( text == "H" )
				control.Range = new Vector2( 0, 360 );
			else
				control.Range = new Vector2( 0f, 1f );

			control.RangeStep = 0.01f;
		}
		else
		{
			control.Range = new Vector2( 0, 255 );
			control.RangeStep = 1f;
		}

		return control;
	}

	public override void StartEditing()
	{
		FirstControl?.StartEditing();
	}

	protected override void OnPaint()
	{
		// nothing
	}
}
