using BlazorMonaco.Editor;
using System.IO;

namespace PanoramicData.Blazor.Demo.Shared;

/// <summary>
/// Loading and displaying the demo's source files in the Source tab.
/// </summary>
public partial class DemoSourceView
{
	private const string _sourceBaseUrl = "https://raw.githubusercontent.com/panoramicdata/PanoramicData.Blazor/main/PanoramicData.Blazor.Demo";
	private const string _hostPageUrl = "https://raw.githubusercontent.com/panoramicdata/PanoramicData.Blazor/main/PanoramicData.Blazor.Web/Pages/_Host.cshtml";
	private readonly HttpClient _httpClient = new();
	private readonly Dictionary<string, SourceFile> _sourceFiles = [];
	private string _activeSourceFile = string.Empty;
	protected StandaloneCodeEditor? Editor { get; set; }

	private async Task LoadSourceFilesAsync()
	{
		var files = SourceFiles
			.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(sourceFile => sourceFile.Trim())
			.Select(path => new SourceFile { Name = path[(path.LastIndexOf('/') + 1)..], Url = GetUrl(path) })
			.ToList();
		_activeSourceFile = files.Count > 0 ? files[0].Name : string.Empty;

		// add _Host.cshtml to every page
		files.Add(new SourceFile { Name = "_Host.cshtml", Url = _hostPageUrl });
		foreach (var file in files)
		{
			_sourceFiles.Add(file.Name, file);
		}

		// load first source file
		if (_sourceFiles.TryGetValue(_activeSourceFile, out var entry))
		{
			try
			{
				entry.Content = await _httpClient.GetStringAsync(entry.Url).ConfigureAwait(true);
			}
			catch
			{
				// Nothing to do...
			}
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
		if (!_sourceFiles.TryGetValue(name, out SourceFile? sourceFile))
		{
			return;
		}

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

	private StandaloneEditorConstructionOptions EditorConstructionOptions() => new()
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
}
