namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDButtonPage : IAsyncDisposable
{
	private bool _operationRunning = false;
	private bool _buttonEnabled = true;
	private IJSObjectReference? _commonModule;
	private readonly ShortcutKey _shortcut1 = ShortcutKey.Create("Shift-Ctrl-Digit1");
	private readonly ShortcutKey _shortcut2 = ShortcutKey.Create("Shift-Ctrl-Digit2");
	private readonly ShortcutKey _shortcut3 = ShortcutKey.Create("Shift-Ctrl-Digit3");
	private int Counter { get; set; }

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	[Inject] private NavigationManager NavigationManager { get; set; } = null!;

	private async Task DoLongRunningOperation(MouseEventArgs args)
	{
		EventManager?.Add(new Event("Long running operation started"));

		await Task.Delay(5000);

		EventManager?.Add(new Event("Long running operation stopped"));
	}

	protected override async Task OnInitializedAsync() => _commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js").ConfigureAwait(true);

	private void OnButton1Click(MouseEventArgs args)
	{
		EventManager?.Add(new Event("Button 1 Click", new EventArgument("Ctrl", args.CtrlKey)));
		Counter++;
	}

	private async Task OnButton3Click(MouseEventArgs args)
	{
		EventManager?.Add(new Event("Button 3 Click", new EventArgument("Ctrl", args.CtrlKey)));

		// subtle navigation difference: The PDLinkButton will instruct the browser to navigate to the
		// given URL this will cause a trip to the server and reload the entire page whereas the below
		// code will use the NavigationManager to navigate and therefore no trip to the server occurs
		// unless the Ctrl key is held in which case a new tab will open and a trip to the server is
		// required.
		if (args.CtrlKey && _commonModule != null)
		{
			// open target in new tab / browser
			await _commonModule.InvokeVoidAsync("openUrl", "/", "_blank").ConfigureAwait(true);
		}
		else
		{
			// simply navigate to page
			NavigationManager.NavigateTo("/");
		}
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}
}