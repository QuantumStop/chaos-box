namespace Editor;

[CustomEditor( typeof( RangedFloat ) )]
public class RangedFloatControlWidget : ControlObjectWidget
{
	Layout Inner;

	public override bool SupportsMultiEdit => true;

	public RangedFloatControlWidget( SerializedProperty property ) : base( property, true )
	{
		Layout = Layout.Row();
		Layout.Spacing = 3;

		Inner = Layout.AddRow();

		{
			var prop = SerializedObject.GetProperty( "Range" );
			var e = ControlWidget.Create( prop );
			e.MaximumWidth = 80;

			Layout.Add( e );
		}

		Rebuild();

		SerializedObject.OnPropertyChanged += ( SerializedProperty prop ) =>
		{
			if ( prop.Name == "Range" ) Rebuild();
		};
	}

	protected override void OnPaint()
	{
		// nothing
	}

	public void Rebuild()
	{
		Inner.Clear( true );

		if ( SerializedProperty.IsMultipleValues )
		{
			var first = SerializedProperty.MultipleProperties.FirstOrDefault()?.GetValue<RangedFloat>();
			if ( first is not null && SerializedProperty.MultipleProperties.Any( x => x?.GetValue<RangedFloat>().Range != first?.Range ) )
			{
				CreateMultipleValueWidget();
				return;
			}
		}

		var type = SerializedObject.GetProperty( "Range" ).GetValue<RangedFloat.RangeType>();

		if ( type == RangedFloat.RangeType.Between )
		{
			Inner.Add( ControlWidget.Create( SerializedObject.GetProperty( "RangeValue" ) ) );
		}

		if ( type == RangedFloat.RangeType.Fixed )
		{
			Inner.Add( ControlWidget.Create( SerializedObject.GetProperty( "FixedValue" ) ) );
		}
	}

	void CreateMultipleValueWidget()
	{
		var textEdit = Inner.Add( new Widget( Parent ) );
		textEdit.FixedHeight = Theme.RowHeight;
		textEdit.OnPaintOverride = () =>
		{
			var h = textEdit.Size.y;

			// Control Background
			Paint.SetBrushAndPen( Theme.ControlBackground );
			Paint.DrawRect( textEdit.LocalRect, 2 );

			// Icon Box
			{
				var iconCol = Theme.MultipleValues;
				Paint.ClearPen();
				Paint.SetBrush( iconCol.Darken( 0.8f ).Desaturate( 0.8f ).WithAlphaMultiplied( IsControlDisabled ? 0.5f : 1.0f ) );
				Paint.DrawRect( new Rect( 0, 0, h, h ).Shrink( 2 ), Theme.ControlRadius - 1.0f );
				Paint.SetPen( iconCol.Darken( 0.1f ).Desaturate( 0.2f ).WithAlphaMultiplied( IsControlDisabled ? 0.5f : 1.0f ) );
				Paint.DrawIcon( new Rect( 0, h ), "content_copy", h - 8, TextFlag.Center );
			}

			// Text
			{
				Paint.SetPen( Theme.MultipleValues );
				Paint.DrawText( new Vector2( h + 4, 5 ), "Multiple Values" );
			}

			return true;
		};
	}
}
