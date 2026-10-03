namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTagInputPage
{
	private static readonly string[] _scheduleSuggestions =
	[
		"Production",
		"Development",
		"Test",
		"Training",
		"ReRun",
		"Demo"
	];

	protected List<string> BasicTags { get; set; } = [];
	protected List<string> SuggestionTags { get; set; } = ["Production"];
	protected List<string> RestrictedTags { get; set; } = [];
	protected List<string> LimitedTags { get; set; } = [];
	protected List<string> TemplateTags { get; set; } = ["Demo"];
	protected List<string> ThemedTags { get; set; } = ["Production", "Test"];
	private readonly List<string> _fixedTags = ["Production", "Training", "Demo"];

	// Wizard demo
	protected List<string> WizardTags { get; set; } = [];
	protected string WizardName { get; set; } = string.Empty;
	private string? _wizardResult;

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	private void OnLogEvent(string name)
	{
		EventManager?.Add(new Event(name));
	}

	private void OnTagRejected(TagRejectedEventArgs args)
	{
		OnLogEvent($"TagRejected: '{args.Tag}' ({args.Reason})");
	}

	private void OnWizardComplete()
	{
		_wizardResult = $"Completed: {WizardName} [{string.Join(", ", WizardTags)}]";
	}

	private void OnWizardCancel()
	{
		_wizardResult = "Cancelled";
	}
}
