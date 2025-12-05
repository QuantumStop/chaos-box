namespace Editor.SpriteEditor;

public class AnimationButton : Widget
{
	private Window SpriteEditor;
	private AnimationList AnimationList;

	public Sprite.Animation Animation { get; private set; }

	public bool Selected => Animation == SpriteEditor?.SelectedAnimation;

	AnimationTextEntry textEntryLabel;

	Drag dragData;
	bool draggingAbove = false;
	bool draggingBelow = false;

	public AnimationButton( AnimationList animationList, Sprite.Animation animation ) : base( null )
	{
		SpriteEditor = animationList.SpriteEditor;
		AnimationList = animationList;
		Animation = animation;

		StatusTip = $"Select Animation \"{Animation.Name}\"";
		Cursor = CursorShape.Finger;

		Layout = Layout.Row();
		Layout.Margin = 4;

		var serializedObject = Animation.GetSerialized();
		serializedObject.TryGetProperty( nameof( Sprite.Animation.Name ), out var name );

		textEntryLabel = new AnimationTextEntry( name );
		textEntryLabel.OnStopEditing = ( value ) =>
		{
			if ( string.IsNullOrEmpty( value ) || SpriteEditor.Sprite.Animations.Where( a => a.Name.ToLowerInvariant() == value.ToLowerInvariant() ).Count() > 1 )
			{
				textEntryLabel.Property.SetValue( textEntryLabel.lastSafeValue );
				Window.ShowNamingError( value );
			}
			else
			{
				textEntryLabel.Property.SetValue( textEntryLabel.lastSafeValue );
				SpriteEditor.ExecuteUndoableAction( "Rename Animation", () =>
				{
					textEntryLabel.Property.SetValue( value );
				} );
			}
			return false;
		};
		Layout.Add( textEntryLabel );

		var duplicateButton = new IconButton( "content_copy" );
		duplicateButton.ToolTip = "Duplicate";
		duplicateButton.StatusTip = "Duplicate Animation";
		duplicateButton.OnClick += DuplicateAnimationPopup;
		Layout.Add( duplicateButton );

		Layout.AddSpacingCell( 4 );

		var deleteButton = new IconButton( "delete" );
		deleteButton.ToolTip = "Delete";
		deleteButton.StatusTip = "Delete Animation";
		deleteButton.OnClick += DeleteAnimationPopup;
		Layout.Add( deleteButton );

		IsDraggable = true;
		AcceptDrops = true;
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		base.OnContextMenu( e );

		var m = new Menu( this );

		m.AddOption( "Rename", "edit", Rename );
		m.AddOption( "Duplicate", "content_copy", DuplicateAnimationPopup );
		m.AddOption( "Delete", "delete", DeleteAnimationPopup );

		m.OpenAtCursor( false );
	}

