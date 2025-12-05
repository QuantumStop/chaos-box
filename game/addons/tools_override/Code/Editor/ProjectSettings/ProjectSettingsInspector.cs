using Editor.ProjectSettingPages;

namespace Editor;

[Inspector( typeof( Project ) )]
internal sealed class ProjectInspector : InspectorWidget
{
	Layout HeaderLayout;
	SegmentedControl SectionsWidget;
	ScrollArea Scroller;
	Layout FooterLayout;
	Button SaveButton;

	Dictionary<Type, string> Sections;
	Project CurrentProject;
	string Current;

	/// <summary>
	/// Called when we save settings
	/// </summary>
	public Action<Project> OnSave { get; set; }

	/// <summary>
	/// Called when a property changes within a <see cref="Category"/> in the inspector
	/// </summary>
	public Action<SerializedProperty> OnPropertyChanged { get; set; }

	private bool _hasUnsavedChanges;
	private PopupDialogWidget _popup;
	private Button _cancelButton;

	/// <summary>
	/// Does this inspector have any unsaved changes?
	/// </summary>
	bool HasUnsavedChanges
	{
		get
		{
			return _hasUnsavedChanges;
		}

		set
		{
			_hasUnsavedChanges = value;
			SaveButton.Enabled = _hasUnsavedChanges;
		}
	}

	public ProjectInspector( SerializedObject so ) : base( so )
	{
		if ( so.Targets.FirstOrDefault() is not Project project )
			return;

		CurrentProject = project;
		Sections = new();

		MinimumWidth = 386;

		Layout = Layout.Column();
		Layout.Margin = 0;

		var header = Layout.Add( new ProjectSettingsHeader( this, CurrentProject ) );

		HeaderLayout = Layout.Column();
		HeaderLayout.Margin = new( 8, 4 );
		SectionsWidget = HeaderLayout.Add( new SegmentedControl() );
		SectionsWidget.OnSelectedChanged += Select;

		Layout.Add( HeaderLayout );

		Scroller = new ScrollArea( this );
		Scroller.Layout = Layout.Column();
		Scroller.FocusMode = FocusMode.None;

		Layout.Add( Scroller, 1 );

		FooterLayout = Layout.Row();
		FooterLayout.Spacing = 8;
		FooterLayout.Margin = new( 8, 8 );

		Layout.AddSeparator();

		AddFooterDefaults();

		Layout.Add( FooterLayout );

		Scroller.Canvas = new Widget( Scroller );
		Scroller.Canvas.Layout = Layout.Column();
		Scroller.Canvas.Layout.Spacing = 0;
		Scroller.Canvas.Layout.Margin = new( 8, 4 );

		AddSegments();

		OnPropertyChanged += _ => HasUnsavedChanges = true;

		Select( "Project" );
	}

	private void AddSegments()
	{
		AddCategory<ProjectPage>( "Project", "settings" );

		var project = CurrentProject;

		if ( project.Config.Type == "game" )
		{
			AddCategory<GameCategory>( "Project" );
			AddCategory<StandaloneCategory>( "Project" );

			AddCategory<PhysicsCategory>( "Physics", "sports_cricket" );

			AddCategory<InputCategory>( "Input", "mouse" );

			AddCategory<MultiplayerCategory>( "Networking", "wifi" );

			AddCategory<CompilerCategory>( "Compiler", "code" );

			AddCategory<ResourcesCategory>( "Other", "tune" );
			AddCategory<ReferencesCategory>( "Other" );
			AddCategory<CursorCategory>( "Other" );
		}

		if ( project.Config.Type == "map" )
		{
			AddCategory<ReferencesCategory>( "Project" );
		}

		if ( project.Config.Type == "library" )
		{
			AddCategory<CompilerCategory>();
		}

		if ( project.Config.Type == "tool" )
		{
			AddCategory<CompilerCategory>();
		}
	}

	void Reset()
	{
		HasUnsavedChanges = false;

		Select( Current );
	}

	[Shortcut( "editor.save", "CTRL+S" )]
	internal void Save()
	{
		foreach ( var child in Scroller.Canvas.Children.OfType<Category>() )
		{
			child.OnSave();
		}

		HasUnsavedChanges = false;
	}

	void AddFooterDefaults()
	{
		FooterLayout.Clear( true );

		var revert = new Button( "Revert Changes", "history", null );
		revert.Clicked = Reset;

		var save = new Button.Primary( "Save", "save", null );
		save.Clicked = Save;
		save.Enabled = false;

		SaveButton = save;

		FooterLayout.AddStretchCell();
		FooterLayout.Add( revert );
		FooterLayout.Add( save );
	}

