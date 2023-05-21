namespace PanoramicData.Blazor;

public partial class PDGlobalListener : IAsyncDisposable
{
	private DotNetObjectReference<PDGlobalListener>? _dotNetObjectReference;
	private IJSObjectReference? _module;

	[Parameter] public RenderFragment? ChildContent { get; set; }

	[Inject] private IGlobalEventService GlobalEventService { get; set; } = null!;

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	public async ValueTask DisposeAsync()
	{
		try
		{
			GlobalEventService.ShortcutsChanged -= GlobalEventService_ShortcutsChanged;
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				await _module!.InvokeVoidAsync("dispose").ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}

	protected override async Task OnInitializedAsync()
	{
		_dotNetObjectReference = DotNetObjectReference.Create<PDGlobalListener>(this);
		_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDGlobalListener.razor.js").ConfigureAwait(true);
		if (_module != null)
		{
			await _module.InvokeVoidAsync("initialize", _dotNetObjectReference).ConfigureAwait(true);
		}

		GlobalEventService.ShortcutsChanged += GlobalEventService_ShortcutsChanged;
	}

	private void GlobalEventService_ShortcutsChanged(object? sender, IEnumerable<ShortcutKey> shortcuts)
	{
		if (_module != null)
		{
			_module.InvokeVoidAsync("registerShortcutKeys", shortcuts);
		}
	}

	[JSInvokable]
	public void OnKeyDown(KeyboardInfo keyboardInfo)
	{
		GlobalEventService?.KeyDown(keyboardInfo);
	}

	[JSInvokable]
	public void OnKeyUp(KeyboardInfo keyboardInfo)
	{
		GlobalEventService?.KeyUp(keyboardInfo);
	}
}
