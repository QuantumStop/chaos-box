namespace Editor;

[CustomEditor( typeof( Sprite.Animation ), NamedEditor = "AnimationList" )]
public class SpriteAnimationListControlWidget : ControlWidget
{
	public Action SelectionChanged;

	ComboBox _comboBox;
	Sprite _sprite;
	IconButton _btnAdd;
	IconButton _btnDelete;
	int _lastSelectedIndex = -1;
	bool _preventChanges = false;

	public SpriteAnimationListControlWidget( SerializedProperty property, Sprite sprite ) : base( property )
	{
		Layout = Layout.Row();
		Layout.Spacing = 4;

		_sprite = sprite;
		_comboBox = new ComboBox( this );
		_comboBox.Insertion = ComboBox.InsertMode.Skip;
		_comboBox.AllowDuplicates = false;
		_comboBox.Editable = true;

		_btnAdd = new IconButton( "add", CreateNewAnimation, this );
		_btnAdd.FixedHeight = _comboBox.Height;
		_btnAdd.FixedWidth = _comboBox.Height;
		_btnAdd.ToolTip = "Add New Animation";

		_btnDelete = new IconButton( "delete", () =>
		{
			OpenFlyout( "Are you sure you want to delete the selected animation?", null, null, DeleteSelectedAnimation );
		}, this );
		_btnDelete.FixedWidth = _comboBox.Height;
		_btnDelete.FixedHeight = _comboBox.Height;
		_btnDelete.ToolTip = "Delete Selected Animation";

		RebuildComboBox();

		Layout.Add( _comboBox );
		Layout.Add( _btnAdd );
		Layout.Add( _btnDelete );
	}

	public void RebuildComboBox()
	{
		var animationValue = SerializedProperty.GetValue<Sprite.Animation>();

		_preventChanges = true;
		_comboBox.ItemChanged -= OnItemChanged;
		_comboBox.Clear();

		if ( (_sprite?.Animations?.Count ?? 0) == 0 )
		{
			_comboBox.CurrentIndex = 0;
			_btnDelete.Visible = false;
		}
		else
		{
			// Populate the combobox with existing animations
			int index = 0;
			foreach ( var animation in _sprite.Animations )
			{
				var isSelected = animation.Name == animationValue?.Name || index == _lastSelectedIndex;
				_comboBox.AddItem(
					animation.Name,
					null,
					() =>
					{
						if ( _preventChanges ) return;
						SerializedProperty.SetValue( animation );
						SelectionChanged?.Invoke();
						_lastSelectedIndex = index;
					},
					null,
					isSelected,
					true
				);
				index++;
			}
			_btnDelete.Visible = true;
		}

		_comboBox.ItemChanged += OnItemChanged;
		_preventChanges = false;
	}

	private void OnItemChanged()
	{
		if ( string.IsNullOrEmpty( _comboBox.CurrentText ) )
		{
			return;
		}

		bool hasChanged = false;
		if ( _comboBox.CurrentIndex != _lastSelectedIndex && _lastSelectedIndex != -1 )
		{
			hasChanged = true;
		}
		_lastSelectedIndex = _comboBox.CurrentIndex;

		var existingAnimation = _sprite.Animations.FirstOrDefault( x => x.Name == _comboBox.CurrentText );
		if ( existingAnimation is null )
		{
			// Create a new animation if it doesn't exist
			var newAnimation = new Sprite.Animation()
			{
				Name = _comboBox.CurrentText,
				Frames = [new Sprite.Frame { Texture = Texture.White }]
			};
			_sprite.Animations.Add( newAnimation );
			SerializedProperty.SetValue( newAnimation );
			SelectionChanged?.Invoke();
			return;
		}

		SerializedProperty.SetValue( existingAnimation );

		if ( hasChanged )
		{
			SelectionChanged?.Invoke();
		}
	}

	private void CreateNewAnimation()
	{
		var selectedAnimation = SerializedProperty.GetValue<Sprite.Animation>();
		if ( string.IsNullOrEmpty( _comboBox.CurrentText ) || _comboBox.CurrentText == selectedAnimation?.Name )
		{
			var lineEdit = new LineEdit();
			OpenFlyout( "Enter a name for the new animation", null, ( popup, button ) =>
			{
				popup.Layout.Add( lineEdit );
				lineEdit.RegexValidator = "^[a-zA-Z0-9 \\._-]{1,32}$";
				lineEdit.TextChanged += ( newString ) =>
				{
					button.Enabled = !string.IsNullOrEmpty( newString ) && !_sprite.Animations.Any( x => x.Name == newString );
				};
				lineEdit.ReturnPressed += button.MouseClick;
				lineEdit.Focus();
			}, () =>
			{
				CreateNewAnimation( lineEdit.Text );
			} );

			return;
		}

		CreateNewAnimation( _comboBox.CurrentText );
	}

	private void CreateNewAnimation( string name )
	{
		if ( string.IsNullOrEmpty( name ) ) return;
		if ( _sprite.Animations.Any( x => x.Name == name ) ) return;
		var newAnimation = new Sprite.Animation()
		{
			Name = name,
			Frames = [new Sprite.Frame { Texture = Texture.White }]
		};
		_sprite.Animations.Add( newAnimation );
		SerializedProperty.SetValue( newAnimation );
		RebuildComboBox();
		SelectionChanged?.Invoke();
	}

	private void DeleteSelectedAnimation()
	{
		var animation = _sprite.Animations.FirstOrDefault( x => x.Name == _comboBox.CurrentText );
		if ( animation is null ) return;

		_sprite.Animations.Remove( animation );
		RebuildComboBox();

		if ( _sprite.Animations.Count > 0 )
		{
			// Select the first animation if available
			SerializedProperty.SetValue( _sprite.Animations[0] );
		}
		else
		{
			// Clear the selection if no animations are left
			SerializedProperty.SetValue<Sprite.Animation>( null );
		}
		SelectionChanged?.Invoke();
	}

	protected override void OnKeyPress( KeyEvent e )
	{
		base.OnKeyPress( e );

		if ( e.Key == KeyCode.Return || e.Key == KeyCode.Enter )
		{
			var animationValue = SerializedProperty.GetValue<Sprite.Animation>();
			if ( animationValue is not null )
			{
				animationValue.Name = _comboBox.CurrentText;
				RebuildComboBox();
			}
		}
	}

	private static void OpenFlyout( string message, Vector2? position, Action<PopupWidget, Button> onLayout = null, Action onSubmit = null )
	{
		var popup = new PopupWidget( null );
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;
		popup.Layout.Spacing = 8;

		popup.Layout.Add( new Label( message ) );

		var button = new Button.Primary( "Confirm" );

		button.MouseClick += () =>
		{
			onSubmit?.Invoke();
			popup.Close();
		};

		onLayout?.Invoke( popup, button );

		var bottomBar = popup.Layout.AddRow();
		bottomBar.AddStretchCell();
		bottomBar.Add( button );
		popup.Position = position ?? Application.CursorPosition;
		popup.ConstrainToScreen();
		popup.Visible = true;
	}

	protected override void PaintUnder()
	{
		// Do nothing...
	}
}
