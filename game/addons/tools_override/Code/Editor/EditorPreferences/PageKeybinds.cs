using System.Reflection;

namespace Editor.Preferences;

internal class PageKeybinds : Widget
{
	Widget KeybindList;
	string Query = "";

	public PageKeybinds( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();
		Layout.Margin = 32;
		var scrollbox = new ScrollArea( this );
		scrollbox.Canvas = new Widget( scrollbox );
		scrollbox.Canvas.Layout = Layout.Column();
		scrollbox.Canvas.VerticalSizeMode = SizeMode.Flexible;
		scrollbox.Canvas.HorizontalSizeMode = SizeMode.Flexible;
		scrollbox.Canvas.ContentMargins = new Sandbox.UI.Margin( 0, 0, 16, 0 );
		Layout.Add( scrollbox );

		scrollbox.Canvas.Layout.Add( new Label.Subtitle( "Editor Keybinds" ) );

		var searchbar = scrollbox.Canvas.Layout.Add( new LineEdit(), 1 );
		searchbar.PlaceholderText = $"⌕  Search";
		searchbar.TextChanged += OnTextChanged;
		scrollbox.Canvas.Layout.AddSpacingCell( 4 );

		KeybindList = new Widget();
		scrollbox.Canvas.Layout.Add( KeybindList );
		scrollbox.Canvas.Layout.AddStretchCell();

		var resetBtn = new Button.Primary( "Reset to Default", "history" );
		resetBtn.Clicked += () =>
		{
			var confirm = new PopupWindow(
					$"Reset Editor Keybinds",
					$"Are you sure you wish to reset all editor keybinds?\nThis process cannot be reverted.", "No",
					new Dictionary<string, Action>() { { "Yes", () => {
						EditorPreferences.ShortcutOverrides = new();
						EditorEvent.Run( "keybinds.update" );
						Rebuild();
					} } }
				);
			confirm.Show();
		};

		Layout.AddSpacingCell( 8 );
		Layout.Add( resetBtn );

		Rebuild();
	}

	[EditorEvent.Hotload]
	void Rebuild()
	{
		KeybindList.Layout?.Clear( true );
		KeybindList.Layout ??= Layout.Column();

		KeybindList.Layout.Margin = 0;
		KeybindList.Layout.Spacing = 2;

		var groups = EditorShortcuts.Entries.GroupBy( x => x.Group ).OrderBy( x => x.Key );

		foreach ( var group in groups )
		{
			var collapsibleCategory = new CollapsibleCategory( null, group.Key );

			List<string> idents = new();
			foreach ( var entry in group.OrderBy( x => x.Name ) )
			{
				if ( idents.Contains( entry.Identifier ) ) continue;
				if ( !string.IsNullOrEmpty( Query ) && !entry.Name.ToLower().Contains( Query.ToLower() ) && !EditorShortcuts.GetKeys( entry.Identifier ).ToLower().Contains( Query.ToLower() ) ) continue;
				var keybindPanel = new KeybindPanel( entry.Identifier, entry.Name );
				collapsibleCategory.Container.Layout.Add( keybindPanel );
				idents.Add( entry.Identifier );
			}

			if ( idents.Count == 0 )
			{
				collapsibleCategory.Destroy();
				continue;
			}

			KeybindList.Layout.Add( collapsibleCategory );
			collapsibleCategory.StateCookieName = $"keybinds.category.{group.Key}";

			KeybindList.Layout.Add( collapsibleCategory );
		}
	}

	void OnTextChanged( string value )
	{
		Query = value;
		Rebuild();
	}

	bool IsPropertyAcceptable( PropertyInfo x )
	{
		if ( !x.CanRead ) return false;
		if ( x.GetMethod.IsStatic ) return false;

		var info = DisplayInfo.ForMember( x );
		return info.Browsable;
	}

	partial class KeybindPanel : Widget
	{
		string Ident { get; }
		string DisplayName { get; }
		int Index { get; set; } = -1;

		IconButton btnRevert;
		IconButton btnClear;
		Widget icoWarning;

		public KeybindPanel( string ident, string name ) : base( null )
		{
			Ident = ident;
			DisplayName = name;
			MinimumHeight = 24;
			VerticalSizeMode = SizeMode.CanGrow;

			Layout = Layout.Row();
			Layout.Spacing = 1;

			var keyboardStr = EditorShortcuts.GetKeys( Ident );
			var keyboard = new ActionCellKeybind( this, keyboardStr, x =>
			{
				var overrides = EditorPreferences.ShortcutOverrides;
				overrides[Ident] = x;
				EditorPreferences.ShortcutOverrides = overrides;
				btnRevert.Visible = true;
				ActionChanged();
			} );
			keyboard.ToolTip = "Press to set the keyboard code for this shortcut.";
			keyboard.FixedWidth = 180;

			var lblName = new Label( this );
			lblName.Text = DisplayName;
			lblName.ContentMargins = new Sandbox.UI.Margin( 8, 0, 0, 0 );
			Layout.Add( lblName, 2 );

			icoWarning = new Widget( this )
			{
				FixedWidth = 16,
				FixedHeight = 16,
				Cursor = CursorShape.Finger
			};
			icoWarning.OnPaintOverride = () =>
			{
				Paint.SetPen( Color.Transparent );
				Paint.SetBrush( Pixmap.FromFile( "common/icon_warning_sm.png" ) );
				Paint.DrawRect( LocalRect );
				return false;
			};
			Layout.Add( icoWarning );
			Layout.AddSpacingCell( 4 );
			UpdateWarnings();

			btnRevert = new IconButton( "history" );
			btnRevert.ToolTip = "Revert to default";
			btnRevert.OnClick = () =>
			{
				var overrides = EditorPreferences.ShortcutOverrides;
				overrides.Remove( Ident );
				EditorPreferences.ShortcutOverrides = overrides;
				ActionChanged();
				btnRevert.Visible = false;
				btnClear.Visible = EditorShortcuts.GetDefaultKeys( Ident ) != "";
				keyboard.Value = EditorShortcuts.GetKeys( Ident );
			};
			if ( !EditorPreferences.ShortcutOverrides.ContainsKey( Ident ) ) btnRevert.Visible = false;
			Layout.Add( btnRevert );

			btnClear = new IconButton( "clear" );
			btnClear.ToolTip = "Clear";
			btnClear.OnClick = () =>
			{
				var overrides = EditorPreferences.ShortcutOverrides;
				overrides[Ident] = "";
				EditorPreferences.ShortcutOverrides = overrides;
				btnClear.Visible = false;
				btnRevert.Visible = EditorShortcuts.GetDefaultKeys( Ident ) != "";
				keyboard.Value = "N/A";
				ActionChanged();
			};
			if ( string.IsNullOrEmpty( keyboardStr ) ) btnClear.Visible = false;
			Layout.Add( btnClear );

			// Layout.Add( new ActionCellEditable( this, Action.GroupName, x => Action.GroupName = x ), 1 );

			Layout.Add( keyboard, 1 );
		}

