using Sandbox.Audio;

namespace Editor.Audio;


public class MixerTreeNode : TreeNode<Mixer>
{
	bool isSelected = false;
	MixerDock ParentDock;

	public MixerTreeNode( MixerDock parent, Mixer mixer ) : base( mixer )
	{
		ParentDock = parent;
		Height = 22;
	}

	public override bool HasChildren => Value.ChildCount > 0;

	protected override void BuildChildren() => SetChildren( Value.GetChildren(), x => new MixerTreeNode( ParentDock, x ) );

	public override int ValueHash => HashCode.Combine( Value, Value.ChildCount );

	public override void OnPaint( VirtualWidget item )
	{
		Paint.Pen = Theme.Yellow;

		var active = Value.Meter.Current.VoiceCount > 0;

		if ( !active )
			Paint.Pen = Theme.Text;

		isSelected = item.Selected;
		if ( isSelected )
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.Primary );
			Paint.DrawRect( new Rect( 0, item.Rect.Top, 1024, item.Rect.Height ) );
			Paint.Pen = Theme.Text;
		}

		var r = item.Rect;

		r.Left += 4;


		Paint.DrawIcon( r, "settings_input_component", 11, TextFlag.LeftCenter );

		r.Left += 18;

		PaintVolume( r );

		r.Height -= 2;

		Paint.Pen = item.GetForegroundColor().WithAlpha( (item.Selected || active) ? 1.0f : 0.7f );
		Paint.SetDefaultFont( weight: Mixer.Default == Value ? 600 : 400 );
		var textRect = Paint.DrawText( r, Value.Name, TextFlag.LeftCenter );

		if ( Value.Meter.Current.VoiceCount > 0 )
		{
			r.Left += textRect.Width + 8;
			Paint.SetDefaultFont( 7 );
			Paint.Pen = Theme.Green.WithAlpha( 0.5f );
			Paint.DrawText( r, $"{Value.Meter.Current.VoiceCount:n0}", TextFlag.LeftCenter );
		}
	}

	private void PaintVolume( Rect rect )
	{
		var v = Value.Meter.Current.MaxLevel.Clamp( 0, 1 );
		if ( v <= 0 )
			return;

		var vr = rect;
		vr.Width *= v;
		vr.Top += vr.Height - 5;
		vr.Height = 2;

		Paint.SetBrushAndPen( Theme.Green.WithAlpha( 0.5f ) );
		Paint.DrawRect( vr );

	}

	public override bool OnContextMenu()
	{
		var menu = new ContextMenu( null );

		menu.AddOption( "Add Child", "add", AddChild );

		var delete = menu.AddOption( "Delete", "close", () => Value.Destroy() );
		delete.Enabled = !Value.IsMaster;

		{
			var o = menu.AddOption( "Set as Default Mixer", "grade", () => Mixer.Default = Value );
			o.Enabled = Mixer.Default != Value;
		}

		menu.OpenAtCursor();

		return true;
	}

	void AddChild()
	{
		var c = Value.AddChild();
		TreeView.Open( this );
		TreeView.SelectItem( c );
		TreeView.BeginRename();
		ParentDock.SetDirty();
	}

	public override bool CanEdit => true;

	public override string Name
	{
		get => Value.Name;
		set => Value.Name = value;
	}

	public override void OnRename( VirtualWidget item, string text, List<TreeNode> selection = null )
	{
		Name = text;
		ParentDock.SetDirty();
	}
}
