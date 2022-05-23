namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The RenameArgs class provides arguments for a rename operation.
/// </summary>
public class RenameArgs
{
	/// <summary>
	/// Gets the file item to be renamed.
	/// </summary>
	public FileExplorerItem? Item { get; set; }

	/// <summary>
	/// Gets or sets whether the rename should be canceled.
	/// </summary>
	public bool Cancel { get; set; }
}