	void AddCategory<T>( string section = "Project", string icon = null ) where T : Category
	{
		Sections.Add( typeof( T ), section );

		if ( !SectionsWidget.HasOption( section ) )
		{
			SectionsWidget.AddOption( section, icon );
		}
	}

	void Select( string name )
	{
		using var su = SuspendUpdates.For( this );

		Current = name;
		Scroller.Canvas.Layout.Clear( true );

		var entries = Sections
			.Where( x => x.Value.Equals( name, StringComparison.OrdinalIgnoreCase ) );

		foreach ( var entry in entries )
		{
			var type = EditorTypeLibrary.GetType( entry.Key );

			var section = type.Create<Category>();
			section.InitFromProject( CurrentProject, OnSave, OnPropertyChanged );

			var header = new Label.Header( type.Title, null );
			Scroller.Canvas.Layout.Add( header );
			Scroller.Canvas.Layout.Add( section );
		}

		Scroller.Canvas.Layout.AddStretchCell();
	}

	public static async void OpenForProject( Project project )
	{
		// Try to load the project first
		await Package.FetchAsync( project.Config.FullIdent, false );
		EditorUtility.InspectorObject = project;
	}

	protected override bool OnInspectorClose( object newObj = null )
	{
		// We have no pending changes, let's just close
		if ( !HasUnsavedChanges )
			return true;

		if ( _popup.IsValid() )
		{
			// If this hits, it means we are switching editor layouts and some other inspector is trying to open, so will continue to try until we get a yes/no.
			// So instead of opening a new one, we will just remove the "cancel" button from the existing one, since cancel will just open another popup anyway.
			_cancelButton?.Destroy();
			_cancelButton = null;
			return false;
		}

		_popup = new PopupDialogWidget( "⚙️" );
		_popup.FixedWidth = 462;
		_popup.WindowTitle = $"Project Settings";
		_popup.MessageLabel.Text = $"You have some unsaved settings, do you want to save them?";

		_popup.ButtonLayout.Spacing = 4;
		_popup.ButtonLayout.AddStretchCell();
		_popup.ButtonLayout.Add( new Button( "Save" )
		{
			Clicked = () =>
			{
				Save();
				_popup.Destroy();
				_popup = null;
				// After we are done with the popup, retrigger the inspector change since we previously blocked it.
				EditorUtility.InspectorObject = newObj;
			}
		} );

		_popup.ButtonLayout.Add( new Button( "Don't Save" )
		{
			Clicked = () =>
			{
				Reset();
				_popup.Destroy();
				_popup = null;
				EditorUtility.InspectorObject = newObj;
			}
		} );
		_cancelButton = _popup.ButtonLayout.Add( new Button( "Cancel" ) { Clicked = () => { _popup.Destroy(); } } );

		_popup.SetModal( true, true );
		_popup.Show();

		return false;
	}

	internal partial class Category : Widget
	{
		public Project Project { get; private set; }

		protected Action<Project> SaveCallback;
		protected Action<SerializedProperty> PropertyChangedCallback;

		public Layout BodyLayout;

		public Category() : base( null )
		{
			Layout = Layout.Column();
			BodyLayout = Layout.AddColumn();
			BodyLayout.Spacing = 4;
		}

		/// <summary>
		/// Listens for changes to a <see cref="SerializedObject"/> and lets the inspector know we changed a value
		/// </summary>
		/// <param name="so"></param>
		protected void ListenForChanges( SerializedObject so )
		{
			so.OnPropertyChanged = StateHasChanged;
		}

		/// <summary>
		/// This tells the project settings inspector that a property has changed, so we know to notify the user.
		/// </summary>
		/// <param name="prop"></param>
		protected void StateHasChanged( SerializedProperty prop = null )
		{
			PropertyChangedCallback?.Invoke( prop );
		}

		public void InitFromProject( Project project, Action<Project> saveCallback, Action<SerializedProperty> propertyCallback )
		{
			Project = project;
			SaveCallback = saveCallback;
			PropertyChangedCallback = propertyCallback;

			OnInit( project );
		}

		public virtual void OnInit( Project project )
		{
		}

		public Layout StartSection( string name, Layout layout = null )
		{
			layout ??= BodyLayout;

			var block = layout.AddColumn();
			block.Add( new Label.Header( name ) );

			return block;
		}

		public virtual void OnSave()
		{
			EditorUtility.Projects.Updated( Project );
			SaveCallback?.Invoke( Project );
		}

		/// <summary>
		/// Rebuild the page on hotload for quick iteration
		/// </summary>
		[EditorEvent.Hotload]
		public void Reset()
		{
			BodyLayout.Clear( true );

			InitFromProject( Project, SaveCallback, PropertyChangedCallback );
		}
	}
}
