using BlazorMonaco.Editor;

namespace PanoramicData.Blazor.Demo.Shared;

/// <summary>
/// A documentation example component showing code and live demo side by side.
/// </summary>
public partial class DocExample
{
	private StandaloneCodeEditor? _editor;
	private bool _copied;

	[Inject]
	private IJSRuntime JSRuntime { get; set; } = default!;

	/// <summary>
	/// The title of the example.
	/// </summary>
	[Parameter, EditorRequired]
	public string Title { get; set; } = string.Empty;

	/// <summary>
	/// The anchor ID for deep linking. Auto-generated from title if not specified.
	/// </summary>
	[Parameter]
	public string? AnchorId { get; set; }

	/// <summary>
	/// The code to display in the Monaco editor.
	/// </summary>
	[Parameter, EditorRequired]
	public string Code { get; set; } = string.Empty;

	/// <summary>
	/// The language for syntax highlighting (razor, csharp, css, etc.).
	/// </summary>
	[Parameter]
	public string Language { get; set; } = "razor";

	/// <summary>
	/// The live demo content to render.
	/// </summary>
	[Parameter]
	public RenderFragment? DemoContent { get; set; }

	/// <summary>
	/// Optional description/explanation shown below the example.
	/// </summary>
	[Parameter]
	public RenderFragment? Description { get; set; }

	/// <summary>
	/// Additional CSS class for the demo container.
	/// </summary>
	[Parameter]
	public string DemoCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Inline style for the demo container.
	/// </summary>
	[Parameter]
	public string DemoStyle { get; set; } = string.Empty;

	private string ComputedAnchorId => AnchorId ?? GenerateAnchorId(Title);

	protected override void OnParametersSet()
	{
		AnchorId ??= GenerateAnchorId(Title);
	}

	private static string GenerateAnchorId(string title)
	{
		return title
			.ToLowerInvariant()
			.Replace(" ", "-")
			.Replace("&", "and")
			.Replace("(", "")
			.Replace(")", "")
			.Replace(",", "");
	}

	private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor _)
	{
		// Count lines to auto-size editor
		var lineCount = Code.Split('\n').Length;
		var minHeight = Math.Max(100, Math.Min(lineCount * 19 + 20, 400)); // 19px per line, max 400px

		return new()
		{
			AutomaticLayout = true,
			Language = GetMonacoLanguage(Language),
			Value = Code.Trim(),
			ReadOnly = true,
			Minimap = new EditorMinimapOptions { Enabled = false },
			ScrollBeyondLastLine = false,
			LineNumbers = "on",
			Folding = false,
			RenderLineHighlight = "none",
			OverviewRulerLanes = 0,
			HideCursorInOverviewRuler = true,
			Dimension = new Dimension { Height = minHeight }
		};
	}

	private static string GetMonacoLanguage(string language) => language.ToLowerInvariant() switch
	{
		"razor" => "razor",
		"csharp" or "cs" or "c#" => "csharp",
		"css" => "css",
		"html" => "html",
		"javascript" or "js" => "javascript",
		"json" => "json",
		_ => "plaintext"
	};

	private async Task CopyCode()
	{
		try
		{
			await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", Code.Trim()).ConfigureAwait(true);
			_copied = true;
			StateHasChanged();

			// Reset after 2 seconds
			await Task.Delay(2000).ConfigureAwait(true);
			_copied = false;
			StateHasChanged();
		}
		catch
		{
			// Clipboard API may not be available
		}
	}
}
