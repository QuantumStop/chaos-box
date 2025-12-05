using System.IO;

namespace Editor.Wizards;

partial class StandaloneWizard : BaseWizard
{
	Project project;
	public ExportConfig Config = new();

	public override string Title => "Export Game";
	public override string Icon => "open_in_browser";

	bool wasSuccessful = false;

	public StandaloneWizard( Project project )
	{
		this.project = project;
		AddSteps();
	}

	protected void AddSteps()
	{
		var dateString = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";

		Config = new();
		Config.Project = project;
		Config.TargetDir = Path.Combine( project.GetRootPath(), "Exports", dateString );
		Config.ExecutableName = project.Package.Ident;
		Config.AppId = 480; // SpaceWar AppID

		AddStep( new ReviewWizardPage() { Project = project, PublishConfig = Config } );      // this the right ident?
		AddStep( new ProgressWizardPage( this ) { Project = project, PublishConfig = Config } );
		AddStep( new SuccessWizardPage( this ) { Project = project, PublishConfig = Config } );

		Current = Steps.First();
	}

	public override void OnSave()
	{
		EditorUtility.Projects.Updated( project );
	}

	public static StandaloneWizard Open( Project project )
	{
		var w = new StandaloneWizard( project );
		w.CreateWindow( 800, 600 );
		return w;
	}
}

