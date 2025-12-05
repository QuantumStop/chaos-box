namespace Editor;

class RadioSelectWidget<T> : Widget
{
	public Action<T> OnValueChanged { get; set; }

	private List<RadioSelectOption<T>> Options { get; } = new();

	public RadioSelectWidget( Widget parent = null ) : base( parent )
	{
		Layout = Layout.Column();
		HorizontalSizeMode = SizeMode.Expand | SizeMode.CanGrow;
	}

	/// <summary>
	/// Add a new option with a specific value.
	/// </summary>
	public RadioSelectOption<T> AddOption( string name, string icon, T value )
	{
		var option = new RadioSelectOption<T>( name, icon, value );

		option.OnSelected = () =>
		{
			// Deselect every other option.
			foreach ( var o in Options.Where( o => o != option ) )
			{
				o.IsSelected = false;
			}

			OnValueChanged?.Invoke( option.Value );
		};

		Options.Add( option );

		Layout.Add( option );
		Layout.AddSpacingCell( 2f );

		return option;
	}

	/// <summary>
	/// Set the current value of this radio select if a valid option exists for it.
	/// </summary>
	/// <param name="value"></param>
	public void SetValue( T value )
	{
		foreach ( var option in Options.Where( option => option.Value.Equals( value ) ) )
		{
			option.IsSelected = true;
			break;
		}
	}
}
