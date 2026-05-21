namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDModalPage
{
	private PDModal _basicModal = null!;
	private PDModal _smallModal = null!;
	private PDModal _mediumModal = null!;
	private PDModal _largeModal = null!;
	private PDModal _xlModal = null!;
	private PDModal _centeredModal = null!;
	private PDModal _closeButtonModal = null!;
	private PDModal _noFooterModal = null!;
	private PDModal _customButtonsModal = null!;
	private PDModal _awaitModal = null!;
	private PDModal _noEscapeModal = null!;

	private string? _customButtonResult;
	private string? _awaitedResult;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private async Task OnCustomButtonClick(string key)
	{
		_customButtonResult = key;
		await _customButtonsModal.HideAsync();
		EventManager?.Add(new Event("ButtonClick", new EventArgument("Key", key)));
	}

	private async Task OnConfirmClick()
	{
		_awaitedResult = null;
		var result = await _awaitModal.ShowAndWaitResultAsync();
		_awaitedResult = result;
		EventManager?.Add(new Event("AwaitResult", new EventArgument("Key", result)));
	}

	// kept for demo source compatibility
	private Task OnClick(MouseEventArgs e) => _basicModal.ShowAsync();
	private Task CloseModal(MouseEventArgs e) => _basicModal.HideAsync();
}
