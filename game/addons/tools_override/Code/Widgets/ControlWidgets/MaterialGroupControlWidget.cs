namespace Editor;

[CustomEditor( typeof( string ), WithAllAttributes = new[] { typeof( Model.MaterialGroupAttribute ) } )]
public class MaterialGroupControlWidget : ControlWidget
{
	public override bool SupportsMultiEdit => true;

	public MaterialGroupControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		Layout.Spacing = 2;

		AcceptDrops = false;

		Rebuild();
	}

	Model Model
	{
		get
		{
			if ( !SerializedProperty.TryGetAttribute<Model.MaterialGroupAttribute>( out var attr ) )
				return null;

			var modelProperty = SerializedProperty.Parent.GetProperty( attr.ModelParameter );
			if ( modelProperty is null )
				return null;

			if ( modelProperty.PropertyType == typeof( string ) )
			{
				var str = modelProperty.GetValue<string>();
				if ( string.IsNullOrWhiteSpace( str ) ) return default;
				return Model.Load( str );
			}

			return modelProperty.GetValue<Model>( null );
		}
	}

	/// <summary>
	/// Gets common material groups from multiple selected objects
	/// </summary>
	private HashSet<string> GetCommonGroups()
	{
		HashSet<string> commonMaterialGroups = null;

		foreach ( var property in SerializedProperty.MultipleProperties )
		{
			if ( !property.TryGetAttribute<Model.MaterialGroupAttribute>( out var attr ) )
				continue;

			var prop = property.Parent.GetProperty( attr.ModelParameter );
			if ( prop is null )
				continue;

			var model = prop.GetValue<Model>( null );

			//
			// Has no material groups, return empty set
			//
			if ( model is null || model.MaterialGroupCount <= 0 )
				return new();

			var modelMaterialGroups = new HashSet<string>();
			for ( int i = 0; i < model.MaterialGroupCount; i++ )
			{
				modelMaterialGroups.Add( model.GetMaterialGroupName( i ) );
			}

			if ( commonMaterialGroups == null )
			{
				commonMaterialGroups = modelMaterialGroups;
			}
			else
			{
				commonMaterialGroups.IntersectWith( modelMaterialGroups );
			}

			if ( commonMaterialGroups.Count == 0 )
				break;
		}

		return commonMaterialGroups ?? new();
	}

	public void Rebuild()
	{
		Layout.Clear( true );

		HashSet<string> materialGroups = new();

		if ( SerializedProperty.IsMultipleValues )
		{
			materialGroups = GetCommonGroups();
		}
		else
		{
			//
			// Single selection - get material groups directly from the model
			//
			var model = Model;
			materialGroups = new HashSet<string>();

			if ( model.IsValid() && model.MaterialGroupCount > 0 )
			{
				for ( int i = 0; i < model.MaterialGroupCount; i++ )
				{
					materialGroups.Add( model.GetMaterialGroupName( i ) );
				}
			}
		}

		if ( materialGroups.Count == 0 )
		{
			Layout.Add( new Label( "None" ) );
			return;
		}

		var comboBox = new ComboBox( this );
		var current = SerializedProperty.GetValue<string>();

		foreach ( var group in materialGroups.OrderBy( x => x ) )
		{
			comboBox.AddItem( group, onSelected: () =>
			{
				PropertyStartEdit();
				SerializedProperty.SetValue( group );
				PropertyFinishEdit();
			}, selected: string.Equals( current, group, StringComparison.OrdinalIgnoreCase ) && !SerializedProperty.IsMultipleDifferentValues );
		}

		Layout.Add( comboBox );
	}

	protected override void OnValueChanged()
	{
		Rebuild();
	}

	protected override int ValueHash
	{
		get
		{
			var hc = new HashCode();
			hc.Add( base.ValueHash );
			hc.Add( Model );

			if ( Model is not null )
			{
				for ( int i = 0; i < Model.MaterialGroupCount; ++i )
				{
					hc.Add( Model.GetMaterialGroupName( i ) );
				}
			}

			return hc.ToHashCode();
		}
	}
}
