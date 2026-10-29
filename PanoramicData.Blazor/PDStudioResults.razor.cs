namespace PanoramicData.Blazor;

/// <summary>
/// Component for displaying execution results in PDStudio.
/// </summary>
public partial class PDStudioResults : PDComponentBase
{
	private string _lastContent = string.Empty;

	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the HTML content to display in the results iframe.
	/// </summary>
	[Parameter] public string Content { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether execution is currently in progress.
	/// </summary>
	[Parameter] public bool IsExecuting { get; set; }

	/// <summary>
	/// Gets or sets the current execution status message.
	/// </summary>
	[Parameter] public string ExecutionStatus { get; set; } = "Ready";

	/// <summary>
	/// Gets or sets whether to show the status bar.
	/// </summary>
	[Parameter] public bool ShowStatusBar { get; set; } = true;

	/// <summary>
	/// Event callback when content changes.
	/// </summary>
	[Parameter] public EventCallback<string> ContentChanged { get; set; }

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Update iframe content if it changed
		if (_lastContent != Content && !string.IsNullOrWhiteSpace(Content))
		{
			_lastContent = Content;

			try
			{
				// Optionally scroll to bottom or handle specific iframe updates
				await Task.Delay(100); // Allow iframe to render
			}
			catch (Exception)
			{
				// Handle any iframe interaction issues gracefully
			}
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	/// <summary>
	/// Gets the CSS class for status bar styling based on execution status.
	/// </summary>
	/// <returns>CSS class names for status styling.</returns>
	private string GetStatusCssClass()
	{
		if (IsExecuting)
		{
			return "status-executing";
		}

		var status = ExecutionStatus.ToLowerInvariant();

		return status switch
		{
			var s when s.Contains("complete") => "status-complete",
			var s when s.Contains("ready") => "status-ready",
			var s when s.Contains("timed out") || s.Contains("timeout") => "status-timeout",
			var s when s.Contains("cancelled") => "status-cancelled",
			var s when s.Contains("invalid code") => "status-invalid",
			var s when s.Contains("runtime error") => "status-runtime-error",
			var s when s.Contains("error") => "status-error",
			_ => "status-unknown"
		};
	}

	/// <summary>
	/// Gets the appropriate icon for the current execution status. Only shown while not executing: a spinner
	/// takes its place during execution.
	/// </summary>
	/// <returns>Font Awesome icon class.</returns>
	private string GetStatusIcon()
	{
		var status = ExecutionStatus.ToLowerInvariant();

		return status switch
		{
			var s when s.Contains("complete") => "fas fa-check-circle",
			var s when s.Contains("ready") => "fas fa-circle",
			var s when s.Contains("timed out") || s.Contains("timeout") => "fas fa-clock",
			var s when s.Contains("cancelled") => "fas fa-stop-circle",
			var s when s.Contains("invalid code") => "fas fa-exclamation-triangle",
			var s when s.Contains("runtime error") => "fas fa-bug",
			var s when s.Contains("error") => "fas fa-times-circle",
			_ => "fas fa-question-circle"
		};
	}

	/// <summary>
	/// Gets or sets tabular results, shown in a grid in place of <see cref="Content"/>.
	/// </summary>
	[Parameter] public PDStudioResultSet? ResultSet { get; set; }

	private string Summary
	{
		get
		{
			var set = ResultSet!;
			var parts = new List<string>
			{
				set.Rows.Count == 1 ? "1 row" : string.Format(CultureInfo.InvariantCulture, "{0:N0} rows", set.Rows.Count)
			};
			if (set.Duration is TimeSpan duration)
			{
				parts.Add(duration.TotalSeconds < 1
					? string.Format(CultureInfo.InvariantCulture, "{0:N0} ms", duration.TotalMilliseconds)
					: string.Format(CultureInfo.InvariantCulture, "{0:N1} s", duration.TotalSeconds));
			}

			if (set.IsTruncated)
			{
				parts.Add(set.RowLimit is int limit
					? string.Format(CultureInfo.InvariantCulture, "limited to {0:N0} rows", limit)
					: "more rows not shown");
			}

			return string.Join(", ", parts);
		}
	}

	/// <summary>
	/// Gets or sets whether the results are shown in dark colours. Ignored when <see cref="StyleSheet"/> is set.
	/// </summary>
	[Parameter] public bool IsDark { get; set; }

	/// <summary>
	/// Gets or sets the CSS for the results document, replacing the built-in light and dark styles.
	/// The results are shown in a sandboxed frame, so the host page's own styles never reach them.
	/// </summary>
	[Parameter] public string? StyleSheet { get; set; }

	private const string _colours = """
		body { --text: #212529; --border: #dee2e6; --head-bg: #f1f3f5; --code-bg: #f8f9fa;
			--error: #842029; --error-bg: #f8d7da; --success: #0f5132; --success-bg: #d1e7dd; --info: #055160; --info-bg: #cff4fc; }
		""";

	private const string _darkColours = """
		body { --text: #dee2e6; --border: #3a3f4b; --head-bg: #2b303b; --code-bg: #1f232b;
			--error: #ffb4ab; --error-bg: #3a1d20; --success: #9be3b5; --success-bg: #16301f; --info: #9ad7ff; --info-bg: #14283a; }
		""";

	private const string _layout = """
		body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 1rem; line-height: 1.5; color: var(--text); background: transparent; }
		table { border-collapse: collapse; width: 100%; margin: 0 0 0.75rem; }
		th, td { border: 1px solid var(--border); padding: 8px; text-align: left; }
		th { background-color: var(--head-bg); font-weight: bold; }
		pre { background-color: var(--code-bg); border: 1px solid var(--border); border-radius: 0.25rem; padding: 1rem; overflow-x: auto; white-space: pre-wrap; word-wrap: break-word; }
		.error, .success, .info { border-radius: 0.25rem; padding: 0.75rem; margin: 0 0 0.75rem; user-select: none; }
		body > :last-child { margin-bottom: 0; }
		.error { color: var(--error); background-color: var(--error-bg); }
		.success { color: var(--success); background-color: var(--success-bg); }
		.info { color: var(--info); background-color: var(--info-bg); }
		""";

	// Without a matching color-scheme the browser paints the frame white inside a dark page.
	private string ColourScheme => StyleSheet is null && IsDark ? "dark" : "light";

	private string EffectiveStyleSheet => StyleSheet ?? (IsDark ? _darkColours : _colours) + _layout;

	/// <summary>
	/// Gets safe HTML content for iframe display. Only rendered when there is content to show.
	/// </summary>
	/// <returns>Safe HTML content wrapped in a basic HTML document.</returns>
	private string GetSafeHtmlContent() => $$"""

		<!DOCTYPE html>
		<html>
		<head>
		    <meta charset="utf-8">
		    <meta name="viewport" content="width=device-width, initial-scale=1">
		    <meta name="color-scheme" content="{{ColourScheme}}">
		    <title>Results</title>
		    <style>
		{{EffectiveStyleSheet}}
		    </style>
		</head>
		<body>
		{{Content}}
		</body>
		</html>
		""";

	/// <summary>
	/// Clears the current results content.
	/// </summary>
	public async Task ClearResults()
	{
		Content = string.Empty;
		_lastContent = string.Empty;

		if (ContentChanged.HasDelegate)
		{
			await ContentChanged.InvokeAsync(Content);
		}

		StateHasChanged();
	}

	/// <summary>
	/// Updates the results content.
	/// </summary>
	/// <param name="htmlContent">The new HTML content to display.</param>
	public async Task UpdateResults(string htmlContent)
	{
		Content = htmlContent ?? string.Empty;

		if (ContentChanged.HasDelegate)
		{
			await ContentChanged.InvokeAsync(Content);
		}

		StateHasChanged();
	}
}