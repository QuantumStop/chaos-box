namespace Editor.Wizards;

internal class StandaloneWizardPage : BaseWizardPage
{
	/// <summary>
	/// Shared data between pages
	/// </summary>
	public ExportConfig PublishConfig { get; set; }
	public Project Project { get; set; }
}
