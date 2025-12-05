namespace Editor.RectEditor;

public class MaterialReference : Widget
{
	private readonly ComboBox ComboBox;
	private readonly Label Label;
	private readonly MaterialPicker MaterialPicker;

	public Action<Asset> OnReferenceChanged { get; set; }

	public MaterialReference( Widget parent, Action<Asset> onReferenceChanged ) : base( parent )
	{
		OnReferenceChanged = onReferenceChanged;

		Name = "Material Reference";
		WindowTitle = "Material Reference";
		SetWindowIcon( "texture" );

		MinimumSize = 200;

		Layout = Layout.Column();
		Layout.Margin = 5;
		Layout.Spacing = 5;

		Label = Layout.Add( new Label.Small( "Referenced Materials:", this ) );
		ComboBox = Layout.Add( new ComboBox( this ) );
		MaterialPicker = Layout.Add( new MaterialPicker( this, OnPicked ), 1 );
	}

	public void Select( Asset materialAsset )
	{
		if ( materialAsset is null )
		{
			MaterialPicker.Asset = null;
			return;
		}
		MaterialPicker.Asset = materialAsset;
		OnPicked();
	}

	private void OnPicked()
	{
		OnReferenceChanged?.Invoke( MaterialPicker.Asset );

		if ( MaterialPicker.Asset is not null )
		{
			var index = ComboBox.FindIndex( MaterialPicker.Asset.Name );
			ComboBox.CurrentIndex = index ?? -1;
		}
	}

	public void SetReferences( IReadOnlyCollection<Asset> assets )
	{
		ComboBox.Clear();
		ComboBox.Hidden = true;
		Label.Hidden = true;

		if ( assets is null )
		{
			MaterialPicker.Asset = null;

			return;
		}

		foreach ( var asset in assets )
		{
			ComboBox.AddItem( asset.Name, null, () => MaterialPicker.Asset = asset );
			ComboBox.Hidden = false;
			Label.Hidden = false;
		}

		MaterialPicker.Asset = assets.FirstOrDefault();
	}
}

public class MaterialPicker : Widget
{
	private Asset _asset;
	public Asset Asset
	{
		get => _asset;
		set
		{
			if ( value == _asset )
				return;

			_asset = value;

			OnPicked?.Invoke();
		}
	}

	public Pixmap Pixmap { get; set; }

	private readonly Action OnPicked;

	public MaterialPicker( Widget parent, Action onPicked ) : base( parent )
	{
		AcceptDrops = true;
		Cursor = CursorShape.Finger;

		OnPicked = onPicked;
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		var pixmap = Asset?.GetAssetThumb();
		if ( pixmap != Pixmap )
		{
			Pixmap = pixmap;
			Update();
		}
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		var m = new ContextMenu( this );

		m.AddOption( "Open in Editor", "edit", () => Asset?.OpenInEditor() ).Enabled = Asset is not null && !Asset.IsProcedural;
		m.AddOption( "Find in Asset Browser", "search", () => LocalAssetBrowser.OpenTo( Asset, true ) ).Enabled = Asset is not null;
		m.AddSeparator();
		m.AddOption( "Copy", "file_copy", action: Copy ).Enabled = Asset is not null;
		m.AddOption( "Paste", "content_paste", action: Paste );
		m.AddSeparator();
		m.AddOption( "Clear", "backspace", action: Clear ).Enabled = Asset is not null;

		m.OpenAtCursor( false );
	}

	private void Copy()
	{
		if ( Asset is null )
			return;

		EditorUtility.Clipboard.Copy( Asset.Path );
	}

	private void Paste()
	{
		var path = EditorUtility.Clipboard.Paste();
		Asset = AssetSystem.FindByPath( path );
	}

	private void Clear()
	{
		Asset = null;
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		var picker = AssetPicker.Create( this, AssetType.Material );
		picker.Window.Title = $"Select {AssetType.Material.FriendlyName}";
		picker.OnAssetHighlighted = x => Asset = x.First();
		picker.OnAssetPicked = x => Asset = x.First();
		picker.Show();

		picker.SetSelection( Asset );
	}

	protected virtual void PaintUnder()
	{
		bool hovered = IsUnderMouse;

		Paint.ClearPen();

		if ( hovered )
		{
			Paint.SetPen( Color.Lerp( Theme.ControlBackground, Theme.Primary, 0.6f ), 1 );
			Paint.SetBrush( Color.Lerp( Theme.ControlBackground, Theme.Primary, 0.2f ) );
			Paint.DrawRect( LocalRect.Shrink( 1 ), Theme.ControlRadius );
			return;
		}

		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, Theme.ControlRadius );
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		PaintUnder();

		var height = Height - (Asset is null ? 0 : 20);
		var nSize = MathF.Min( Width, height );
		var topLeft = new Vector2( (Width - nSize) / 2, (height - nSize) / 2 );
		var pixmapRect = new Rect( topLeft, nSize );

		Paint.ClearPen();
		Paint.ClearBrush();
		Paint.Draw( pixmapRect.Shrink( 10 ), Pixmap is null ? AssetType.Material.Icon128 : Pixmap );

		if ( Asset is not null )
		{
			var textRect = LocalRect.Shrink( 10 );
			textRect.Top = textRect.Bottom - 14;

			Paint.SetPen( Theme.Text.WithAlpha( 0.9f ) );
			Paint.SetHeadingFont( 8, 450 );
			var t = Paint.DrawText( textRect, $"{Asset.Name}", TextFlag.LeftCenter );

			textRect.Left = t.Right + 6;
			Paint.SetDefaultFont( 7 );
			Theme.DrawFilename( textRect, Asset.RelativePath, TextFlag.LeftCenter, Color.White.WithAlpha( 0.5f ) );
		}
	}
}
