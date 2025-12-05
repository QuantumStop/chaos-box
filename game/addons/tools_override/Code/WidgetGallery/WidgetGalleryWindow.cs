namespace Editor;

/// <summary>
/// When used on a static method, this method will be called to create an example of this panel for the "Widget Gallery" window.
/// This method should create and return a <see cref="Widget"/> that serves as an example usage of the Widget class the method is defined in.
/// You can use [Title], [Icon], etc on this method as well.
/// </summary>
[System.AttributeUsage( System.AttributeTargets.Method )]
public class WidgetGalleryAttribute : System.Attribute
{

}

[EditorApp( "Widget Gallery", "grid_view", "A test window, for testing" )]
public class WidgetGalleryWindow : BaseWindow
{
	NavigationView View;

	public WidgetGalleryWindow()
	{
		WindowTitle = "Widget Gallery";
		SetWindowIcon( "grid_view" );

		Size = new Vector2( 1280, 800 );
		View = new NavigationView( this );

		Layout = Layout.Column();
		Layout.Add( View, 1 );

		Rebuild();
		Show();
	}

	[EditorEvent.Hotload]
	public void Rebuild()
	{
		View.ClearPages();

		var methods = EditorTypeLibrary
			.GetMethodsWithAttribute<WidgetGalleryAttribute>()
			.OrderBy( x => (x.Method.GetCustomAttribute<OrderAttribute>() is OrderAttribute order) ? order.Value : 0 )
			.ThenBy( x => x.Method.Title )
			.Select( x => x.Method );
		foreach ( var g in methods.GroupBy( x => x.Group ?? x.Title ) )
		{
			var f = g.First();

			var option = new NavigationView.Option( g.Key, f.Icon );
			option.CreatePage = () =>
			{
				var scroll = new ScrollArea( null );
				scroll.Canvas = new Widget( scroll );
				scroll.Canvas.Layout = Layout.Column();
				scroll.Canvas.Layout.Margin = 32;

				var body = scroll.Canvas.Layout;

				foreach ( var m in g )
				{
					var widget = m.InvokeWithReturn<Widget>( null );

					body.Add( new Label.Subtitle( m.Title ) );

					if ( m.Description != null )
						body.Add( new Label.Body( m.Description ) );

					body.Add( widget, 1 );
					body.AddSpacingCell( 32 );
				}

				body.AddStretchCell();

				return scroll;
			};

			View.AddPage( option );
		}
	}

	[WidgetGallery]
	[Category( "Buttons" )]
	[Title( "Normal Button" )]
	[Icon( "highlight_alt" )]
	internal static Widget NormalButton()
	{
		return new Button( "Normal Button", "people" );
	}

	/// <summary>
	/// A primary button is used for things that are the most common, positive action. Such as saving or pressing OK.
	/// </summary>
	[WidgetGallery]
	[Category( "Buttons" )]
	[Title( "Primary Button" )]
	[Icon( "highlight_alt" )]
	internal static Widget PrimaryButton()
	{
		return new Button.Primary( "Save Changes", "save" );
	}

	[WidgetGallery]
	[Category( "Buttons" )]
	[Title( "Clear Button" )]
	[Icon( "highlight_alt" )]
	internal static Widget ClearButton()
	{
		return new Button.Clear( "Undo", "history" );
	}

	[WidgetGallery]
	[Category( "Buttons" )]
	[Title( "Danger Button" )]
	[Icon( "highlight_alt" )]
	internal static Widget DangerButton()
	{
		return new Button.Danger( "Delete", "delete" );
	}

