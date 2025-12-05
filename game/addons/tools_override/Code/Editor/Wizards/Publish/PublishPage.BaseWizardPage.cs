namespace Editor.Wizards;

internal class PublishWizardPage : BaseWizardPage
{
	/// <summary>
	/// This should replace SharedBag as a way to pass data between pages
	/// </summary>
	public PublishConfig PublishConfig { get; set; }

	public Project Project { get; set; }
}
