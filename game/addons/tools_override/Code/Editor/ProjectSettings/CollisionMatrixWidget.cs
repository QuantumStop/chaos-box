using Sandbox.Physics;


namespace Editor.ProjectSettingPages;

public partial class CollisionMatrixWidget : Widget
{
	public Action ValueChanged { get; set; }

	public CollisionMatrixWidget( Widget parent ) : base( parent )
	{
		MinimumSize = 80;
		SetSizeMode( SizeMode.Expand, SizeMode.Expand );
		Layout = Layout.Column();
	}

	CollisionRules current;

	string[] tags;
	public List<Layout> rows = new();

	float cellSize = 24;
	float labelMargin = 60;

	Widget firstControl;

	[EditorEvent.Hotload]
	public void Hotload()
	{
		if ( current == null ) return;

		Rebuild( current );
	}

	public void Rebuild( CollisionRules from )
	{
		Layout.Clear( true );
		Layout.Margin = 32;
		rows = new List<Layout>();
		firstControl = null;

		current = from;

		var tagBuilder = new List<string>();
		tagBuilder.Add( "solid" );
		tagBuilder.Add( "trigger" );
		tagBuilder.AddRange( from.Defaults.Select( x => x.Key ) );

		tags = tagBuilder.Distinct().ToArray();

		Layout.AddSpacingCell( labelMargin );

		MinimumSize = tags.Length * cellSize + labelMargin + 64 + cellSize + 8;

		foreach ( var r in tags )
		{
			var row = Layout.AddRow();

			rows.Add( row );

			row.Add( new LayerName( r, this ) { MinimumWidth = labelMargin } );
			row.AddSpacingCell( 8 );

			{
				var e = new MatrixButton( this, r, null ) { MinimumSize = cellSize };
				row.Add( e );

				if ( !firstControl.IsValid() )
					firstControl = e;

				row.AddSpacingCell( 8 );
			}

			int iColumn = tags.Length;
			foreach ( var c in tags.Reverse() )
			{
				if ( rows.Count() > iColumn )
				{
					row.AddSpacingCell( cellSize );
					continue;
				}

				var e = new MatrixButton( this, r, c ) { MinimumSize = cellSize };

				row.Add( e );

				iColumn--;
			}

			row.AddStretchCell();
		}

		{
			Layout.AddSpacingCell( 8 );
			var row = Layout.AddRow();
			row.AddSpacingCell( labelMargin + 8 );
			row.Add( new Button.Clear( null, "add", this ) { MaximumSize = cellSize - 2, ToolTip = "Add Tag", MouseClick = OpenAddTagFlyout } );
			row.AddStretchCell();
		}

		Layout.AddStretchCell();
	}

	void OpenAddTagFlyout()
	{
		var popup = new PopupWidget( this );
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;
		popup.Layout.Spacing = 8;

		popup.Layout.Add( new Label( "New Tag Name:" ) );

		var entry = new LineEdit( popup );
		entry.RegexValidator = "^[a-zA-Z0-9\\._-]{1,32}$";

		var button = new Button.Primary( "add" );

		button.MouseClick = () =>
		{
			AddTag( entry.Text );
			popup.Visible = false;
		};

		entry.ReturnPressed += button.MouseClick;

		popup.Layout.Add( entry );

		var bottomBar = popup.Layout.AddRow();
		bottomBar.AddStretchCell();
		bottomBar.Add( button );

		popup.Position = Application.CursorPosition;
		popup.ConstrainToScreen();
		popup.Visible = true;

		entry.Focus();
	}

	string rowHovered;
	string colHovered;

