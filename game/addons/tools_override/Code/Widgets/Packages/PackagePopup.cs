namespace Editor.Widgets.Packages;

public partial class PackagePopup : PopupWidget
{
	public Package Package { get; set; }

	Package.IRevision Latest;

	bool isInstalling;
	Button installButton;

	Layout metaLayout;

	public PackagePopup( Package package, Widget parent ) : base( parent )
	{
		Package = package;
		MinimumSize = new Vector2( 600, 300 );
		MaximumSize = MinimumSize;
		Layout = Layout.Row();

		Rebuild();
	}

	public void Rebuild()
	{
		Layout.Clear( true );
		Layout.Margin = 16;
		Layout.Spacing = 32;

		// Left column
		{
			var l = Layout.AddColumn();
			l.Spacing = 8;

			{
				var r = l.AddRow();
				var fav = r.Add( new Button.Clear( $"{Package.Favourited:n0}", "favorite" ) { ToolTip = "Favourites", MouseClick = () => _ = Package.SetFavouriteAsync( !Package.Interaction.Favourite ) } );
				fav.SetProperty( "is-active", Package.Interaction.Favourite );

				r.AddSpacingCell( 16 );
				r.AddStretchCell();
				var voteUp = r.Add( new Button.Clear( $"{Package.VotesUp:n0}", "thumb_up" ) { ToolTip = "Thumbs Up", MouseClick = () => _ = Package.SetVoteAsync( true ) } );
				var voteDn = r.Add( new Button.Clear( $"{Package.VotesDown:n0}", "thumb_down" ) { ToolTip = "ThumbsDown", MouseClick = () => _ = Package.SetVoteAsync( false ) } );

				voteUp.SetProperty( "is-active", Package.Interaction.Rating == 0 );
				voteDn.SetProperty( "is-active", Package.Interaction.Rating == 1 );
			}


			l.Add( new Button.Clear( "View Online", "link" ) { MouseClick = () => EditorUtility.OpenFolder( Package.Url ) } );
			l.Add( new Button.Clear( "Copy Ident", "content_copy" ) { MouseClick = () => { EditorUtility.Clipboard.Copy( Package.FullIdent ); Close(); } } );



			l.AddStretchCell();

			if ( AssetSystem.CanCloudInstall( Package ) )
			{
				installButton = l.Add( new Button.Clear( "", "" ) { MouseClick = () => _ = Install(), Enabled = !isInstalling } );
				CheckForUpdate();
			}

			if ( CanOpenInEditor )
			{
				l.Add( new Button.Clear( "Open in Editor", "input" ) { MouseClick = () => _ = OpenInEditor() } ); ;
			}
		}

		// Right column
		{
			metaLayout = Layout.AddColumn( 1 );

			RebuildMeta();
		}
	}

	async void CheckForUpdate()
	{
		var local = AssetSystem.GetInstalledRevision( Package.FullIdent );
		if ( local is null )
		{
			installButton.Visible = false;
			return;
		}

		installButton.Visible = true;
		installButton.Text = "Checking for update";
		installButton.Icon = "refresh";
		installButton.TransparentForMouseEvents = true;

		Latest = (await Package.FetchVersions( Package.FullIdent )).FirstOrDefault();
		RebuildMeta();

		if ( !installButton.IsValid() )
			return;

		bool isUpdateAvailable = Latest is not null && Latest.VersionId != local.VersionId;

		if ( isUpdateAvailable )
		{
			installButton.Text = "Update";
			installButton.Icon = "file_download";
			installButton.TransparentForMouseEvents = false;
		}
		else
		{
			installButton.Text = "Up to Date";
			installButton.Icon = "check";
			installButton.TransparentForMouseEvents = true;
		}
	}

	[Event( "package.changed" )]
	void OnPackageChanged( Package package )
	{
		if ( Package.FullIdent != package.FullIdent )
			return;

		Package = package;
		Rebuild();
	}

