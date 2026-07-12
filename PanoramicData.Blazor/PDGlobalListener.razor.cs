namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that listens for global keyboard events and forwards them to the <see cref="IGlobalEventService"/>.
/// </summary>
public partial class PDGlobalListener : IAsyncDisposable
{
	private DotNetObjectReference<PDGlobalListener>? _dotNetObjectReference;
	private IJSObjectReference? _module;

	/// <summary>
	/// Gets or sets the child content of the component.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	[Inject] private IGlobalEventService GlobalEventService { get; set; } = null!;

	/// <summary>
	/// Gets or sets the injected JavaScript runtime.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <inheritdoc />
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

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		GlobalEventService.ShortcutsChanged += GlobalEventService_ShortcutsChanged;
	}

	/// <inheritdoc />
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

					// Push any already-registered shortcuts to JS
					var existingShortcuts = GlobalEventService.GetRegisteredShortcuts();
					if (existingShortcuts.Any())
					{
						await _module.InvokeVoidAsync("registerShortcutKeys", existingShortcuts).ConfigureAwait(true);
					}
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	private async void GlobalEventService_ShortcutsChanged(object? sender, IEnumerable<ShortcutKey> shortcuts)
	{
		try
		{
			if (_module is not null)
			{
				await _module.InvokeVoidAsync("registerShortcutKeys", shortcuts).ConfigureAwait(true);
			}
		}
		catch
		{
			// Nothing to do
		}
	}

	/// <summary>
	/// Invoked by JavaScript when a key-down event occurs.
	/// </summary>
	/// <param name="keyboardInfo">Information about the key that was pressed.</param>
	[JSInvokable]
	public void OnKeyDown(KeyboardInfo keyboardInfo) => GlobalEventService?.KeyDown(keyboardInfo);

	/// <summary>
	/// Invoked by JavaScript when a key-up event occurs.
	/// </summary>
	/// <param name="keyboardInfo">Information about the key that was released.</param>
	[JSInvokable]
	public void OnKeyUp(KeyboardInfo keyboardInfo) => GlobalEventService?.KeyUp(keyboardInfo);
}