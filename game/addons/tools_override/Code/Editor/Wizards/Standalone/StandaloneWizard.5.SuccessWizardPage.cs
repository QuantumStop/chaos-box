namespace Editor.Wizards;

partial class StandaloneWizard
{
	/// <summary>
	/// Look for files, upload missing
	/// </summary>
	class SuccessWizardPage : StandaloneWizardPage
	{
		bool Success => Wizard.wasSuccessful;

		public override string PageTitle => Success ? "Export Successful" : "Export failed";
		public override string PageSubtitle => Success ? "We did it!" : "Encountered errors";

		public string ChangeTitle { get; set; }
		public string ChangeDetail { get; set; }

		StandaloneWizard Wizard;

		public SuccessWizardPage( StandaloneWizard wizard )
		{
			Wizard = wizard;
		}

		public override async Task OpenAsync()
		{
			// Play a sound for people to moan about
			EditorUtility.PlayRawSound( Success ? "sounds/editor/published.wav" : "sounds/editor/fail.wav" );

			// Clear the cache so if we try to use this addon it'll download the latest version
			EditorUtility.ClearPackageCache();

			BodyLayout?.Clear( true );
			BodyLayout.Margin = new Sandbox.UI.Margin( 0, 0 );

			BodyLayout.AddStretchCell( 1 );

			BodyLayout.Add( new Label( Success ? "🥳" : "🙁" ) { Alignment = TextFlag.Center } ).SetStyles( "font-size: 100px" );
			BodyLayout.Add( new Label.Subtitle( Success ? "Export Complete!" : "Export Failed" ) { Alignment = TextFlag.Center } );
			BodyLayout.AddSpacingCell( 16 );
			BodyLayout.Add( new Label.Body( Success ? "Congratulations. Your game has been exported." : "Check the console for more info." ) { Alignment = TextFlag.Center } );
			BodyLayout.AddSpacingCell( 16 );

			var r = BodyLayout.AddRow();

			if ( Success )
			{
				r.AddStretchCell();
				r.Add( new Button.Primary( "Open Folder", "folder" )
				{
					Clicked = () => EditorUtility.OpenFolder( PublishConfig.TargetDir )
				} );
				r.AddStretchCell();
			}


			BodyLayout.AddStretchCell( 1 );
			Visible = true;

			await Task.CompletedTask;
		}

		public override bool CanProceed()
		{
			return true;
		}
	}
}