	[WidgetGallery]
	[Title( "Layout" )]
	[Icon( "grid_4x4" )]
	[Description( @"<p>Layouts can be used to position elements. Layouts can contain other layouts. They can be columns or rows.</p><p>Any widget can have a layout, and they can define margins and spacing.</p>" )]
	internal static Widget LayoutGallery()
	{
		var view = new Widget( null );
		view.SetSizeMode( SizeMode.CanGrow, SizeMode.CanGrow );
		view.Layout = Layout.Column();
		view.Layout.Spacing = 2;

		view.Layout.Add( new ColouredLabel( Theme.Green, "stretch 1" ), 1 );

		{
			var row = view.Layout.AddRow( 1 );

			row.Spacing = 2;

			row.Add( new ColouredLabel( Theme.Blue, "stretch 1" ), 1 );
			row.Add( new ColouredLabel( Theme.Green, "no stretch" ) );
			row.Add( new ColouredLabel( Theme.Blue, "no stretch" ) );
		}

		{
			var row = view.Layout.AddRow( 1 );

			row.Spacing = 2;

			row.Add( new ColouredLabel( Theme.Green, "stretch 1" ), 1 );
			row.Add( new ColouredLabel( Theme.Blue, "stretch 2" ), 2 );
			row.Add( new ColouredLabel( Theme.Green, "stretch 5" ), 5 );
		}

		{
			var row = view.Layout.AddRow( 1 );

			row.Spacing = 2;

			row.Add( new ColouredLabel( Theme.Blue, "empty stretch cell ->" ) );
			row.AddStretchCell();
			row.Add( new ColouredLabel( Theme.Green, "<- empty stretch cell" ) );
		}

		{
			var row = view.Layout.AddRow( 1 );

			row.Spacing = 2;

			row.AddStretchCell();
			row.Add( new ColouredLabel( Theme.Green, "<- empty stretch cell ->" ) );
			row.AddStretchCell();
			row.Add( new ColouredLabel( Theme.Blue, "<- empty stretch cell ->" ) );
			row.AddStretchCell();
		}

		{
			var row = view.Layout.AddRow( 1 );

			row.Spacing = 2;

			row.AddStretchCell();
			row.Add( new ColouredLabel( Theme.Blue, "<- empty stretch cell" ) );
			row.Add( new ColouredLabel( Theme.Green, "empty stretch cell ->" ) );
			row.AddStretchCell();
		}

		return view;
	}

	/*
	[WidgetGallery]
	[Title( "___Flow Layout" )]
	[Icon( "grid_4x4" )]
	[Description( @"<p>Layouts can be used to position elements. Layouts can contain other layouts. They can be columns or rows.</p><p>Any widget can have a layout, and they can define margins and spacing.</p>" )]
	internal static Widget FlowLayout()
	{
		var view = new Widget( null );
		view.SetSizeMode( SizeMode.CanGrow, SizeMode.CanGrow );
		view.Layout = Layout.Flow();
		view.Layout.Spacing = 2;

		for (int i=0; i<30; i++ )
		{
			var height = System.Random.Shared.Float( 20, 100 );
			view.Layout.Add( new ColouredLabel( Theme.Green, $"stretch {height:n0}px" ) { MinimumSize = height } );

			var row = view.Layout.AddRow();

			for ( int x = 0; x < 10; x++ )
			{
				var width = System.Random.Shared.Float( 20, 50 );
				height = System.Random.Shared.Float( 20, 100 );
				row.Add( new ColouredLabel( Theme.Blue, $"stretch {height:n0}px" ) { MinimumSize = height, FixedWidth = width } );
			}
		}

		return view;
	}
	*/

	[WidgetGallery]
	[Category( "Segmented Control" )]
	[Title( "Segmented Control" )]
	[Icon( "splitscreen" )]
	internal static Widget SegmentedControl()
	{
		var wrapper = new Widget( null );
		wrapper.Layout = Layout.Column();

		var sc = wrapper.Layout.Add( new SegmentedControl() );
		sc.AddOption( "One" );
		sc.AddOption( "Two" );
		sc.AddOption( "Three" );
		sc.AddOption( "Four" );
		sc.AddOption( "Five" );

		var lbl = wrapper.Layout.Add( new Label() );
		lbl.Bind( "Text" ).ReadOnly().From( () => $"Selected: {sc.Selected}", _ => { } );

		return wrapper;
	}

	[WidgetGallery]
	[Category( "Segmented Control" )]
	[Title( "Segmented Control With Icons" )]
	[Icon( "splitscreen" )]
	internal static Widget SegmentedControlWithIcons()
	{
		var wrapper = new Widget( null );
		wrapper.Layout = Layout.Column();

		var sc = wrapper.Layout.Add( new SegmentedControl() );
		sc.AddOption( "One", "search" );
		sc.AddOption( "Two", "home" );
		sc.AddOption( "Three", "menu" );
		sc.AddOption( "Four", "close" );
		sc.AddOption( "Five", "settings" );

		var lbl = wrapper.Layout.Add( new Label() );
		lbl.Bind( "Text" ).ReadOnly().From( () => $"Selected: {sc.Selected}", _ => { } );

		return wrapper;
	}

	public class ColouredLabel : Widget
	{
		Color color;
		string label;

		public ColouredLabel( Color color, string label ) : base( null )
		{
			this.color = color;
			this.label = label;
			MinimumSize = 100;
		}

		protected override void OnPaint()
		{
			Paint.ClearPen();
			Paint.SetBrush( color.Darken( 0.4f ) );
			Paint.DrawRect( LocalRect );

			Paint.SetPen( color );
			Paint.DrawText( LocalRect, label, TextFlag.Center | TextFlag.WordWrap );
		}
	}
}
