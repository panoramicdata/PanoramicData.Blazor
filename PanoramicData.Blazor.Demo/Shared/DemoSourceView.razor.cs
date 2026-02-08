using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components.Routing;
using System.IO;

namespace PanoramicData.Blazor.Demo.Shared;

public partial class DemoSourceView : IDisposable
{
	private string ActiveTab { get; set; } = "Demo";
	private const string _sourceBaseUrl = "https://raw.githubusercontent.com/panoramicdata/PanoramicData.Blazor/main/PanoramicData.Blazor.Demo";
	private readonly HttpClient _httpClient = new();
	private readonly Dictionary<string, SourceFile> _sourceFiles = [];
	private string _activeSourceFile = string.Empty;
	private StandaloneCodeEditor? Editor { get; set; }

	[Inject] private INavigationCancelService NavigationCancelService { get; set; } = default!;

	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

	[Inject] private NavigationManager NavigationManager { get; set; } = default!;

	/// <summary>
	/// Sets the child content that the drop zone wraps.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Optional documentation content for the Documentation tab.
	/// </summary>
	[Parameter] public RenderFragment? DocumentationContent { get; set; }

	/// <summary>
	/// Event called prior to the user changing tabs.
	/// </summary>
	[Parameter] public EventCallback<CancelEventArgs> BeforeChangeTab { get; set; }

	/// <summary>
	/// Sets the source code pages used in the demo.
	/// </summary>
	[Parameter] public string SourceFiles { get; set; } = string.Empty;

	protected override void OnInitialized()
	{
		NavigationManager.LocationChanged += OnLocationChanged;
		ParseTabFromUrl();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			foreach (var sourceFile in SourceFiles.Split(',', StringSplitOptions.RemoveEmptyEntries))
			{
				var trimmedSourceFile = sourceFile.Trim();
				var name = trimmedSourceFile[(trimmedSourceFile.LastIndexOf('/') + 1)..];
				_sourceFiles.Add(name, new SourceFile
				{
					Name = name,
					Url = GetUrl(trimmedSourceFile)
				});
				if (string.IsNullOrWhiteSpace(_activeSourceFile))
				{
					_activeSourceFile = name;
				}
			}
			// add _Host.cshtml to every page
			_sourceFiles.Add("_Host.cshtml", new SourceFile
			{
				Name = "_Host.cshtml",
				Url = "https://raw.githubusercontent.com/panoramicdata/PanoramicData.Blazor/main/PanoramicData.Blazor.Web/Pages/_Host.cshtml"
			});
			// load first source file
			if (!string.IsNullOrWhiteSpace(_activeSourceFile))
			{
				var entry = _sourceFiles[_activeSourceFile];
				try
				{
					entry.Content = await _httpClient.GetStringAsync(entry.Url).ConfigureAwait(true);
				}
				catch
				{
				}
			}

			// Scroll to anchor if present
			await ScrollToAnchorAsync().ConfigureAwait(true);
		}
	}

	private void ParseTabFromUrl()
	{
		var uri = new Uri(NavigationManager.Uri);
		var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
		var tab = query["tab"];

		if (!string.IsNullOrEmpty(tab))
		{
			ActiveTab = tab switch
			{
				"demo" => "Demo",
				"source" => "Source",
				"docs" or "documentation" => DocumentationContent != null ? "Documentation" : "Demo",
				_ => "Demo"
			};
		}
	}

	private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
	{
		ParseTabFromUrl();
		StateHasChanged();
	}

	private async Task OnChangeTab(string tab)
	{
		if (await NavigationCancelService.ProceedAsync(tab).ConfigureAwait(true))
		{
			ActiveTab = tab;
			UpdateUrlWithTab(tab);
		}
	}

	private void UpdateUrlWithTab(string tab)
	{
		var uri = new Uri(NavigationManager.Uri);
		var baseUrl = uri.GetLeftPart(UriPartial.Path);
		var tabParam = tab.ToLowerInvariant() switch
		{
			"demo" => "demo",
			"source" => "source",
			"documentation" => "docs",
			_ => "demo"
		};

		// Preserve the fragment (anchor) if present
		var fragment = uri.Fragment;
		var newUrl = $"{baseUrl}?tab={tabParam}{fragment}";

		NavigationManager.NavigateTo(newUrl, replace: true);
	}

	private async Task ScrollToAnchorAsync()
	{
		var uri = new Uri(NavigationManager.Uri);
		if (!string.IsNullOrEmpty(uri.Fragment))
		{
			var anchor = uri.Fragment.TrimStart('#');
			await JSRuntime.InvokeVoidAsync("scrollToElement", anchor).ConfigureAwait(true);
		}
	}

	private string SourceCode =>
		_sourceFiles.TryGetValue(_activeSourceFile, out var value) ? value.Content : string.Empty;

	public static string GetUrl(string url) => url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : $"{_sourceBaseUrl}/{url}";

	private async Task<string> LoadSourceAsync(string url)
	{
		try
		{
			return await _httpClient.GetStringAsync(GetUrl(url)).ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			return $"Failed to load source: {ex.Message}";
		}
	}

	private async Task OnFileClick(string name)
	{
		if (_sourceFiles.TryGetValue(name, out SourceFile? sourceFile))
		{
			if (string.IsNullOrWhiteSpace(sourceFile.Content))
			{
				sourceFile.Content = await LoadSourceAsync(sourceFile.Url).ConfigureAwait(true);
			}

			var extnChanged = Path.GetExtension(name) != Path.GetExtension(_activeSourceFile);
			_activeSourceFile = name;

			await Editor!.SetValue(SourceCode).ConfigureAwait(true);

			if (extnChanged)
			{
				var model = await Editor.GetModel().ConfigureAwait(true);
				await Global.SetModelLanguage(JSRuntime, model, GetLanguageForFile(_activeSourceFile)).ConfigureAwait(true);
			}
		}
	}

	private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor _) => new()
	{
		AutomaticLayout = true,
		Language = GetLanguageForFile(_activeSourceFile),
		Value = SourceCode,
		ReadOnly = true
	};

	private static string GetLanguageForFile(string filename) => Path.GetExtension(filename) switch
	{
		".cs" => "csharp",
		".css" => "css",
		".html" => "html",
		".cshtml" => "razor",
		".razor" => "razor",
		_ => "csharp"
	};

	public void Dispose()
	{
		NavigationManager.LocationChanged -= OnLocationChanged;
		GC.SuppressFinalize(this);
	}
}
