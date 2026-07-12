namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that displays a clickable image that can be expanded to full screen.
/// </summary>
public partial class PDClickableImage
{
	/// <summary>
	/// Gets or sets the source URL of the image.
	/// </summary>
	[Parameter]
	public string ImageSource { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the alternate text for the image.
	/// </summary>
	[Parameter]
	public string Alt { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the title of the image.
	/// </summary>
	[Parameter]
	public string Title { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the CSS styles for the image.
	/// </summary>
	[Parameter]
	public string CssStyles { get; set; } = string.Empty;

	private bool _isFullScreen;

	private void OpenFullScreen() => _isFullScreen = true;

	private void CloseFullScreen() => _isFullScreen = false;
}

