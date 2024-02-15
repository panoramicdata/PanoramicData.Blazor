using PanoramicData.Blazor.PreviewProviders;

namespace PanoramicData.Blazor;

public partial class PDFilePreview
{
	private string? _lastPreviewPath;
	private PreviewInfo? _lastPreviewInfo;
	private PreviewInfo _previewInfo = new();

	[Parameter]
	public EventCallback<Exception> ExceptionHandler { get; set; }

	[Parameter]
	public FileExplorerItem? Item { get; set; }

	[Parameter]
	public IPreviewProvider PreviewProvider { get; set; } = new DefaultPreviewProvider();

	protected override async Task OnParametersSetAsync()
	{
		// cache last preview by path
		if (Item?.Path == _lastPreviewPath)
		{
			return;
		}
		_lastPreviewPath = Item?.Path;

		try
		{
			_previewInfo = await PreviewProvider.GetPreviewInfoAsync(Item);
		}
		catch (Exception ex)
		{
			_previewInfo = await PreviewProvider.GetBasicPreviewInfoAsync(Item);
			await ExceptionHandler.InvokeAsync(ex);
		}
	}
}
