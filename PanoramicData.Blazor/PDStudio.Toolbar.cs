namespace PanoramicData.Blazor;

/// <summary>
/// The toolbar and menu content a host can supply, and the examples menu.
/// </summary>
public partial class PDStudio
{
	/// <summary>
	/// Gets or sets content shown in a panel to the left of the editor and its tabs, such as a schema browser.
	/// </summary>
	[Parameter] public RenderFragment? LeftPanelContent { get; set; }

	/// <summary>
	/// Gets or sets content placed at the right-hand end of the toolbar.
	/// </summary>
	[Parameter] public RenderFragment? ToolbarEndContent { get; set; }

	/// <summary>
	/// Gets or sets content placed at the start of the toolbar, before Execute, such as a File menu.
	/// </summary>
	[Parameter] public RenderFragment? ToolbarStartContent { get; set; }

	/// <summary>
	/// Gets or sets toolbar items grouped with Execute, after it and before Cancel when
	/// <see cref="PDStudioOptions.ShowCancelButton"/> is set, such as an Explain button.
	/// </summary>
	[Parameter] public RenderFragment? ExecuteGroupContent { get; set; }

	/// <summary>
	/// Gets or sets a menu joined to Execute as a split button, such as options for the run; null or empty for none.
	/// </summary>
	[Parameter] public List<MenuItem>? ExecuteMenuItems { get; set; }

	/// <summary>
	/// Raised with the key of the item chosen on <see cref="ExecuteMenuItems"/>.
	/// </summary>
	[Parameter] public EventCallback<string> ExecuteMenuClick { get; set; }

	/// <summary>
	/// Gets or sets the tooltip of the menu joined to Execute, such as the options it holds; null for none.
	/// </summary>
	[Parameter] public string? ExecuteMenuToolTip { get; set; }

	// The menu takes Execute's colour, which is Cancel's while Execute is Cancel
	// Execute gives way to its own Cancel button while running, and its menu goes with it
	private bool IsPlayEnabled => !(Options.ShowCancelButton && _isExecuting);

	private string ExecuteMenuCssClass
		=> $"pd-studio-execute-menu {(IsPlayCancel ? Options.CancelButtonCssClass : Options.ExecuteButtonCssClass)}";

	/// <summary>
	/// Gets or sets toolbar items grouped with New and Examples, after them, such as a Restore button.
	/// </summary>
	[Parameter] public RenderFragment? MenuGroupContent { get; set; }

	// The menu group only when something is in it.
	private bool HasMenuButtons
		=> (Options.ShowMenu && (Options.ShowNewButton || CurrentExamples.Count > 0)) || MenuGroupContent != null;

	// Without a Cancel button of its own, Execute turns into Cancel while code runs.
	private bool IsPlayCancel => _isExecuting && !Options.ShowCancelButton;

	// One width for Execute and Cancel only matters when the one button is both.
	private string PlayCssClass => Options.ShowCancelButton
		? Options.ExecuteButtonCssClass
		: $"pd-studio-execute {(_isExecuting ? Options.CancelButtonCssClass : Options.ExecuteButtonCssClass)}";

	/// <summary>
	/// Gets or sets content shown in a bar along the bottom of the studio, such as the cursor position.
	/// </summary>
	[Parameter] public RenderFragment? FooterContent { get; set; }

	/// <summary>
	/// Gets or sets the examples offered on the Examples menu; those for another language are left out.
	/// </summary>
	[Parameter] public IReadOnlyList<PDStudioExample> Examples { get; set; } = [];

	private const string _examplePrefix = "example-";

	private List<PDStudioExample> CurrentExamples => [.. Examples.Where(e => e.IsFor(Options.Language))];

	private static string ExampleKey(int index) => _examplePrefix + index.ToString(CultureInfo.InvariantCulture);

	// Built afresh each render: a child PDMenuItem registers with its dropdown only once, so would not follow a language change.
	private List<MenuItem> ExampleMenuItems()
		=> [.. CurrentExamples.Select((e, i) => new MenuItem { Text = e.Name, Key = ExampleKey(i) })];
}
