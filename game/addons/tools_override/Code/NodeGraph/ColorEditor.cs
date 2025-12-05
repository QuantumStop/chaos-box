using Editor.ShaderGraph;

namespace Editor.NodeEditor;

public class ColorEditor : ValueEditor
{
	public string Title { get; set; }
	public Color Value { get; set; }
	public NodeUI Node { get; set; }

	private SubgraphNode BoundNode;
	private string BoundParameter;

	public ColorEditor( GraphicsItem parent ) : base( parent )
	{
		HoverEvents = true;
		Cursor = CursorShape.Finger;
	}

	protected override void OnPaint()
	{
		if ( !Enabled )
			return;

		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		var bg = Theme.ControlBackground.WithAlpha( 0.4f );
		var fg = Theme.TextControl;

		if ( !Paint.HasMouseOver )
		{
			bg = bg.Darken( 0.1f );
			fg = fg.Darken( 0.1f );
		}

		var rect = LocalRect.Shrink( 1 );

		Paint.ClearPen();
		Paint.SetBrush( bg );
		Paint.DrawRect( rect, 2 );

		var swatchColor = Value;
		var max = MathF.Max( swatchColor.r, swatchColor.g );
		max = MathF.Max( max, swatchColor.b );
		float intensity = 0;

		if ( max > 1 )
		{
			intensity = max / 1000.0f;

			var div = 1.0f / max;
			swatchColor.r *= div;
			swatchColor.g *= div;
			swatchColor.b *= div;
		}

		var colorRect = rect;
		bool hasTitle = !string.IsNullOrWhiteSpace( Title );

		if ( hasTitle )
		{
			colorRect = colorRect.Shrink( colorRect.Width / 2, 0, 0, 0 );
		}

		Paint.SetBrush( "/image/transparent-small.png" );
		Paint.DrawRect( colorRect.Shrink( 2 ), 2 );
		Paint.SetBrushLinear( rect.TopLeft, rect.TopRight, swatchColor.WithAlpha( 1 ), Color.Lerp( swatchColor, Color.White.WithAlpha( swatchColor.a ), intensity ) );
		Paint.DrawRect( colorRect.Shrink( 1 ), 2 );

		Paint.SetDefaultFont();
		Paint.SetPen( fg );

		if ( hasTitle )
		{
			Paint.DrawText( rect.Shrink( 4, 0, 4, 0 ), Title, TextFlag.LeftCenter );
		}
	}

	protected override void OnMousePressed( GraphicsMouseEvent e )
	{
		base.OnMousePressed( e );

		if ( !Enabled )
			return;

		if ( !e.LeftMouseButton )
			return;

		if ( !LocalRect.IsInside( e.LocalPosition ) )
			return;

		var view = Node.GraphicsView;
		var position = view.ToScreen( view.FromScene( ToScene( new Vector2( Size.x + 1, 1 ) ) ) );

		ColorPicker.OpenColorPopup( Value, ( v ) =>
		{
			Value = v;
			if ( BoundNode is not null )
			{
				BoundNode.DefaultValues[BoundParameter] = v;
			}
			Node.Graph.ChildValuesChanged( null );
			Node.Update();
		}, position );

		e.Accepted = true;
	}

	public void BindToParameter( SubgraphNode subgraphNode, string parameter )
	{
		BoundNode = subgraphNode;
		BoundParameter = parameter;

		Value = Color.Parse( subgraphNode.DefaultValues[parameter].ToString() ) ?? Color.White;
	}
}
