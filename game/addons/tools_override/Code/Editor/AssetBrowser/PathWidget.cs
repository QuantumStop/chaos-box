using System.IO;

namespace Editor;

public class PathWidget : Widget
{
	private AssetBrowser Browser { get; init; }

	public Action<string> OnPathEdited;

	private LineEdit LineEdit;
	private Widget SegmentParent;

	private Layout SegmentLayout => SegmentParent.Layout;
	private Layout EditLayout;

	public PathWidget( AssetBrowser assetBrowser ) : base( assetBrowser )
	{
		Browser = assetBrowser;

		MouseClick += StartEditing;

		Layout = Layout.Row();

		EditLayout = Layout.AddRow( 1 );

		var segmentRow = Layout.AddRow();

		SegmentParent = segmentRow.Add( new Widget(), 1 );
		SegmentParent.Layout = Layout.Row();
		SegmentLayout.Margin = new Sandbox.UI.Margin( 2, 0 );
		SegmentParent.HorizontalSizeMode = SizeMode.CanShrink;
		segmentRow.AddStretchCell();

		LineEdit = EditLayout.Add( new LineEdit(), 1 );
		LineEdit.EditingFinished += StopEditing;
		LineEdit.HorizontalSizeMode = SizeMode.Flexible;
		LineEdit.Visible = false;

		Width = 125;
	}

	private void StartEditing()
	{
		if ( LineEdit.IsFocused )
			return;

		LineEdit.Visible = true;
		LineEdit.Focus();
		LineEdit.SelectAll();

		SegmentLayout.Clear( true );
	}

	protected override void OnResize()
	{
		base.OnResize();
		LineEdit.Visible = false;
		UpdateSegments();
	}

	public void UpdateSegments()
	{
		if ( SegmentLayout == null )
			return;

		SegmentLayout.Clear( true );
		Enabled = true;

		var location = Browser.CurrentLocation;
		if ( location is null )
			return;

		LineEdit.Visible = false;
		LineEdit.Value = location.Path;

		float availableWidth = LocalRect.Width - 32 - 64;
		float currentWidth = 0;

		if ( string.IsNullOrWhiteSpace( location.RelativePath ) && !location.IsRoot )
		{
			// not a real path
			SegmentLayout.Add( new PathSegment( Browser, location.Name, location.Path ) );
			Enabled = false;
			return;
		}

		bool hasRoot = location.RootPath is not null;
		var segments = location.RelativePath.NormalizeFilename( hasRoot, false ).Split( ['/'] );

		int truncIdx = -1;
		bool hasVisible = false;

		// work out what segments should be visible
		for ( int i = segments.Length - 1; i >= 0; i-- )
		{
			var segment = segments[i];
			if ( string.IsNullOrEmpty( segment ) && i > 0 ) continue;

			if ( segment == "." ) continue;

			string label = GetSegmentLabel( i, segment, location );
			float segmentWidth = MeasureTextWidth( label );
			if ( currentWidth + segmentWidth + 32 > availableWidth && hasVisible )
			{
				truncIdx = i;
				break;
			}

			currentWidth += segmentWidth;
			hasVisible = true;
		}

		string currentPath = "";
		PathElipses elipses = null;
		for ( int i = 0; i < segments.Length; i++ )
		{
			var segment = segments[i];
			if ( i > 0 && segment.Length == 0 )
				continue;

			currentPath += (i > 0 ? "/" : "") + segments[i];

			var absolutePath = location.RootPath is null ? currentPath : $"{location.RootPath}{currentPath}";
			if ( !absolutePath.EndsWith( "/" ) )
				absolutePath += "/";

			string label = GetSegmentLabel( i, segment, location );

			if ( truncIdx != -1 && i <= truncIdx )
			{
				elipses ??= SegmentLayout.Add( new PathElipses( Browser ) );
				elipses.Paths.Add( (label, absolutePath) );
				continue;
			}

			SegmentLayout.Add( new PathSegment( Browser, label, absolutePath ) );

			// Separator
			bool hasSubdirectories = true;
			if ( i == segments.Length - 1 )
			{
				// current location's segment
				if ( !location.IsValid() ) break;
				hasSubdirectories = location.GetDirectories().Any();
			}

			if ( hasSubdirectories )
			{
				SegmentLayout.Add( new PathSeparator( Browser, absolutePath ) );
			}
		}
	}

