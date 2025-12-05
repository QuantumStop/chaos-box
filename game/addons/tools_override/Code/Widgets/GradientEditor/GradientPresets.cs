using System.Collections.Immutable;
using static Sandbox.Gradient;

namespace Editor;

/// <summary>
/// A widget which holds a list of user gradient presets, saved in a cookie
/// </summary>
public class GradientPresets : ListView
{
	public GradientEditorWidget GradientEditor { get; set; }

	List<Gradient> userList = new();
	List<Gradient> defaultList = new();

	public record GradientPreset( Gradient gradient, bool isDefault );

	string CookieName;

	public GradientPresets( GradientEditorWidget parent, string cookie = "GradientPresets" ) : base( parent )
	{
		ItemContextMenu = ShowItemContext;
		ItemSelected = OnItemClicked;
		Margin = 10;
		ItemSpacing = 5;

		GradientEditor = parent;
		CookieName = cookie;
		ItemSize = new Vector2( 68, 38 );

		userList = EditorCookie.Get( cookie, userList );

		// defaults
		// Sol: keeping these seperate so we can change these at whim and still play nice with user presets	
		defaultList = new List<Gradient>()
		{
			// Black to White
			new Gradient(
				new ColorFrame(0, "#000000"), // Black
				new ColorFrame(1, "#FFFFFF")  // White
			) { Alphas = ImmutableList<AlphaFrame>.Empty.Add( new AlphaFrame( 0, 1 ) ).Add( new AlphaFrame( 1, 1 ) ) },

			// White to Black
			new Gradient(
				new ColorFrame(0, "#FFFFFF"), // White
				new ColorFrame(1, "#000000")  // Black
			) { Alphas = ImmutableList<AlphaFrame>.Empty.Add( new AlphaFrame( 0, 1 ) ).Add( new AlphaFrame( 1, 1 ) ) },

			// Sunset Gradient
			new Gradient(
				new ColorFrame(0, "#FF4500"), // OrangeRed
				new ColorFrame(0.5f, "#FFD700"), // Gold
				new ColorFrame(1, "#00008B")  // DarkBlue
			) { Alphas = ImmutableList<AlphaFrame>.Empty.Add( new AlphaFrame( 0, 1 ) ).Add( new AlphaFrame( 1, 1 ) ) },

			// Rainbow Gradient
			new Gradient(
				new ColorFrame(0, "#FF0000"), // Red
				new ColorFrame(0.17f, "#FF7F00"), // Orange
				new ColorFrame(0.33f, "#FFFF00"), // Yellow
				new ColorFrame(0.5f, "#00FF00"), // Green
				new ColorFrame(0.67f, "#0000FF"), // Blue
				new ColorFrame(0.83f, "#4B0082"), // Indigo
				new ColorFrame(1, "#8B00FF")  // Violet
			) { Alphas = ImmutableList<AlphaFrame>.Empty.Add( new AlphaFrame( 0, 1 ) ).Add( new AlphaFrame( 1, 1 ) ) },

			// Fire
			new Gradient(
				new ColorFrame(0, "#8B0000"), // DarkRed
				new ColorFrame(0.33f, "#FF0000"), // Red
				new ColorFrame(0.66f, "#FFA500"), // Orange
				new ColorFrame(1, "#FFFF00")  // Yellow
			) { Alphas = ImmutableList<AlphaFrame>.Empty.Add( new AlphaFrame( 0, 1 ) ).Add( new AlphaFrame( 1, 1 ) ) },

			new Gradient(
				new ColorFrame( 0, "#FFFFFF" ), // White
				new ColorFrame( 1, "#FFFFFF" )  // White
			) { Alphas = ImmutableList<AlphaFrame>.Empty.Add( new AlphaFrame( 0, 0 ) ).Add( new AlphaFrame( 1, 1 ) ) },

			new Gradient(
				new ColorFrame( 0, "#000000" ), // Black
				new ColorFrame( 1, "#000000" )  // Black
			) { Alphas = ImmutableList<AlphaFrame>.Empty.Add( new AlphaFrame( 0, 0 ) ).Add( new AlphaFrame( 1, 1 ) ) }
		};

		BuildItems();
	}

