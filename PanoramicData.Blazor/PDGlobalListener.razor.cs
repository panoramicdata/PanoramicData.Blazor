namespace PanoramicData.Blazor;

public partial class PDGlobalListener : IAsyncDisposable
{
	private DotNetObjectReference<PDGlobalListener>? _dotNetObjectReference;
	private IJSObjectReference? _module;

	/// <summary>
	/// Gets or sets the child content of the component.
	/// </summary>
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

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_dotNetObjectReference = DotNetObjectReference.Create(this);
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDGlobalListener.razor.js").ConfigureAwait(true);
				if (_module != null)
				{
					await _module.InvokeVoidAsync("initialize", _dotNetObjectReference).ConfigureAwait(true);
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}

		GlobalEventService.ShortcutsChanged += GlobalEventService_ShortcutsChanged;
	}

	private void GlobalEventService_ShortcutsChanged(object? sender, IEnumerable<ShortcutKey> shortcuts)
	{
		try
		{
			_module?.InvokeVoidAsync("registerShortcutKeys", shortcuts);
		}
		catch
		{
			// Nothing to do
		}
	}

	[JSInvokable]
	public void OnKeyDown(KeyboardInfo keyboardInfo) => GlobalEventService?.KeyDown(keyboardInfo);

	[JSInvokable]
	public void OnKeyUp(KeyboardInfo keyboardInfo) => GlobalEventService?.KeyUp(keyboardInfo);
}