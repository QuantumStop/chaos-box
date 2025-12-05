using System;

namespace Editor;

file class GPUResidentTextures : BaseWindow
{
	[Menu( "Editor", "Debug/GPU Resident Textures", "memory" )]
	public static void OpenTextureVisualizer()
	{
		var vis = new GPUResidentTextures();
		vis.Show();
	}

	TableView<TextureResidencyInfo> TableView;
	LineEdit FilterText;
	Label ResidentCountLabel;
	Label ResidentSizeLabel;

	List<TextureResidencyInfo> Data;

	public GPUResidentTextures()
	{
		WindowTitle = "GPU Resident Textures";
		SetWindowIcon( "memory" );
		Size = new Vector2( 1000, 600 );

		TableView = new( this );

		//
		// This is pretty gross
		//

		TableView.Columns.Add( new TableView<TextureResidencyInfo>.Column()
		{
			Name = "Resource Name",
			Width = 300,
			Value = ( info ) => { return string.IsNullOrEmpty( info.Name ) ? "<unnamed>" : info.Name; }
		} );

		TableView.Columns.Add( new TableView<TextureResidencyInfo>.Column()
		{
			Name = "Type",
			Width = 80,
			Value = ( info ) => { return $"{info.Dimension.ToString().Replace( "_", "" )}"; }
		} );

		TableView.Columns.Add( new TableView<TextureResidencyInfo>.Column()
		{
			Name = "Format",
			Width = 80,
			Value = ( info ) => { return $"{info.Format}"; }
		} );

		TableView.Columns.Add( new TableView<TextureResidencyInfo>.Column()
		{
			Name = "Disk Dimensions",
			Width = 120,
			Value = ( info ) => { return info.Disk.Depth > 1 ? $"{info.Disk.Width}x{info.Disk.Height}x{info.Disk.Depth}" : $"{info.Disk.Width}x{info.Disk.Height}"; }
		} );

		TableView.Columns.Add( new TableView<TextureResidencyInfo>.Column()
		{
			Name = "Disk Size",
			Width = 80,
			Value = ( info ) => { return $"{info.Disk.MemorySize.FormatBytes()}"; }
		} );

		TableView.Columns.Add( new TableView<TextureResidencyInfo>.Column()
		{
			Name = "Resident Dimensions",
			Width = 120,
			Value = ( info ) => { return info.Loaded.Depth > 1 ? $"{info.Loaded.Width}x{info.Loaded.Height}x{info.Loaded.Depth}" : $"{info.Loaded.Width}x{info.Loaded.Height}"; }
		} );

		TableView.Columns.Add( new TableView<TextureResidencyInfo>.Column()
		{
			Name = "Loaded Size",
			Width = 80,
			Value = ( info ) => { return $"{info.Loaded.MemorySize.FormatBytes()}"; }
		} );

		Layout = Layout.Column();
		Layout.Add( TableView, 1 );

		var bottom = Layout.Row();

		FilterText = new LineEdit() { MaximumHeight = 24, PlaceholderText = "Filter..." };
		FilterText.TextChanged += ( _ ) => FilterAndSort();
		ResidentCountLabel = new Label();
		ResidentSizeLabel = new Label();

		bottom.Add( new Button() { Text = "Refresh", Clicked = Refresh } );
		bottom.Add( FilterText, 1 );
		bottom.Add( ResidentCountLabel );
		bottom.Add( ResidentSizeLabel );

		bottom.Margin = 8;
		bottom.Spacing = 8;

		Layout.Add( bottom );

		Refresh();
	}

	void Refresh()
	{
		Data = TextureResidencyInfo.GetAll().ToList();
		FilterAndSort();

		ResidentCountLabel.Text = $"{Data.Count} Textures";
		ResidentSizeLabel.Text = $"{Data.Sum( x => x.Loaded.MemorySize ).FormatBytes()} GPU Resident / {Data.Sum( x => x.Disk.MemorySize ).FormatBytes()} Disk Memory";
	}

	void FilterAndSort()
	{
		var data = Data.AsEnumerable();

		if ( FilterText.Text != "" )
		{
			data = data.Where( x => x.Name.Contains( FilterText.Text, StringComparison.OrdinalIgnoreCase ) );
		}

		data = data.OrderByDescending( x => x.Loaded.MemorySize );

		TableView.SetItems( data );
	}
}

/// <summary>
/// This isn't good enough to be public
/// </summary>
file class TableView<T> : Widget
{
	class TableHeader : Widget
	{
		readonly TableView<T> Table;

		public TableHeader( TableView<T> parent ) : base( parent )
		{
			Table = parent;
			MinimumSize = 32;
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground );
			Paint.DrawRect( LocalRect );

			Rect rect = LocalRect;

			foreach ( var column in Table.Columns )
			{
				Paint.SetDefaultFont();
				Paint.SetPen( Theme.Text );
				rect.Width = column.Width;
				Paint.DrawText( rect.Shrink( 8, 0 ), column.Name, column.TextFlag | TextFlag.CenterVertically | TextFlag.SingleLine );
				rect.Left += column.Width;
			}
		}
	}

	public class Column
	{
		public string Name;
		public int Width;
		public Func<T, string> Value;
		public TextFlag TextFlag = TextFlag.Left;
	}

	List<T> Items = new();
	ListView ListView { get; set; }

	public List<Column> Columns { get; set; } = new();

	public TableView( Widget parent ) : base( parent )
	{
		ListView = new( this );

		ListView.ItemPaint = PaintRow;
		ListView.ItemSize = new Vector2( 0, 24 );
		ListView.ItemSpacing = 0;
		ListView.Margin = 0;

		Layout = Layout.Column();
		Layout.Add( new TableHeader( this ) );
		Layout.Add( ListView, 1 );
	}

	public void SetItems( IEnumerable<T> items )
	{
		Items = items.ToList();
		ListView.SetItems( Items.Cast<object>() );
	}

	private void PaintRow( VirtualWidget widget )
	{
		if ( widget.Object is not T t )
			return;

		var isAlt = widget.Row % 2 == 0;
		var backgroundColor = widget.Selected ? Theme.Blue : (widget.Hovered ? Color.White.Darken( 0.2f ) : Color.White.Darken( isAlt ? 0.15f : 0.1f ));

		Paint.ClearPen();
		Paint.SetBrush( backgroundColor );
		Paint.DrawRect( widget.Rect );

		Rect rect = widget.Rect;

		Paint.SetDefaultFont();
		Paint.SetPen( Color.Black );

		foreach ( var column in Columns )
		{
			rect.Width = column.Width;
			Paint.DrawText( rect.Shrink( 8, 0 ), column.Value( t ), column.TextFlag | TextFlag.CenterVertically | TextFlag.SingleLine );
			rect.Left += column.Width;
		}
	}
}
