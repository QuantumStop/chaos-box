namespace Editor;

class MountSourceNode : FolderNode
{
	public override string Name => Value.Name;

	private bool IsMounted;

	public MountSourceNode( MountLocation location, bool isMounted ) : base( location )
	{
		Icon = location.Icon;
		IsMounted = isMounted;
	}

	protected override void BuildChildren()
	{
		Clear();

		foreach ( var dir in Value.GetDirectories().OrderBy( x => x.Name ) )
		{
			AddItem( CreateChildFor( dir ) );
		}
	}

	protected override TreeNode CreateChildFor( LocalAssetBrowser.Location dir ) => new MountSourceNode( dir as MountLocation, IsMounted );

	public override void OnPaint( VirtualWidget item )
	{
		PaintSelection( item );

		var color = IsMounted ? Theme.Text : Theme.TextDisabled;

		var rect = item.Rect;

		Paint.SetPen( Value.IsRoot ? color : Theme.Yellow );
		Paint.DrawIcon( rect, Icon, 16, TextFlag.LeftCenter );

		rect.Left += 24;

		Paint.SetPen( color );
		Paint.SetDefaultFont();
		Paint.DrawText( rect, TitleCase ? Name.ToTitleCase() : Name, TextFlag.LeftCenter );
	}

	public override bool OnContextMenu()
	{
		var m = new ContextMenu();

		if ( Value.IsRoot && Value is MountLocation ml )
		{
			var mountedOption = new Option()
			{
				Text = "Mounted",
				Checkable = true,
				Checked = IsMounted,
				Toggled = async ( b ) =>
				{
					await EditorUtility.Mounting.SetMounted( ml.Source.Ident, b );
					Dirty();
				}
			};

			m.AddOption( mountedOption );

			m.AddOption( "Reload", "refresh", async () =>
			{
				await EditorUtility.Mounting.Refresh( ml.Source.Ident );
				Dirty();
			} );
		}

		m.AddOption( "Copy Path", "content_paste", () => { EditorUtility.Clipboard.Copy( Value.Path ); } );
		m.OpenAtCursor();

		return true;
	}
}

