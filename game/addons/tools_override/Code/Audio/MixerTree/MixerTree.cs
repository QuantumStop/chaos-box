using Sandbox.Audio;

namespace Editor.Audio;

public class MixerTree : TreeView
{
	MixerDock ParentDock;

	public MixerTree( MixerDock parent )
	{
		ParentDock = parent;
		FixedWidth = 180;
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		if ( SetContentHash( ContentHash, 0.1f ) )
		{
			Clear();

			var root = new MixerTreeNode( ParentDock, Mixer.Master );
			AddItem( root );
			Open( root );
		}
	}

	int ContentHash() => HashCode.Combine( Mixer.Master );

	protected override void OnPaint()
	{
		Paint.SetBrushAndPen( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, 4 );

		base.OnPaint();

		if ( Visible )
		{
			Update();
		}
	}

	[Shortcut( "editor.delete", "DEL" )]
	void DeleteMixer()
	{
		var selected = SelectedItems.FirstOrDefault();
		if ( selected is Mixer mixer )
		{
			mixer.Destroy();
		}
		ParentDock.SetDirty();
	}

}
