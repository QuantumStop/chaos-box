namespace Editor;

public class CurveEditorPopup : Widget
{
	CurveEditor Editor;

	Label labelMultiple;

	public CurveEditorPopup( Widget parent ) : base( parent )
	{
		WindowFlags = WindowFlags.Dialog;
		WindowTitle = "Curve Editor";
		DeleteOnClose = true;

		MinimumSize = new( 894, 100 );

		Editor = new CurveEditor( this );
		Editor.Size = new( 700, 300 );
		Editor.MinimumSize = Editor.Size;

		Layout = Layout.Column();
		Layout.Margin = 16;
		Layout.Spacing = 8;

		labelMultiple = Layout.Add( new Label( this ) );
		labelMultiple.Text = "Multiple Values Selected. Making changes will modify all.";
		labelMultiple.SetStyles( $"color: {Theme.MultipleValues.Hex};" );
		labelMultiple.Visible = false;

		Layout.Add( Editor, 1 );


	}

	public void AddCurve( SerializedProperty serializedProperty, Action onChanged )
	{
		labelMultiple.Visible = serializedProperty.IsMultipleDifferentValues;
		ApplyRangeAttributes( serializedProperty );

		AddCurve( () => serializedProperty.GetValue<Curve>(), v =>
		{
			serializedProperty.SetValue( v );
			onChanged?.Invoke();
			labelMultiple.Visible = serializedProperty.IsMultipleDifferentValues;
		} );
	}

	public void AddCurve( Func<Curve> get, Action<Curve> set )
	{
		Editor.AddCurve( get, set );
	}

	public void AddPresets( SerializedProperty serializedProperty )
	{
		if ( serializedProperty.PropertyType == typeof( Curve ) )
		{
			var Presets = new CurvePresets( this );
			Presets.SetSizeMode( SizeMode.Default, SizeMode.CanShrink );
			Presets.MaximumSize = new( 1000, 148 );
			Presets.OnCurveClicked = c =>
			{
				var existingCurve = serializedProperty.GetValue<Curve>();
				if ( !Editor.CanEditTimeRange )
				{
					c.UpdateTimeRange( existingCurve.TimeRange, false );
				}
				if ( !Editor.CanEditValueRange )
				{
					c.UpdateValueRange( existingCurve.ValueRange, false );
				}
				serializedProperty.SetValue( c );
			};
			Presets.GetCurveToSave = () => serializedProperty.GetValue<Curve>();
			Layout.Add( Presets );
		}
	}

	/// <summary>
	/// Set this editor to be a range editor
	/// </summary>
	public void SetIsRange()
	{
		Editor.SetIsRange();
	}

	private void ApplyRangeAttributes( SerializedProperty property )
	{
		// Only apply attributes to default curve, as it has not been set by the user yet.
		var curve = property.GetValue<Curve>();
		bool editRanges = curve.Equals( default( Curve ) ) || curve.Equals( new Curve() );

		if ( property.TryGetAttribute<TimeRangeAttribute>( out var timeRange ) )
		{
			Editor.SetCanEditTimeRange( timeRange.CanModify );
			if ( editRanges )
			{
				curve.UpdateTimeRange( new Vector2( timeRange.Min, timeRange.Max ), false );
			}
		}

		if ( property.TryGetAttribute<ValueRangeAttribute>( out var valueRange ) )
		{
			Editor.SetCanEditValueRange( valueRange.CanModify );
			if ( editRanges )
			{
				curve.UpdateValueRange( new Vector2( valueRange.Min, valueRange.Max ), false );
			}
		}

		property.SetValue( curve );
	}
}
