namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration options for PDStudio component.
/// </summary>
public class PDStudioOptions
{
	/// <summary>
	/// Gets or sets the programming language for the Monaco Editor.
	/// </summary>
	public string Language { get; set; } = "html";

	/// <summary>
	/// Gets or sets the theme for the Monaco Editor.
	/// </summary>
	public string Theme { get; set; } = "light";

	/// <summary>
	/// Gets or sets the splitter sizes: the editor above, the Results and Messages tabs below.
	/// </summary>
	public double[] MainSplitSizes { get; set; } = [75.0, 25.0];

	/// <summary>
	/// No longer used: results are shown on a tab below the editor.
	/// </summary>
	public double[] TopSplitSizes { get; set; } = [50.0, 50.0];

	/// <summary>
	/// Gets or sets whether the Messages tab is shown. Messages are kept while it is hidden.
	/// </summary>
	public bool IsLoggingVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets the level the Messages tab starts filtered to; Debug and below show everything.
	/// </summary>
	public LogLevel DefaultLogLevel { get; set; } = LogLevel.Debug;

	/// <summary>
	/// Gets or sets whether to show the menu toolbar.
	/// </summary>
	public bool ShowMenu { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the execution toolbar.
	/// </summary>
	public bool ShowToolbar { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the status bar in results panel.
	/// </summary>
	public bool ShowStatusBar { get; set; } = true;

	/// <summary>
	/// Gets or sets whether editing is enabled during code execution.
	/// When false, a spinner overlay blocks interaction with Monaco Editor.
	/// </summary>
	public bool IsEditingEnabledDuringExecution { get; set; } = true;

	/// <summary>
	/// Gets or sets the execution timeout in seconds. Default is 30 seconds.
	/// Set to 0 or negative value to disable timeout.
	/// </summary>
	public int ExecutionTimeoutSeconds { get; set; } = 30;

	/// <summary>
	/// Gets or sets the size of the toolbar's buttons.
	/// </summary>
	public ButtonSizes ButtonSize { get; set; } = ButtonSizes.Small;

	/// <summary>
	/// Gets or sets the CSS class on each toolbar button's text, which sets the gap after its icon.
	/// </summary>
	public string ButtonTextCssClass { get; set; } = "ps-2";

	/// <summary>
	/// Gets or sets the Execute button's text.
	/// </summary>
	public string ExecuteButtonText { get; set; } = "Execute";

	/// <summary>
	/// Gets or sets the Execute button's CSS class.
	/// </summary>
	public string ExecuteButtonCssClass { get; set; } = "btn-success";

	/// <summary>
	/// Gets or sets Cancel's text, which the Execute button shows while code runs, or the Cancel button when
	/// <see cref="ShowCancelButton"/> is set.
	/// </summary>
	public string CancelButtonText { get; set; } = "Cancel";

	/// <summary>
	/// Gets or sets Cancel's CSS class, as for <see cref="CancelButtonText"/>.
	/// </summary>
	public string CancelButtonCssClass { get; set; } = "btn-danger";

	/// <summary>
	/// Gets or sets whether Cancel is a button of its own, after Execute and enabled only while code runs, so the
	/// toolbar keeps its shape; false makes Execute turn into Cancel while code runs.
	/// </summary>
	public bool ShowCancelButton { get; set; }

	/// <summary>
	/// Gets or sets the height in pixels of the bar between the editor and the output, which is dragged to resize them.
	/// </summary>
	public int GutterSize { get; set; } = 4;

	/// <summary>
	/// Gets or sets an icon shown before the Results tab's title, such as "fas fa-table"; null for none.
	/// </summary>
	public string? ResultsTabIconCssClass { get; set; }

	/// <summary>
	/// Gets or sets an icon shown before the Messages tab's title, such as "fas fa-list"; null for none.
	/// </summary>
	public string? MessagesTabIconCssClass { get; set; }

	/// <summary>
	/// Gets or sets whether the New button is shown, for example false when the host has its own File menu.
	/// </summary>
	public bool ShowNewButton { get; set; } = true;

	/// <summary>
	/// Gets or sets the New button's text.
	/// </summary>
	public string NewButtonText { get; set; } = "New";

	/// <summary>
	/// Gets or sets the New button's CSS class.
	/// </summary>
	public string NewButtonCssClass { get; set; } = "btn-secondary";

	/// <summary>
	/// Gets or sets the Examples menu's text.
	/// </summary>
	public string ExamplesButtonText { get; set; } = "Examples";

	/// <summary>
	/// Gets or sets the Examples menu's CSS class.
	/// </summary>
	public string ExamplesButtonCssClass { get; set; } = "btn-secondary";

	/// <summary>
	/// Gets or sets the CSS for the results document, replacing the built-in styles, which are dark when
	/// <see cref="Theme"/> contains "dark". The results are shown in a sandboxed frame the page's styles never reach.
	/// </summary>
	public string? ResultsStyleSheet { get; set; }

	/// <summary>Gets or sets the Execute button's icon.</summary>
	public string ExecuteButtonIconCssClass { get; set; } = "fas fa-play";

	/// <summary>Gets or sets the Execute button's tooltip; null uses its text. The shortcut is added either way.</summary>
	public string? ExecuteButtonToolTip { get; set; }

	/// <summary>Gets or sets Cancel's icon, as for <see cref="CancelButtonText"/>.</summary>
	public string CancelButtonIconCssClass { get; set; } = "fas fa-stop";

	/// <summary>Gets or sets the New button's icon.</summary>
	public string NewButtonIconCssClass { get; set; } = "fas fa-file";

	/// <summary>Gets or sets the Examples menu's icon.</summary>
	public string ExamplesButtonIconCssClass { get; set; } = "fas fa-book-open";

	/// <summary>Gets or sets the Results tab's title.</summary>
	public string ResultsTabTitle { get; set; } = "Results";

	/// <summary>Gets or sets the Results tab's tooltip; null for none.</summary>
	public string? ResultsTabToolTip { get; set; }

	/// <summary>Gets or sets the Messages tab's title.</summary>
	public string MessagesTabTitle { get; set; } = "Messages";

	/// <summary>Gets or sets the Messages tab's tooltip; null for none.</summary>
	public string? MessagesTabToolTip { get; set; }

	/// <summary>Gets or sets the text shown in the results before anything has run.</summary>
	public string ResultsPlaceholderText { get; set; } = "Execute code to see results here";

	/// <summary>Gets or sets how many messages are kept; the oldest are dropped first.</summary>
	public int MaxMessages { get; set; } = 1000;

	/// <summary>
	/// Gets or sets custom properties for extensibility.
	/// </summary>
	public Dictionary<string, object> CustomProperties { get; set; } = [];
}