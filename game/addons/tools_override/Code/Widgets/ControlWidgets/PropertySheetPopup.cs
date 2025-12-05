namespace Editor;

/// <summary>
/// This is created using EditorUtility.OpenControlSheet
/// </summary>
public class PopupEditor : Widget, IPopupEditor<object>
{
	public SerializedObject SerializedObject { get; init; }

	public PopupEditor( SerializedObject so, Widget parent ) : base( parent )
	{
		SerializedObject = so;
		SerializedObject.OnPropertyChanged += OnPropertyChanged;

		MinimumWidth = 400;
		MaximumHeight = 1720;

		Initialize();
	}

	public override void OnDestroyed()
	{
		if ( SerializedObject is not null )
		{
			SerializedObject.OnPropertyChanged -= OnPropertyChanged;
		}

		base.OnDestroyed();
	}

	public virtual void OnPropertyChanged( SerializedProperty property )
	{

	}

	public virtual void Initialize()
	{
		var properties = CreateProperties( this );

		Layout = Layout.Column();
		Layout.Add( properties, 1 );

		var height = properties.Canvas.Height + 8;
		if ( height > 600 ) height = 600;

		MinimumHeight = height;
	}

	public virtual ScrollArea CreateProperties( Widget parent )
	{
		var scrollArea = new ScrollArea( this );
		scrollArea.Canvas = new Widget( this );
		scrollArea.NoSystemBackground = true;
		scrollArea.TranslucentBackground = true;
		scrollArea.SetStyles( "QScrollArea { border: 0px solid red; }" );

		scrollArea.Canvas.VerticalSizeMode = SizeMode.CanGrow;
		scrollArea.Canvas.Layout = Layout.Column();
		scrollArea.Canvas.Layout.Margin = 8;
		scrollArea.Canvas.Layout.Add( ControlSheet.Create( SerializedObject ) );
		scrollArea.Canvas.Layout.AddStretchCell();

		scrollArea.Canvas.UpdateGeometry();
		scrollArea.Canvas.AdjustSize();

		return scrollArea;
	}

	protected override bool OnClose()
	{
		//ProjectCookie.Set( $"SerializedObjectPopup/{cookieName}/Position", Window.Position );
		return true;
	}

	protected override void OnKeyPress( KeyEvent e )
	{
		if ( e.Key == KeyCode.Escape )
		{
			Close();
			e.Accepted = true;
			return;
		}

		base.OnKeyPress( e );
	}
}