	protected override void OnPaint()
	{
		if ( Selected )
		{
			Paint.SetBrushAndPen( Theme.Primary.WithAlpha( 0.5f ) );
			Paint.DrawRect( LocalRect );
		}
		else if ( IsUnderMouse )
		{
			Paint.SetBrushAndPen( Theme.Highlight );
			Paint.DrawRect( LocalRect );
		}

		if ( dragData?.IsValid ?? false )
		{
			Paint.SetBrushAndPen( Theme.WindowBackground.WithAlpha( 0.5f ) );
			Paint.DrawRect( LocalRect );
		}

		base.OnPaint();

		if ( draggingAbove )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( LocalRect.TopLeft, LocalRect.TopRight );
			draggingAbove = false;
		}
		else if ( draggingBelow )
		{
			Paint.SetPen( Theme.Primary, 2f, PenStyle.Dot );
			Paint.DrawLine( LocalRect.BottomLeft, LocalRect.BottomRight );
			draggingBelow = false;
		}
	}

	protected override void OnDragStart()
	{
		base.OnDragStart();

		dragData = new Drag( this );
		dragData.Data.Object = Animation;
		dragData.Execute();
	}

	public override void OnDragHover( DragEvent ev )
	{
		base.OnDragHover( ev );

		if ( !TryDragOperation( ev, out var dragDelta ) )
		{
			draggingAbove = false;
			draggingBelow = false;
			return;
		}

		draggingAbove = dragDelta > 0;
		draggingBelow = dragDelta < 0;
	}

	public override void OnDragDrop( DragEvent ev )
	{
		base.OnDragDrop( ev );

		if ( !TryDragOperation( ev, out var delta ) ) return;

		SpriteEditor.ExecuteUndoableAction( "Reorder Animations", () =>
		{
			var list = SpriteEditor.Sprite.Animations;
			var index = list.IndexOf( Animation );
			var movingIndex = index + delta;
			var anim = list[movingIndex];

			SpriteEditor.Sprite.Animations.RemoveAt( movingIndex );
			SpriteEditor.Sprite.Animations.Insert( index, anim );
		} );

		SpriteEditor?.OnSpriteModified?.Invoke();
	}

	bool TryDragOperation( DragEvent ev, out int delta )
	{
		delta = 0;
		var animation = ev.Data.OfType<Sprite.Animation>().FirstOrDefault();

		if ( animation is null || Animation is null || animation == Animation )
		{
			return false;
		}

		var animationList = SpriteEditor.Sprite.Animations;
		var myIndex = animationList.IndexOf( Animation );
		var otherIndex = animationList.IndexOf( animation );

		if ( myIndex < 0 || otherIndex < 0 || myIndex == otherIndex )
		{
			return false;
		}

		delta = otherIndex - myIndex;
		return true;
	}

	private void Rename()
	{
		textEntryLabel.Edit();
	}

	private void Delete()
	{
		SpriteEditor.ExecuteUndoableAction( $"Delete Animation {Animation.Name}", () =>
		{
			SpriteEditor.Sprite.Animations.Remove( Animation );
			if ( SpriteEditor.SelectedAnimation == Animation )
			{
				SpriteEditor.SelectedAnimation = SpriteEditor.Sprite.Animations.FirstOrDefault();
			}
		} );

		SpriteEditor?.OnSpriteModified?.Invoke();
	}

	private void Duplicate( string name )
	{
		if ( !string.IsNullOrEmpty( name ) && !SpriteEditor.Sprite.Animations.Any( x => x.Name.ToLowerInvariant() == name.ToLowerInvariant() ) )
		{
			SpriteEditor.ExecuteUndoableAction( $"Duplicate Animation {Animation.Name}", () =>
			{
				var animJson = Json.Serialize( Animation );
				var newAnim = Json.Deserialize<Sprite.Animation>( animJson );
				newAnim.Name = name;
				int index = SpriteEditor.Sprite.Animations.IndexOf( Animation );
				SpriteEditor.Sprite.Animations.Insert( index + 1, newAnim );
				SpriteEditor.SelectedAnimation = newAnim;
			} );
			SpriteEditor?.OnSpriteModified?.Invoke();
		}
		else
		{
			Window.ShowNamingError( name );
		}
	}

	public void DeleteAnimationPopup()
	{
		var popup = new PopupWidget( SpriteEditor );
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;
		popup.Layout.Spacing = 8;

		popup.Layout.Add( new Label( $"Are you sure you want to delete this animation?" ) );

		var button = new Button.Primary( "Delete" );


		button.MouseClick = () =>
		{
			Delete();
			popup.Visible = false;
		};

		var bottomBar = popup.Layout.AddRow();
		bottomBar.AddStretchCell();
		bottomBar.Add( button );

		popup.Position = Editor.Application.CursorPosition;
		popup.Visible = true;
	}

	void DuplicateAnimationPopup()
	{
		var popup = new PopupWidget( SpriteEditor );
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;
		popup.Layout.Spacing = 8;

		popup.Layout.Add( new Label( $"What would you like to name the duplicated animation?" ) );

		var entry = new LineEdit( popup );
		entry.Text = $"{Animation.Name} 2";
		var button = new Button.Primary( "Duplicate" );

		button.MouseClick = () =>
		{
			Duplicate( entry.Text );
			popup.Visible = false;
		};

		entry.ReturnPressed += button.MouseClick;

		popup.Layout.Add( entry );

		var bottomBar = popup.Layout.AddRow();
		bottomBar.AddStretchCell();
		bottomBar.Add( button );

		popup.Position = Editor.Application.CursorPosition;
		popup.Visible = true;

		entry.Focus();
	}
}
