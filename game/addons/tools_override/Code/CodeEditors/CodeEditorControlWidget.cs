
namespace Editor;

[CustomEditor( typeof( ICodeEditor ) )]
public class CodeEditorControlWidget : ControlWidget
{
	public CodeEditorControlWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Row();

		var comboBox = new ComboBox( this );

		var codeEditors = EditorTypeLibrary.GetTypes<ICodeEditor>()
			.Where( x => !x.IsInterface )
			.OrderBy( x => x.Name );

		// If we have no code editors, the combobox will end up defaulting to a code editor we don't have installed.
		if ( !codeEditors.Any() )
		{
			comboBox.AddItem( "None - install one!", "error" );
		}

		foreach ( var codeEditor in codeEditors.OrderByDescending( x => x.Create<ICodeEditor>()?.IsInstalled() ) )
		{
			if ( codeEditor.TargetType == typeof( ICodeEditor ) ) continue;

			var instance = codeEditor.Create<ICodeEditor>();

			comboBox.AddItem(
				codeEditor.Title,
				codeEditor.Icon,
				() => property.SetValue( codeEditor.Create<ICodeEditor>() ),
				codeEditor.Description,
				false,
				instance.IsInstalled()
			);
		}

		if ( CodeEditor.Current is not null )
		{
			comboBox.TrySelectNamed( DisplayInfo.ForType( CodeEditor.Current.GetType() ).Name );
		}

		Layout.Add( comboBox );
	}
}
