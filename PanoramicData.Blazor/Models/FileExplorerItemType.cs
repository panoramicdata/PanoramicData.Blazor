namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies whether a <see cref="FileExplorerItem"/> represents a directory or a file.
/// </summary>
public enum FileExplorerItemType
{
	/// <summary>The item is a directory (folder).</summary>
	Directory,
	/// <summary>The item is a file.</summary>
	File
}
