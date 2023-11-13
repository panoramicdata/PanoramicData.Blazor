using PanoramicData.Blazor.PreviewProviders;

namespace PanoramicData.Blazor;

public partial class PDFilePreview
{
	private PreviewInfo _previewInfo = new();

	[Parameter]
	public FileExplorerItem? Item { get; set; }

	[Parameter]
	public IPreviewProvider PreviewProvider { get; set; } = new DefaultPreviewProvider();

	protected override async Task OnParametersSetAsync()
	{
		_previewInfo = await PreviewProvider.GetPreviewInfoAsync(Item);
	}
}