		protected override void OnPaint()
		{
			if ( Index == -1 )
				Index = Parent?.Children?.ToList()?.IndexOf( this ) ?? -1;

			if ( Index % 2 == 1 )
				Paint.SetBrushAndPen( Theme.WidgetBackground.Darken( 0.1f ) );
			else
				Paint.SetBrushAndPen( Theme.WidgetBackground );
			Paint.DrawRect( LocalRect );

			base.OnPaint();
		}

		internal void ActionChanged()
		{
			// Tell all our widgets to re-register their keybinds
			EditorEvent.Run( "keybinds.update" );
		}

		[Event( "keybinds.update" )]
		void UpdateWarnings()
		{
			var keys = EditorShortcuts.GetKeys( Ident );
			if ( string.IsNullOrEmpty( keys ) )
			{
				icoWarning.Visible = false;
				return;
			}
			var entries = EditorShortcuts.Entries
							.Where( x => x.Keys == keys )
							.GroupBy( x => x.Identifier )
							.Select( x => x.FirstOrDefault() ).ToList();
			if ( entries.Count > 1 )
			{
				string str = "Shared with ";
				List<string> conflicts = new();
				int index = 0;
				foreach ( var entry in entries )
				{
					if ( entry.Identifier == Ident ) continue;
					if ( conflicts.Contains( entry.Identifier ) ) continue;
					conflicts.Add( entry.Identifier );
					if ( index > 0 ) str += ", ";
					str += $"\"{entry.Identifier}\"";
					index++;
				}
				icoWarning.ToolTip = str;
				icoWarning.Visible = true;
			}
			else
			{
				icoWarning.Visible = false;
			}
		}

		class ActionCellKeybind : KeyBind
		{
			string Icon = "keyboard";
			protected override bool AllowModifiers => true;

			public ActionCellKeybind( Widget parent, string bind, Action<string> onEdited = null )
			{
				Value = bind;
				ValueChanged = onEdited;
				MinimumWidth = 150;
			}

			protected override void OnPaint()
			{
				if ( IsTrapping )
				{
					// Paint base
					base.OnPaint();
					return;
				}

				var rect = LocalRect;

				var pen = Theme.TextControl;

				Paint.ClearPen();

				var col = Color.Transparent;
				if ( IsUnderMouse ) col = Color.White.WithAlpha( 0.2f );

				Paint.SetBrush( col );
				Paint.DrawRect( rect );

				Paint.SetPen( pen );
				Paint.SetDefaultFont();

				var textRect = rect;

				if ( Icon != null )
				{
					var iconRect = rect;
					iconRect.Left += 8;

					Paint.DrawIcon( iconRect, Icon, 16, TextFlag.LeftCenter );
				}

				Paint.DrawText( textRect, Value, TextFlag.Center );
			}
			protected override string GetKeyName( KeyEvent e )
			{
				return e.Name.ToUpperInvariant();
			}
		}

		abstract class InputCell : Widget
		{
			public string Text { get; set; }
			public string Icon { get; set; }

			public InputCell( Widget parent, string text, string icon = null ) : base( parent )
			{
				MinimumSize = 24;

				Text = text;
				Icon = icon;
				Cursor = CursorShape.Finger;
			}

			protected override void OnPaint()
			{
				var rect = LocalRect;
				var pen = Theme.TextControl;

				Paint.ClearPen();

				var col = new Color( 0.23f, 0.224f, 0.238f, 1f );
				if ( IsUnderMouse ) col = col.Lighten( 0.2f ).Saturate( 0.4f );

				if ( Text == "None" )
					pen = pen.WithAlpha( 0.5f );

				Paint.SetBrush( col );
				Paint.DrawRect( rect );

				Paint.SetPen( pen );
				Paint.SetDefaultFont();

				var textRect = rect;

				if ( Icon != null )
				{
					var iconRect = rect;
					iconRect.Left += !string.IsNullOrEmpty( Text ) ? 8 : 0;

					Paint.DrawIcon( iconRect, Icon, 16, (Text != null) ? TextFlag.LeftCenter : TextFlag.Center );
				}
				else
				{
					textRect.Left += 8;
				}

				Paint.DrawText( textRect, Text, Icon != null ? TextFlag.Center : TextFlag.LeftCenter );
			}
		}
	}
}
