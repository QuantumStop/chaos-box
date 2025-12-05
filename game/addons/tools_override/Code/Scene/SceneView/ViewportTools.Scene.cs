namespace Editor;

partial class ViewportTools
{
	private void BuildToolbarScene( Layout layout )
	{
		var so = EditorScene.GizmoSettings.GetSerialized();

		{
			var group = layout.Add( AddGroup() );

			AddToggleButton(
				group.Layout,
				"Global Space",
				() => "public",
				() => EditorScene.GizmoSettings.GlobalSpace,
				( v ) => EditorScene.GizmoSettings.GlobalSpace = v
			);

			AddToggleButton(
				group.Layout,
				"Draw Gizmos",
				() => "touch_app",
				() => EditorScene.GizmoSettings.GizmosEnabled,
				( v ) => EditorScene.GizmoSettings.GizmosEnabled = v
			);
		}

		{
			var group = layout.Add( AddGroup() );

			AddToggleButton(
				group.Layout,
				"Angle Snap",
				() => "rotate_90_degrees_cw",
				() => EditorScene.GizmoSettings.SnapToAngles,
				( v ) => EditorScene.GizmoSettings.SnapToAngles = v
			);

			{
				var angleStep = new AngleStepWidget( so.GetProperty( nameof( EditorScene.GizmoSettings.AngleSpacing ) ) );
				angleStep.ToolTip = "Angle Step";
				angleStep.FixedWidth = 65;
				group.Layout.Add( angleStep );
			}
		}

		{
			var group = layout.Add( AddGroup() );

			AddToggleButton(
				group.Layout,
				"Grid Snap",
				() => "grid_on",
				() => EditorScene.GizmoSettings.SnapToGrid,
				( v ) => EditorScene.GizmoSettings.SnapToGrid = v
			);

			{
				var snapStep = new SnapStepWidget( so.GetProperty( nameof( EditorScene.GizmoSettings.GridSpacing ) ) )
				{
					Min = 0.125f,
					Max = 128.0f
				};
				snapStep.ToolTip = "Grid Step";
				snapStep.FixedWidth = 65;
				group.Layout.Add( snapStep );
			}
		}
	}
}

file class AngleStepWidget : SnapStepWidget
{
	private float[] values =
	{
		0.25f,
		0.5f,
		1f,
		5f,
		15f,
		30f,
		45f,
		90f,
		180f
	};

	public AngleStepWidget( SerializedProperty property ) : base( property, "º" )
	{

	}

	public override void Decrease()
	{
		var value = SerializedProperty.GetValue<float>();

		var index = Array.IndexOf( values, values.OrderBy( a => MathF.Abs( value - a ) ).First() );
		if ( index > 0 ) index--;

		LineEdit.Blur();
		SerializedProperty.SetValue( values[index] );
	}

	public override void Increase()
	{
		var value = SerializedProperty.GetValue<float>();

		var index = Array.IndexOf( values, values.OrderBy( a => MathF.Abs( value - a ) ).First() );
		if ( index != values.Count() - 1 ) index++;

		LineEdit.Blur();
		SerializedProperty.SetValue( values[index] );
	}
}

/// <summary>
/// Pretty Spinbox-like widget for headerbar value picking
/// </summary>
class SnapStepWidget : ControlWidget
{
	protected LineEdit LineEdit;

	public float Min { get; set; } = 0.25f;
	public float Max { get; set; } = 128f;

	public SnapStepWidget( SerializedProperty property, string suffix = null ) : base( property )
	{
		Layout = Layout.Row();
		LineEdit = new LineEdit( this );
		LineEdit.TextEdited += ( text ) => property.SetValue<object>( float.TryParse( text, out float v ) ? v : text );
		LineEdit.MinimumSize = Theme.RowHeight;
		LineEdit.MaximumSize = new Vector2( 4096, Size.y );
		LineEdit.ReadOnly = ReadOnly;
		LineEdit.SetStyles( "background-color: transparent; vertical-align: middle; text-align: left;" );
		Layout.Add( LineEdit );

		if ( suffix != null )
		{
			var label = new Label( this );
			label.Text = suffix;
			label.SetStyles( "background-color: transparent; vertical-align: middle; text-align: right;" );
			Layout.Add( label );
		}

		var buttons = Layout.AddColumn();

		var bIncrease = new IconButton( "keyboard_arrow_up", Increase );
		bIncrease.Background = Color.Transparent;
		bIncrease.FixedHeight = Theme.ControlHeight / 2;
		bIncrease.FixedWidth = 20;
		buttons.Add( bIncrease );

		var bDecrease = new IconButton( "keyboard_arrow_down", Decrease );
		bDecrease.Background = Color.Transparent;
		bDecrease.FixedHeight = Theme.ControlHeight / 2;
		bDecrease.FixedWidth = 20;
		buttons.Add( bDecrease );

		LineEdit.Text = property.GetValue<float>().ToString();
	}

	protected override void OnValueChanged()
	{
		base.OnValueChanged();

		if ( LineEdit.IsFocused )
			return;

		LineEdit.Text = SerializedProperty.GetValue<float>().ToString();

		// we put the curor at the start of the line so that
		// it keeps the front of the string in focus, since that
		// is most likely the important part
		LineEdit.CursorPosition = 0;
	}

	public virtual void Decrease()
	{
		var value = SerializedProperty.GetValue<float>();
		if ( value <= Min )
			return;

		LineEdit.Blur();
		SerializedProperty.SetValue( value / 2.0f );
	}

	public virtual void Increase()
	{
		var value = SerializedProperty.GetValue<float>();
		if ( value >= Max )
			return;

		LineEdit.Blur();
		SerializedProperty.SetValue( value * 2 );
	}
}
