using Editor.Wizards;

internal static class MenuButtons
{
	[Menu( "Editor", "Project/Settings..", "tune", Priority = -2 )]
	static void OpenProjectSettings()
	{
		if ( Project.Current is Project project )
		{
			ProjectInspector.OpenForProject( project );
		}
	}

	[Menu( "Editor", "Project/Publish..", "upload_file", Priority = -2 )]
	static void OpenProjectPublish()
	{
		if ( Project.Current is Project project )
		{
			PublishWizard.Open( project );
		}
	}

	[Menu( "Editor", "Project/Export..", "open_in_browser", Priority = -2 )]
	static void OpenStandaloneExport()
	{
		if ( Project.Current is Project project )
		{
			StandaloneWizard.Open( project );
		}
	}
}