	void RebuildMeta()
	{
		if ( !metaLayout.IsValid() )
			return;

		metaLayout.Clear( true );

		metaLayout.SizeConstraint = SizeConstraint.SetMaximumSize;
		metaLayout.Add( new Label( Package.Title ) ).SetStyles( "font-family: Poppins; font-size: 18px; font-weight: bold;" );
		metaLayout.Add( new Label.Small( $"Published by: {Package.Org.Title}" ) ).SetStyles( "margin-bottom: 8px;" );

		if ( !string.IsNullOrWhiteSpace( Package.Summary ) )
		{
			metaLayout.Add( new Label( Package.Summary ) { WordWrap = true } ).SetStyles( "margin-bottom: 8px; color: #666;" );
		}

		var w = new Widget()
		{
			MinimumWidth = 170,
			MaximumWidth = 400,
			HorizontalSizeMode = SizeMode.Expand
		};
		metaLayout.Add( w );
		w.Layout = Layout.Column();
		w.Layout.Spacing = 5;

		BuildMetaRow( w.Layout, "Package Type:", Package.TypeName );
		BuildMetaRow( w.Layout, "Created:", Package.Created.LocalDateTime.ToShortDateString() );
		BuildMetaRow( w.Layout, "Users:", $"{Package.Usage.Total.Users}" );
		BuildMetaRow( w.Layout, "References:", $"{Package.Referenced}" );
		BuildMetaRow( w.Layout, "Collections:", $"{Package.Collections}" );
		BuildMetaRow( w.Layout, "Tags:", $"{string.Join( ", ", Package.Tags )}" );

		if ( AssetSystem.CanCloudInstall( Package ) )
		{
			var local = AssetSystem.GetInstalledRevision( Package.FullIdent );
			w.Layout.Add( new Label.Small( "Versions" ) ).SetStyles( "margin-top: 8px;" );

			var remote = (Latest ?? Package.Revision);
			if ( remote is null )
				BuildMetaRow( w.Layout, "Remote", $"{Package.Created.LocalDateTime.ToShortDateString()}" );
			else
				BuildMetaRow( w.Layout, "Remote", $"{remote.VersionId} ({remote.Created.LocalDateTime.ToShortDateString()})" );

			BuildMetaRow( w.Layout, "Local", local is null ? "None" : $"{local.VersionId} ({local.Created.LocalDateTime.ToShortDateString()})" );
		}

		metaLayout.AddStretchCell();
	}

	void BuildMetaRow( Layout l, string label, string value )
	{
		var r = l.AddRow();
		r.Spacing = 5;

		r.Add( new Label( label ) { HorizontalSizeMode = SizeMode.Expand, Alignment = TextFlag.LeftTop } );
		r.AddStretchCell();
		r.Add( new Label( value ) { HorizontalSizeMode = SizeMode.CanShrink, Alignment = TextFlag.RightTop, WordWrap = true } );
	}

	bool CanOpenInEditor
	{
		get
		{
			if ( Package.TypeName == "model" ) return true;
			if ( Package.TypeName == "material" ) return true;

			return false;
		}
	}

	async Task OpenInEditor()
	{
		(await AssetSystem.InstallAsync( Package.FullIdent ))?.OpenInEditor();
	}

	async Task Install()
	{
		if ( isInstalling )
			return;

		isInstalling = true;
		installButton.Text = "Installing..";

		await AssetSystem.InstallAsync( Package.FullIdent, false );

		if ( !IsValid )
			return;

		isInstalling = false;
		Rebuild();
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;

		Paint.SetPen( Theme.WidgetBackground.Lighten( 0.3f ), 2 );
		Paint.SetBrush( Theme.WindowBackground );
		Paint.DrawRect( LocalRect.Shrink( 2 ), 2 );

		Paint.SetPen( Theme.WindowBackground.Darken( 0.8f ), 1 );
		Paint.ClearBrush();
		Paint.DrawRect( LocalRect.Shrink( 1 ), 2 );
	}
}
