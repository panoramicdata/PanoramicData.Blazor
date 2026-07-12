using PanoramicData.Blazor.PreviewProviders;

namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that displays a preview of a file selected in a file explorer.
/// </summary>
public partial class PDFilePreview
{
	private string? _lastPreviewPath;
	private PreviewInfo _previewInfo = new();

	/// <summary>
	/// An event callback that is invoked when an exception occurs.
	/// </summary>
	[Parameter]
	public EventCallback<Exception> ExceptionHandler { get; set; }

	/// <summary>
	/// Gets or sets the file item to be previewed.
	/// </summary>
	[Parameter]
	public FileExplorerItem? Item { get; set; }

	/// <summary>
	/// Gets or sets the preview provider for the file.
	/// </summary>
	[Parameter]
	public IPreviewProvider PreviewProvider { get; set; } = new DefaultPreviewProvider();

	/// <inheritdoc />
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
			// only show basic info with spinner is download takes more than N ms
			var minTimeTask = Task.Delay(PreviewProvider.SpinnerTriggerMs);
			var previewTask = PreviewProvider.GetPreviewInfoAsync(Item);
			var firstToComplete = await Task.WhenAny(minTimeTask, previewTask);
			if (firstToComplete == minTimeTask)
			{
				_previewInfo = await PreviewProvider.GetBasicPreviewInfoAsync(Item, true);
				var delayTask = Task.Delay(PreviewProvider.SpinnerMinDisplayMs);
				StateHasChanged();
				await Task.WhenAll(delayTask, previewTask);
			}

			_previewInfo = await previewTask;
		}
		catch (Exception ex)
		{
			_previewInfo = await PreviewProvider.GetBasicPreviewInfoAsync(Item);
			await ExceptionHandler.InvokeAsync(ex);
		}
	}
}
