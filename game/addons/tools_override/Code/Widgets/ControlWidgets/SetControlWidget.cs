namespace Editor;

[CustomEditor( typeof( HashSet<> ) )]
public class SetControlWidget : ControlWidget
{
	public override bool SupportsMultiEdit => false;

	SerializedCollection Collection;
	readonly Layout Content;
	protected override int ValueHash => Collection is null ? base.ValueHash : HashCode.Combine( base.ValueHash, Collection.Count() );

	IconButton addButton;
	int? buildHash;
	object buildValue;

	private bool _showAddButton = true;
	public bool ShowAddButton
	{
		get => _showAddButton;
		set
		{
			_showAddButton = value;
			if ( addButton is not null )
			{
				addButton.Visible = value;
			}
		}
	}

	public SetControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		Layout.Spacing = 2;

		if ( !property.TryGetAsObject( out var so ) || so is not SerializedCollection sc )
			return;

		Collection = sc;
		Collection.OnEntryAdded = Rebuild;
		Collection.OnEntryRemoved = Rebuild;

		buildValue = SerializedProperty?.GetValue<object>();

		Content = Layout.AddColumn();

		Rebuild();
	}

	private void RefreshCollection()
	{
		var value = SerializedProperty?.GetValue<object>();

		if ( buildValue == value )
			return;

		buildValue = value;

		if ( !SerializedProperty.TryGetAsObject( out var so ) || so is not SerializedCollection sc )
			return;

		Collection = sc;
		Collection.OnEntryAdded = Rebuild;
		Collection.OnEntryRemoved = Rebuild;
	}

	protected override void OnValueChanged()
	{
		RefreshCollection();
		Rebuild();
	}

	public void Rebuild()
	{
		if ( Collection is null )
			return;

		var hash = ValueHash;
		if ( buildHash.HasValue && hash == buildHash.Value ) return;
		buildHash = hash;

		using var _ = SuspendUpdates.For( this );

		Content.Clear( true );
		Content.Margin = 0;

		var column = Content.AddColumn();
		//column.Spacing = 2;

		// Add existing items
		foreach ( var entry in Collection )
		{
			var itemRow = column.AddRow();
			itemRow.Margin = new Sandbox.UI.Margin( 0, 2 );

			var valControl = ControlSheetRow.CreateEditor( entry );
			valControl.ReadOnly = ReadOnly;
			valControl.Enabled = Enabled;

			itemRow.Add( valControl, 1 );

			if ( !IsControlDisabled )
			{
				itemRow.Add( new IconButton( "clear", () => RemoveEntry( entry.GetValue<object>() ) )
				{
					Background = Theme.ControlBackground,
					FixedWidth = Theme.RowHeight,
					FixedHeight = Theme.RowHeight
				} );
			}
		}

		// Add button at the bottom - match ListControlWidget exactly
		if ( !IsControlDisabled )
		{
			var buttonRow = column.AddRow();
			buttonRow.Margin = new Sandbox.UI.Margin( 0, 0, 0, 0 );

			addButton = new CollectionAddButton( Collection );
			addButton.MouseClick = AddEntry;

			if ( !ShowAddButton )
				addButton.Visible = ShowAddButton;

			buttonRow.Add( addButton );
			buttonRow.AddStretchCell( 1 );
		}
	}

	void AddEntry()
	{
		if ( Collection == null )
			return;

		// For string types, add empty string instead of null
		if ( Collection.ValueType == typeof( string ) )
		{
			Collection.Add( string.Empty );
		}
		else
		{
			Collection.Add( null );
		}
	}

	void RemoveEntry( object value )
	{
		if ( Collection.RemoveAt( value ) )
		{
			Rebuild();
		}
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;

		Paint.ClearPen();
		Paint.SetBrush( Theme.TextControl.Darken( 0.6f ) );
	}
}
