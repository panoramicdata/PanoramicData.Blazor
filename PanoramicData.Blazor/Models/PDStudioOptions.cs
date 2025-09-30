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
	/// Gets or sets the splitter sizes for main split (Top panel vs Log panel).
	/// </summary>
	public double[] MainSplitSizes { get; set; } = new[] { 75.0, 25.0 };

	/// <summary>
	/// Gets or sets the splitter sizes for top split (Editor vs Results).
	/// </summary>
	public double[] TopSplitSizes { get; set; } = new[] { 50.0, 50.0 };

	/// <summary>
	/// Gets or sets whether the logging panel is visible at initialization.
	/// </summary>
	public bool IsLoggingVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets the default log level for the PDLog component.
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
	/// Gets or sets custom properties for extensibility.
	/// </summary>
	public Dictionary<string, object> CustomProperties { get; set; } = new();
}