	void SaveChanges()
	{
		EditorCookie.Set( CookieName, userList );
	}

	public void OnItemClicked( object value )
	{
		// add
		if ( value is string )
		{
			userList.Add( GradientEditor.Value );

			// save list
			BuildItems();
			SaveChanges();
		}

		if ( value is GradientPreset preset )
		{
			GradientEditor.Value = preset.gradient;
		}
	}

	private void ShowItemContext( object obj )
	{
		if ( obj is not GradientPreset preset ) return;

		var m = new ContextMenu();

		m.AddOption( "Copy", "content_copy", () =>
		{
			var json = JsonSerializer.Serialize( preset.gradient );
			EditorUtility.Clipboard.Copy( json );
		} );

		var clipboard = EditorUtility.Clipboard.Paste();
		var p = m.AddOption( "Paste", "content_paste", () =>
		{
			var value = JsonSerializer.Deserialize<Gradient>( clipboard );

			var i = userList.IndexOf( preset.gradient );
			userList.Remove( preset.gradient );

			userList.Insert( i, value );

			BuildItems();
			SaveChanges();
		} );

		try
		{
			JsonSerializer.Deserialize<Gradient>( clipboard );
		}
		catch ( Exception )
		{
			p.Enabled = false;
		}
		p.Enabled &= !preset.isDefault;

		m.AddSeparator();

		var r = m.AddOption( "Replace", "refresh", () =>
		{
			var i = userList.IndexOf( preset.gradient );
			userList.Remove( preset.gradient );

			userList.Insert( i, GradientEditor.Value );

			BuildItems();
			SaveChanges();
		} ).Enabled = !preset.isDefault;

		m.AddSeparator();

		m.AddOption( "Delete", "delete", () =>
		{
			userList.Remove( preset.gradient );
			BuildItems();
			SaveChanges();
		} ).Enabled = !preset.isDefault;

		m.OpenAtCursor();
	}

	void BuildItems()
	{
		SetItems( defaultList.Select( x => new GradientPreset( x, true ) ) );
		AddItems( userList.Select( x => new GradientPreset( x, false ) ) );
		AddItem( "add" );
	}

	protected override void PaintItem( VirtualWidget item )
	{
		var rect = item.Rect;

		if ( item.Object is GradientPreset preset )
		{
			Paint.SetPen( Theme.Blue.WithAlpha( Paint.HasMouseOver ? 1 : 0.7f ), 3.0f );
			Paint.ClearBrush();
			preset.gradient.PaintBlock( rect );

			Paint.Antialiasing = true;

			// overdraw so we can round the corners
			Paint.ClearBrush();
			Paint.SetPen( Theme.ControlBackground, 3 );
			Paint.DrawRect( rect.Grow( 1 ), 4 );

			if ( Paint.HasMouseOver )
			{
				Paint.SetPen( Theme.Text, 2 );
				Paint.DrawRect( rect, 4 );
			}
		}

		if ( item.Object is string )
		{
			Paint.SetBrush( Theme.Green.WithAlpha( Paint.HasMouseOver ? 0.5f : 0.3f ) );
			Paint.SetBrushRadial( rect.Center, 65, Theme.Green.WithAlpha( Paint.HasMouseOver ? 0.5f : 0.3f ), Theme.Blue.WithAlpha( 0.03f ) );
			Paint.ClearPen();
			Paint.DrawRect( rect, 4 );

			Paint.Antialiasing = true;
			Paint.SetPen( Theme.Green, 2.0f );
			Paint.ClearBrush();
			Paint.SetDefaultFont( 20, 1000 );
			Paint.DrawText( rect, "+" );
		}
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, 4 );

		base.OnPaint();
	}
}
