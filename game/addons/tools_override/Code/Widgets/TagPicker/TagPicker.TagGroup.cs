namespace Editor;

partial class TagPicker
{
	class TagGroup : Widget
	{
		public string Title { get; private set; }

		public TagGroup( string title, bool showCheckbox ) : base( null )
		{
			Title = title;

			Layout = Layout.Column();

			if ( showCheckbox )
			{
				var checkbox = Layout.Add( new Checkbox( Title ) );
				checkbox.StateChanged = OnCheckboxStateChanged;
			}
		}

		private void OnCheckboxStateChanged( CheckState state )
		{
			foreach ( var child in Children )
			{
				if ( child is TagOption option )
				{
					option.MouseLeftPress();
					option.Update();
				}
			}

			Update();
		}

		public TagOption Add( TagOption option )
		{
			Layout.Add( option );
			option.Parent = this;

			return option;
		}
	}
}
