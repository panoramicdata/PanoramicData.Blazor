namespace PanoramicData.Blazor.Models;

/// <summary>
/// The DownloadResponse class is used to return the results of a download.
/// </summary>
public class DownloadResponse
{
	/// <summary>
	/// Gets or sets whether the download was successful or not.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets or sets a short description of any error encountered.
	/// </summary>
	public string ErrorMessage { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the suggested name of file.
	/// </summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the file contents.
	/// </summary>
	public byte[] Content { get; set; } = new byte[0];
}
