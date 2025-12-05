namespace Editor;

[CustomEditor( typeof( string ), NamedEditor = "folder" )]
public class FolderControlWidget : ControlWidget
{
	private readonly FolderEdit _folder;

	public string Value
	{
		get => SerializedProperty.As.String;
		set
		{
			SerializedProperty.As.String = value;

			if ( _folder.IsValid() )
				_folder.Value = value;
		}
	}

	public FolderControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Row();

		_folder = Layout.Add( new FolderEdit( this ) );
		_folder.Value = property.GetValue<string>();
		_folder.FolderSelected = ( v ) => Value = v;
	}
}
