namespace PanoramicData.Blazor;

/// <summary>
/// Component for displaying execution results in PDStudio.
/// </summary>
public partial class PDStudioResults : PDComponentBase
{
	private ElementReference ResultsIframe;
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
	/// Gets the appropriate icon for the current execution status.
	/// </summary>
	/// <returns>Font Awesome icon class.</returns>
	private string GetStatusIcon()
	{
		if (IsExecuting)
		{
			return "fas fa-spinner fa-spin";
		}

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
	/// Gets safe HTML content for iframe display.
	/// </summary>
	/// <returns>Safe HTML content wrapped in a basic HTML document.</returns>
	private string GetSafeHtmlContent()
	{
		if (string.IsNullOrWhiteSpace(Content))
		{
			return string.Empty;
		}

		// Wrap content in a basic HTML document structure
		return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Results</title>
    <style>
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            margin: 0;
            padding: 1rem;
            line-height: 1.5;
            color: #333;
        }}
        table {{ 
            border-collapse: collapse; 
            width: 100%; 
            margin: 1rem 0;
        }}
        th, td {{ 
            border: 1px solid #ddd; 
            padding: 8px; 
            text-align: left; 
        }}
        th {{ 
            background-color: #f5f5f5; 
            font-weight: bold; 
        }}
        pre {{ 
            background-color: #f8f9fa;
            border: 1px solid #e9ecef;
            border-radius: 0.25rem;
            padding: 1rem;
            overflow-x: auto;
            white-space: pre-wrap;
            word-wrap: break-word;
        }}
        .error {{ 
            color: #dc3545; 
            background-color: #f8d7da;
            border: 1px solid #f5c6cb;
            border-radius: 0.25rem;
            padding: 0.75rem;
            margin: 1rem 0;
        }}
        .success {{ 
            color: #155724; 
            background-color: #d4edda;
            border: 1px solid #c3e6cb;
            border-radius: 0.25rem;
            padding: 0.75rem;
            margin: 1rem 0;
        }}
        .info {{ 
            color: #0c5460; 
            background-color: #d1ecf1;
            border: 1px solid #bee5eb;
            border-radius: 0.25rem;
            padding: 0.75rem;
            margin: 1rem 0;
        }}
    </style>
</head>
<body>
{Content}
</body>
</html>";
	}

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