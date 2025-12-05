namespace Editor.Wizards;

partial class StandaloneWizard
{
	/// <summary>
	/// Look for files, upload missing
	/// </summary>
	class ProgressWizardPage : StandaloneWizardPage
	{
		public override string PageTitle => "Build";
		public override string PageSubtitle => "Hold tight while we finish building your executable";
		public override bool IsAutoStep => false;

		Widget ProgressBar;
		Label ProgressLabel;
		Label StepLabel;
		Label PercentageLabel;

		StandaloneExporter Exporter;
		StandaloneExporter.ExportProgress LastProgress;

		Group IssuesGroup;
		Label CurrentOperationLabel;

		StandaloneWizard Wizard;

		public ProgressWizardPage( StandaloneWizard wizard )
		{
			Wizard = wizard;
		}

		public override async Task OpenAsync()
		{
			BodyLayout.Clear( true );
			BodyLayout.Margin = new Sandbox.UI.Margin( 0, 0 );
			BodyLayout.Alignment = TextFlag.Left;
			BodyLayout.Spacing = 8;

			BodyLayout.AddStretchCell();

			var titleRow = BodyLayout.AddRow();
			StepLabel = titleRow.Add( new Label.Subtitle( "Building Game Assets" ) );
			titleRow.AddStretchCell();
			PercentageLabel = titleRow.Add( new Label.Subtitle() );

			var row = BodyLayout.AddRow( 1 );

			ProgressBar = row.Add( new Widget( null ), 1 );
			ProgressBar.FixedHeight = 18f;
			ProgressBar.OnPaintOverride = PaintProgress;

			ProgressLabel = BodyLayout.Add( new Label( "Progress Text" ) );
			ProgressLabel.Alignment = TextFlag.Left | TextFlag.DontClip;

			BodyLayout.AddSpacingCell( 16 );

			{
				var group = BodyLayout.Add( new Group( this ) );
				group.Title = "Current Operation";
				group.Icon = "info";
				group.Layout = Layout.Column();
				group.Layout.Margin = new Sandbox.UI.Margin( 14, 32, 14, 14 );
				CurrentOperationLabel = group.Layout.Add( new Label( "Current Operation" ) );
			}

			{
				IssuesGroup = BodyLayout.Add( new Group( this ) );
				IssuesGroup.Title = "Build Issues";
				IssuesGroup.Icon = "warning";
				IssuesGroup.Layout = Layout.Column();
				IssuesGroup.Layout.Margin = new Sandbox.UI.Margin( 14, 32, 14, 14 );
			}

			BodyLayout.AddStretchCell();

			await Refresh();
			Enabled = true;
			Visible = true;

			await Build();
		}

		bool PaintProgress()
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground );
			Paint.DrawRect( ProgressBar.LocalRect, Theme.ControlRadius );

			var r = ProgressBar.LocalRect;
			r.Width *= LastProgress.ProgressFraction.Clamp( 0, 1 );

			Paint.ClearPen();
			Paint.SetBrush( Theme.Blue );
			Paint.DrawRect( r, Theme.ControlRadius );

			return false;
		}

		public async Task Refresh()
		{
			Exporter = await StandaloneExporter.FromConfig( PublishConfig );
			Exporter.OnProgressChanged = ( progress ) =>
			{
				LastProgress = progress;

				ProgressLabel.Text = $"{progress.FilesDone} of {progress.FilesTotal} files processed";
				CurrentOperationLabel.Text = progress.CurrentOperation;
				PercentageLabel.Text = (progress.ProgressFraction * 100f).CeilToInt() + "%";

				IssuesGroup.Visible = progress.BuildIssues.Length > 0;
				IssuesGroup.Layout.Clear( true );

				foreach ( var buildIssue in progress.BuildIssues )
				{
					IssuesGroup.Layout.Add( new Label( buildIssue, IssuesGroup ) );
				}

				Update();
			};
		}

		private bool isFinished = false;

		private async Task Build()
		{
			try
			{
				await Exporter.Run();

				Wizard.wasSuccessful = true;

				StepLabel.Text = "Done!";
				CurrentOperationLabel.Text = "Completed Successfully - hit Next to continue";
			}
			catch
			{
				StepLabel.Text = "Export Failed";
				CurrentOperationLabel.Text = "See the console for more info";
			}
			finally
			{
				isFinished = true;
			}
		}

		public override bool CanProceed()
		{
			return isFinished;
		}
	}
}