	string GetSegmentLabel( int index, string segment, AssetBrowser.Location location )
	{
		if ( index == 0 && location.RootTitle is not null )
			return location.RootTitle;

		return segment.Contains( ':' ) ? $"Drive ({segment})" : segment;
	}

	public static float MeasureTextWidth( string text )
	{
		Paint.SetDefaultFont( 8 );
		return Paint.MeasureText( text ).x + (8 * 2);
	}


	private void StopEditing()
	{
		LineEdit.Visible = false;
		OnPathEdited?.Invoke( LineEdit.Value );

		UpdateSegments();
		Update();
	}

	protected override void OnPaint()
	{
		if ( LineEdit.Visible )
			return;

		Paint.ClearBrush();
		Paint.ClearPen();

		var rect = LocalRect;

		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( rect, Theme.ControlRadius );
	}
}

file class PathSegment : Widget
{
	private AssetBrowser Browser { get; init; }
	private string Label;
	private string TargetPath;
	private string RelativePath
	{
		get
		{
			var assetsPath = Project.Current.GetAssetsPath();
			var relativePath = System.IO.Path.GetRelativePath( assetsPath, TargetPath );
			relativePath = relativePath.Replace( '\\', '/' );
			if ( relativePath.StartsWith( ".." ) ) return null;
			return relativePath.ToLower();
		}
	}

	public PathSegment( AssetBrowser browser, string text, string path ) : base( null )
	{
		Browser = browser;
		Label = text;

		Paint.SetDefaultFont( 8 );
		FixedWidth = PathWidget.MeasureTextWidth( text );

		AcceptDrops = true;
		TargetPath = path;

		Cursor = CursorShape.Finger;
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton )
		{
			if ( !AssetBrowser.Location.TryParse( TargetPath, out var location ) )
				return;

			Browser.NavigateTo( location );
		}
		else if ( e.RightMouseButton )
		{
			var menu = new Menu();
			menu.AddOption( "Show in Explorer", "drive_file_move", action: () => EditorUtility.OpenFileFolder( TargetPath ) );
			menu.AddSeparator();
			var relativePath = RelativePath;
			menu.AddOption( $"Copy Relative Path", "content_paste_go", action: () => EditorUtility.Clipboard.Copy( relativePath ) ).Enabled = relativePath is not null;
			menu.AddOption( $"Copy Absolute Path", "content_paste", action: () => EditorUtility.Clipboard.Copy( TargetPath ) );
			menu.OpenAtCursor();
		}

		e.Accepted = false;
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.ClearBrush();
		Paint.ClearPen();

		if ( Paint.HasMouseOver )
		{
			Paint.SetBrush( Color.White.WithAlpha( 0.1f ) );
			Paint.DrawRect( LocalRect.Shrink( 0, 2 ) );
		}

		Paint.SetPen( Theme.TextControl );
		Paint.SetDefaultFont( 8 );
		Paint.DrawText( LocalRect.Shrink( 8, 0 ), Label, TextFlag.LeftCenter );
	}

	public override void OnDragHover( DragEvent ev )
	{
		if ( !ev.Data.Files.Any() )
		{
			ev.Action = DropAction.Ignore;
			return;
		}

		ev.Action = ev.HasCtrl ? DropAction.Copy : DropAction.Move;
	}

	public override void OnDragDrop( DragEvent ev )
	{
		ev.Action = ev.HasCtrl ? DropAction.Copy : DropAction.Move;

		foreach ( var file in ev.Data.Files )
		{
			var asset = AssetSystem.FindByPath( file );

			if ( asset is null )
			{
				if ( !Path.Exists( file ) ) continue;

				// This isn't an asset so just copy the file in directly
				var destinationFile = Path.Combine( TargetPath, Path.GetFileName( file ) );

				if ( Directory.Exists( file ) )
				{
					// Move Directory
					EditorUtility.RenameDirectory( file, destinationFile );
					DirectoryEntry.RenameMetadata( file, destinationFile );
				}
				else
				{
					// Move File
					if ( Path.GetFullPath( file ) == Path.GetFullPath( destinationFile ) )
						continue;

					if ( ev.Action == DropAction.Copy )
						File.Copy( file, destinationFile );
					else
						File.Move( file, destinationFile );
				}
			}
			else
			{
				if ( asset.IsDeleted ) continue;
				if ( ev.Action == DropAction.Copy )
					EditorUtility.CopyAssetToDirectory( asset, TargetPath );
				else
					EditorUtility.MoveAssetToDirectory( asset, TargetPath );
			}
		}
	}
}

