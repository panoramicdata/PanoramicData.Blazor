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

	private List<string> _basicTags = [];
	private List<string> _suggestionTags = ["Production"];
	private List<string> _restrictedTags = [];
	private List<string> _limitedTags = [];
	private List<string> _templateTags = ["Demo"];
	private List<string> _themedTags = ["Production", "Test"];
	private readonly List<string> _fixedTags = ["Production", "Training", "Demo"];

	// Wizard demo
	private List<string> _wizardTags = [];
	private string _wizardName = string.Empty;
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
		_wizardResult = $"Completed: {_wizardName} [{string.Join(", ", _wizardTags)}]";
	}

	private void OnWizardCancel()
	{
		_wizardResult = "Cancelled";
	}
}