	public void OnHovered( MatrixButton button )
	{
		if ( button.IsValid() )
		{
			rowHovered = button.Left;
			colHovered = button.Right ?? "~tagdefault~";
		}
		else
		{
			rowHovered = null;
			colHovered = null;
		}

		Update();
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		if ( !firstControl.IsValid() )
			return;

		var topRow = firstControl.Position;

		Paint.TextAntialiasing = true;
		Paint.Antialiasing = true;

		//Paint.ClearPen();
		//Paint.SetBrush( Theme.ControlBackground );
		//Paint.DrawRect( LocalRect, Theme.ControlRadius );

		//
		// Top labels
		//
		{
			Paint.SetFont( null, 8 );
			var rotation = -45.0f;
			var x = topRow.x + cellSize * 0.15f;
			var y = topRow.y - cellSize * 0.6f;

			{
				Paint.SetPen( colHovered == "~tagdefault~" ? Color.White : Theme.TextControl.WithAlpha( 0.5f ) );

				var center = new Vector2( x, y );
				Paint.Rotate( rotation, center );
				Paint.DrawText( center, "default" );
				Paint.ResetTransform();
			}

			x += cellSize + 4;

			foreach ( var c in tags.Reverse() )
			{
				Paint.SetPen( colHovered == c ? Color.White : Theme.TextControl.WithAlpha( 0.5f ) );

				var center = new Vector2( x, y );
				Paint.Rotate( rotation, center );
				Paint.DrawText( center, c );
				Paint.ResetTransform();

				x += cellSize;
			}
		}

		var def = current.Defaults.FirstOrDefault( x => x.Key == rowHovered );
		var pair = FindPair( rowHovered, colHovered );
		var tooltip = $"{rowHovered} vs {colHovered} = {pair.ToString().ToLower()} ";

		if ( colHovered == "~tagdefault~" )
		{
			if ( def.Value != CollisionRules.Result.Unset ) tooltip = $"{rowHovered} defaults to {def.Value.ToString().ToLower()}";
			else tooltip = $"{rowHovered} has no default";
		}
		else
		{
			if ( pair == CollisionRules.Result.Unset ) tooltip = null;
		}

		if ( tooltip != null )
		{
			Paint.SetPen( Theme.Text.WithAlpha( 0.4f ) );
			Paint.DrawText( LocalRect.Shrink( 4 ), tooltip, TextFlag.CenterBottom );
		}

		// draw legend 

	}

	public CollisionRules.Result FindPair( string left, string right )
	{
		if ( current is null || left is null || right is null )
			return CollisionRules.Result.Unset;

		return current.Pairs.GetValueOrDefault( (left, right) );
	}

	public void SetPair( string left, string right, CollisionRules.Result rule )
	{
		if ( right == null )
		{
			current.Defaults[left] = rule;
		}
		else
		{
			current.Pairs[(left, right)] = rule;
		}

		Update();
		ValueChanged?.Invoke();
	}

	private void DeleteTag( string layer )
	{
		var toRemove = current.Pairs.Keys
			.Where( x => x.Contains( layer ) )
			.ToArray();

		foreach ( var pair in toRemove )
		{
			current.Pairs.Remove( pair );
		}

		current.Defaults.Remove( layer );

		Rebuild( current );
		ValueChanged?.Invoke();
	}

	private void AddTag( string layer )
	{
		if ( string.IsNullOrWhiteSpace( layer ) )
			return;

		layer = layer.ToLower();

		if ( !current.Defaults.TryAdd( layer, CollisionRules.Result.Unset ) )
			return;

		Rebuild( current );
		ValueChanged?.Invoke();
	}

	public class LayerName : Widget
	{
		public string Layer { get; set; }
		CollisionMatrixWidget Matrix;

		public LayerName( string layer, CollisionMatrixWidget parent ) : base( parent )
		{
			Layer = layer;
			Matrix = parent;
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			bool highlight = Matrix.rowHovered == Layer;
			highlight = highlight || Paint.HasMouseOver;

			Paint.SetPen( highlight ? Color.White : Theme.TextControl.WithAlpha( 0.5f ) );

			Paint.DrawText( LocalRect, Layer, TextFlag.RightCenter );
		}

		protected override void OnMousePress( MouseEvent e )
		{
			base.OnMousePress( e );

			if ( e.RightMouseButton )
			{
				var menu = new ContextMenu( this );
				menu.AddOption( $"Delete \"{Layer}\"", "delete", () => Matrix.DeleteTag( Layer ) );
				menu.OpenAt( e.ScreenPosition );
			}
		}
	}


}
