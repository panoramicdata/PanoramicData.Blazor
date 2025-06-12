namespace PanoramicData.Blazor;
public partial class PDClickableImage
{
	[Parameter]
	public string ImageSource { get; set; } = string.Empty;

	[Parameter]
	public string Alt { get; set; } = string.Empty;

	[Parameter]
	public string Title { get; set; } = string.Empty;

	[Parameter]
	public string CssStyles { get; set; } = string.Empty;

	private bool _isFullScreen;

	private void OpenFullScreen() => _isFullScreen = true;

	private void CloseFullScreen() => _isFullScreen = false;
}

