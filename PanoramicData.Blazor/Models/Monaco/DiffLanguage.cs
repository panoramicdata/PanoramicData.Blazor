namespace PanoramicData.Blazor.Models.Monaco;

/// <summary>
/// Maps a file name to the Monaco language id used to highlight it.
/// </summary>
public static class DiffLanguage
{
	private static readonly Dictionary<string, string> _byExtension = new(StringComparer.OrdinalIgnoreCase)
	{
		[".cs"] = "csharp",
		[".razor"] = "razor",
		[".cshtml"] = "razor",
		[".json"] = "json",
		[".yml"] = "yaml",
		[".yaml"] = "yaml",
		[".csproj"] = "xml",
		[".props"] = "xml",
		[".targets"] = "xml",
		[".slnx"] = "xml",
		[".xml"] = "xml",
		[".md"] = "markdown",
		[".ps1"] = "powershell",
		[".css"] = "css",
		[".html"] = "html",
		[".js"] = "javascript",
		[".ts"] = "typescript",
		[".sql"] = "sql",
		[".sh"] = "shell"
	};

	/// <summary>
	/// Returns the Monaco language id for <paramref name="fileName"/>, or <c>plaintext</c> when unknown.
	/// </summary>
	/// <param name="fileName">A file name or path, or null.</param>
	public static string FromFileName(string? fileName)
		=> _byExtension.TryGetValue(Path.GetExtension(fileName ?? string.Empty), out var language)
			? language
			: "plaintext";
}