file class PathSeparator : Widget
{
	private ContextMenu menu;

	private AssetBrowser Browser { get; init; }
	public string AbsolutePath { get; init; }

	public PathSeparator( AssetBrowser browser, string absolutePath ) : base( null )
	{
		MinimumWidth = 16;
		AbsolutePath = absolutePath;
		Browser = browser;
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.ClearBrush();
		Paint.ClearPen();

		if ( Paint.HasMouseOver )
		{
			Paint.SetBrush( Color.White.WithAlpha( 0.1f ) );
			Paint.DrawRect( LocalRect.Shrink( 0, 2 ) );
		}

		var rect = LocalRect;

		Paint.SetPen( Theme.TextControl );

		if ( menu.IsValid() )
		{
			Paint.Rotate( 90, rect.Position + new Vector2( 8, 8 ) );
			Paint.DrawIcon( rect + new Vector2( 4, -2 ), "arrow_forward_ios", 8f, TextFlag.Center );
		}
		else
		{
			Paint.DrawIcon( rect, "arrow_forward_ios", 8f, TextFlag.Center );
		}
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		menu?.Close();
		menu = new ContextMenu();

		if ( !AssetBrowser.Location.TryParse( AbsolutePath, out var location ) )
			return;

		foreach ( var subDirectory in location.GetDirectories() )
		{
			menu.AddOption( subDirectory.Name, action: () => Browser.NavigateTo( subDirectory ) );
		}

		menu.OpenAt( ScreenRect.BottomLeft );

		e.Accepted = true;
	}
}

file class PathElipses : Widget
{
	private ContextMenu menu;

	private AssetBrowser Browser { get; init; }
	public List<(string Label, string Path)> Paths { get; init; }

	public PathElipses( AssetBrowser browser ) : base( null )
	{
		FixedWidth = 32;
		Paths = new();
		Browser = browser;
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.ClearBrush();
		Paint.ClearPen();

		if ( Paint.HasMouseOver )
		{
			Paint.SetBrush( Color.White.WithAlpha( 0.1f ) );
			Paint.DrawRect( LocalRect.Shrink( 0, 2 ) );
		}

		Paint.SetPen( Theme.TextControl );
		Paint.SetDefaultFont( 8 );
		Paint.DrawText( LocalRect.Shrink( 8, 0 ), "...", TextFlag.Center );
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		menu?.Close();
		menu = new ContextMenu();

		foreach ( var entry in Paths.Reverse<(string Label, string Path)>() )
		{
			if ( !AssetBrowser.Location.TryParse( entry.Path, out var location ) )
				continue;

			menu.AddOption( entry.Label, action: () => Browser.NavigateTo( location ) );
		}

		menu.OpenAt( ScreenRect.BottomLeft );

		e.Accepted = true;
	}
}
