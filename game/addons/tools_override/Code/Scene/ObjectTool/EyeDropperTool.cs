namespace Editor;

[EditorTool( "tools.eye-dropper-tool", Hidden = true )]
public class EyeDropperTool : EditorTool
{
	static string LastTool;
	internal static SerializedProperty TargetProperty = null;
	internal static Action OnBackToLastTool;

	string LastSelection;


	public override void OnEnabled()
	{
		base.OnEnabled();

		SubscribeEvents();

		AllowGameObjectSelection = true;
		LastSelection = Manager.CurrentSession.SerializeSelection();

		SceneOverlay.Parent.Cursor = CursorShape.BitmapCursor;
		SceneOverlay.Parent.PixmapCursor = Pixmap.FromFile( "cursors/eyedropper_centered.png" );
	}

	public override void OnDisabled()
	{
		base.OnDisabled();

		UnsubscribeEvents();
		SceneOverlay.Parent.Cursor = CursorShape.Arrow;
	}

	void SubscribeEvents()
	{
		Selection.OnItemAdded += OnItemAdded;
		if ( MainAssetBrowser.Instance?.IsValid ?? false )
		{
			MainAssetBrowser.Instance.Local.OnAssetHighlight += OnItemAdded;
			MainAssetBrowser.Instance.Local.OnHighlight += OnItemAdded;
			Application.OnWidgetClicked += OnWidgetPressed;
		}
	}

	void UnsubscribeEvents()
	{
		Selection.OnItemAdded -= OnItemAdded;
		if ( MainAssetBrowser.Instance?.IsValid ?? false )
		{
			MainAssetBrowser.Instance.Local.OnAssetHighlight -= OnItemAdded;
			MainAssetBrowser.Instance.Local.OnHighlight -= OnItemAdded;
			Application.OnWidgetClicked -= OnWidgetPressed;
		}
	}

	void OnItemAdded( object obj )
	{
		Select( obj );
		UnsubscribeEvents();
		Manager.CurrentSession.DeserializeSelection( LastSelection );
	}

	void OnWidgetPressed( Widget widget, MouseEvent mouseEvent )
	{
		// Allow clicking on a Component Header to select the Component
		if ( widget is ComponentSheetHeader header )
		{
			var component = header.GetComponent();
			if ( component.IsValid() )
			{
				Select( component );
			}

			mouseEvent.Accepted = true;
			return;
		}

		// Allow clicking on a GameObjectControlWidget when it has a Prefab
		if ( widget is GameObjectControlWidget controlWidget )
		{
			var val = controlWidget.SerializedProperty.GetValue<GameObject>();
			if ( val is PrefabScene prefabScene )
			{
				var resource = prefabScene?.Source ?? null;
				var asset = resource != null ? AssetSystem.FindByPath( resource.ResourcePath ) : null;

				if ( asset != null )
				{
					Select( asset );
					mouseEvent.Accepted = true;
				}
			}
		}
	}

	internal static void Select( object obj )
	{
		if ( obj is GameObject gameObject )
		{
			ProcessObject( gameObject );
		}
		else if ( obj is Component component )
		{
			ProcessComponent( component );
		}
		else if ( obj is Asset asset )
		{
			// Process Prefabs as GameObjects
			if ( asset.TryLoadResource( out PrefabFile prefabFile ) && TargetProperty.PropertyType == typeof( GameObject ) )
			{
				ProcessObject( SceneUtility.GetPrefabScene( prefabFile ) );
			}
		}
		BackToLastTool();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();

		if ( !TargetProperty.Parent.Contains( TargetProperty ) )
		{
			BackToLastTool();
			return;
		}


		if ( Gizmo.WasLeftMouseReleased )
		{
			var tr = MeshTrace.Run();
			GameObject hitObject = null;
			if ( tr.Hit )
			{
				hitObject = tr.GameObject;
			}
			ProcessAfterSelection( hitObject );
			return;
		}

		// Allow clicking on the header to select the component when using Eye Dropper
		//if ( Widget.CurrentlyPressedWidget is ComponentSheetHeader )
		//{
		//	var component = TargetObject.Targets.FirstOrDefault() as Component;
		//	if ( component.IsValid() )
		//	{
		//		EyeDropperTool.Select( component );
		//	}
		//	return;
		//}
	}

	static void ProcessObject( GameObject obj )
	{
		if ( TargetProperty is null ) return;
		if ( TargetProperty.PropertyType == typeof( GameObject ) )
		{
			// GameObject Target
			TargetProperty.SetValue( obj );
		}
		else
		{
			// Component Target, search for any enabled components first
			var comp = obj.Components.Get( TargetProperty.PropertyType, FindMode.EnabledInSelfAndDescendants );
			// If none found, search for any that are disabled
			comp ??= obj.Components.Get( TargetProperty.PropertyType, FindMode.DisabledInSelfAndDescendants );
			ProcessComponent( comp );
		}
	}

	static void ProcessComponent( Component comp )
	{
		if ( TargetProperty is null ) return;
		TargetProperty.SetValue( comp );
	}

	async void ProcessAfterSelection( GameObject obj )
	{
		await Task.Delay( 100 );

		if ( obj.IsValid() )
		{
			ProcessObject( obj );
		}

		BackToLastTool();
	}

	public static void SetTargetProperty( SerializedProperty property )
	{
		if ( EditorToolManager.CurrentModeName == nameof( EyeDropperTool ) )
		{
			BackToLastTool();
			return;
		}

		LastTool = EditorToolManager.CurrentModeName;
		EditorToolManager.SetTool( nameof( EyeDropperTool ) );
		TargetProperty = property;
	}

	internal static void BackToLastTool()
	{
		if ( string.IsNullOrEmpty( LastTool ) )
			return;

		EditorToolManager.SetTool( LastTool );
		LastTool = null;
		TargetProperty = null;

		OnBackToLastTool?.Invoke();
	}
}
