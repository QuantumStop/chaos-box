
namespace Editor;

[CustomEditor( typeof( string ), NamedEditor = "Sequence" )]
file class SequenceControlWidget : ControlWidget
{
	public SequenceControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Row();

		Rebuild();
	}

	private void Rebuild()
	{
		Layout.Clear( true );

		var sequence = SerializedProperty.Parent.ParentProperty;
		var v = sequence.GetValue<SkinnedModelRenderer.SequenceAccessor>();
		var comboBox = new ComboBox( this );

		void SelectSequence( string name )
		{
			var propertyPath = $"{nameof( SkinnedModelRenderer.Sequence )}.{nameof( SkinnedModelRenderer.SequenceAccessor.Name )}";
			var targets = sequence.Parent.Targets.OfType<Component>();
			targets.DispatchPreEdited( propertyPath );
			v.Name = name;
			targets.DispatchEdited( propertyPath );
		}

		// Add empty item to select no sequence.
		comboBox.AddItem( string.Empty, onSelected: () => SelectSequence( null ), selected: string.IsNullOrEmpty( v.Name ) );

		foreach ( var name in v.SequenceNames )
		{
			comboBox.AddItem( name, onSelected: () => SelectSequence( name ), selected: string.Equals( v.Name, name, StringComparison.OrdinalIgnoreCase ) );
		}

		Layout.Add( comboBox );
	}
}
