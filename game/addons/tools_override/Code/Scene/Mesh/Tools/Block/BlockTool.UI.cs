
namespace Editor.MeshEditor;

[Inspector( typeof( BlockTool ) )]
public class BlockToolInspector : InspectorWidget
{
	public BlockToolInspector( SerializedObject so ) : base( so )
	{
		if ( so.Targets.FirstOrDefault() is not BlockTool tool )
			return;

		Layout = Layout.Column();
		Layout.Add( tool.BuildUI() );
	}
}

public partial class BlockTool
{
	private StatusWidget Header { get; set; }
	private Layout ControlLayout { get; set; }

	public Widget BuildUI()
	{
		var widget = new Widget( null );
		widget.Layout = Layout.Column();
		widget.Layout.Margin = 4;

		Header = new StatusWidget( widget );
		UpdateStatus();
		widget.Layout.Add( Header );
		widget.Layout.AddSpacingCell( 8 );

		{
			var layout = widget.Layout.AddRow();
			layout.Spacing = 4;

			var geometryComboBox = new ComboBox();
			foreach ( var builder in GetBuilderTypes() )
			{
				var displayInfo = DisplayInfo.ForType( builder.TargetType );
				geometryComboBox.AddItem( displayInfo.Name, displayInfo.Icon ?? "square", () =>
					Current = _primitives.FirstOrDefault( x => x.GetType() == builder.TargetType ) );
			}

			layout.Add( geometryComboBox, 1 );
		}

		widget.Layout.AddSpacingCell( 8 );

		ControlLayout = widget.Layout.AddRow();
		BuildControlSheet();

		widget.Layout.AddSpacingCell( 8 );
		widget.Layout.AddStretchCell();

		return widget;
	}

	public void OnEdited( SerializedProperty property )
	{
		RebuildMesh();
	}

	private void BuildControlSheet()
	{
		if ( !ControlLayout.IsValid() )
			return;

		if ( Current is null )
			return;

		var so = Current.GetSerialized();
		so.OnPropertyChanged += OnEdited;
		var sheet = new ControlSheet();
		sheet.AddObject( so );
		ControlLayout.Clear( true );
		ControlLayout.Add( sheet );
	}

	private void UpdateStatus()
	{
		if ( !Header.IsValid() )
			return;

		Header.Text = $"{(InProgress ? "Placing" : "Create")} Geometry";
		Header.LeadText = InProgress ? "Press Enter to complete the geometry." : "Drag out a rectangle to create the geometry.";
		Header.Color = InProgress ? Theme.Blue : Theme.Green;
		Header.Icon = InProgress ? "check_circle_outline" : "view_in_ar";
		Header.Update();
	}

	private class StatusWidget : Widget
	{
		public string Icon { get; set; }
		public string Text { get; set; }
		public string LeadText { get; set; }
		public Color Color { get; set; }

		public StatusWidget( Widget parent ) : base( parent )
		{
			MinimumSize = 48;
			SetSizeMode( SizeMode.Default, SizeMode.CanShrink );
		}

		protected override void OnPaint()
		{
			var rect = new Rect( 0, Size );

			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground.Lighten( 0.9f ) );
			Paint.DrawRect( rect );

			rect.Left += 8;

			Paint.SetPen( Color );
			var iconRect = Paint.DrawIcon( rect, Icon, 24, TextFlag.LeftCenter );

			rect.Top += 8;
			rect.Left = iconRect.Right + 8;

			Paint.SetPen( Color );
			Paint.SetDefaultFont( 10, 500 );
			var titleRect = Paint.DrawText( rect, Text, TextFlag.LeftTop );

			rect.Top = titleRect.Bottom + 2;

			Paint.SetPen( Color.WithAlpha( 0.6f ) );
			Paint.SetDefaultFont( 8, 400 );
			Paint.DrawText( rect, LeadText, TextFlag.LeftTop );
		}
	}
}
