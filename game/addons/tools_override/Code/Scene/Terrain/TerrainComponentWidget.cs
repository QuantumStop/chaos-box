namespace Editor.TerrainEditor;

[CustomEditor( typeof( Terrain ) )]
partial class TerrainComponentWidget : ComponentEditorWidget
{
	public TerrainComponentWidget( SerializedObject obj ) : base( obj )
	{
		SetSizeMode( SizeMode.Default, SizeMode.Default );

		Layout = Layout.Column();
		BuildUI();
	}

	void BuildUI()
	{
		Layout.Clear( true );

		// If there is no valid Storage on this Terrain - give create UI.
		var storageProperty = SerializedObject.GetProperty( "Storage" );
		if ( storageProperty is null || storageProperty.IsNull )
		{
			Layout.Add( CreateTerrain() );
			return;
		}

		Layout.Add( SettingsPage() );
	}
}
