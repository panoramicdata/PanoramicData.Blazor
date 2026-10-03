namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDModalPage
{
	protected PDModal BasicModal { get; set; } = null!;
	protected PDModal SmallModal { get; set; } = null!;
	protected PDModal MediumModal { get; set; } = null!;
	protected PDModal LargeModal { get; set; } = null!;
	protected PDModal XlModal { get; set; } = null!;
	protected PDModal CenteredModal { get; set; } = null!;
	protected PDModal CloseButtonModal { get; set; } = null!;
	protected PDModal NoFooterModal { get; set; } = null!;
	protected PDModal CustomButtonsModal { get; set; } = null!;
	protected PDModal AwaitModal { get; set; } = null!;
	protected PDModal NoEscapeModal { get; set; } = null!;

	private string? _customButtonResult;
	private string? _awaitedResult;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private async Task OnCustomButtonClick(string key)
	{
		_customButtonResult = key;
		await CustomButtonsModal.HideAsync();
		EventManager?.Add(new Event("ButtonClick", new EventArgument("Key", key)));
	}

	private async Task OnConfirmClick()
	{
		_awaitedResult = null;
		var result = await AwaitModal.ShowAndWaitResultAsync();
		_awaitedResult = result;
		EventManager?.Add(new Event("AwaitResult", new EventArgument("Key", result)));
	}

	// kept for demo source compatibility
	private Task OnClick() => BasicModal.ShowAsync();
	private Task CloseModal() => BasicModal.HideAsync();
}
