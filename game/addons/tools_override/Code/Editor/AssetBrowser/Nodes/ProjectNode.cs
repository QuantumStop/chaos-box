namespace Editor;

class ProjectNode : FolderNode
{
	public ProjectNode( LocalAssetBrowser.Location location ) : base( location )
	{

	}

	public override void OnPaint( VirtualWidget item )
	{
		PaintSelection( item );

		var rect = item.Rect;

		Paint.SetPen( Theme.Text );
		Paint.DrawIcon( rect, Icon, 16, TextFlag.LeftCenter );

		rect.Left += 24;

		Paint.SetPen( Theme.Text );
		Paint.SetDefaultFont();
		Paint.DrawText( rect, TitleCase ? Name.ToTitleCase() : Name, TextFlag.LeftCenter );
	}
}
