namespace PanoramicData.Blazor;

/// <summary>
/// PDContextMenu: loading and releasing the JavaScript modules, and the popper.js dependency they need.
/// </summary>
public partial class PDContextMenu : IAsyncDisposable
{
	private IJSObjectReference? _module;
	private IJSObjectReference? _commonModule;
	private bool _popperMissing;

	/// <summary>
	/// Gets or sets the logger to which a missing popper.js dependency is reported.
	/// </summary>
	[Inject] private ILogger<PDContextMenu> Logger { get; set; } = null!;

	/// <inheritdoc />
	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			var popperAvailable = await InitializeModulesAsync(JSRuntime).ConfigureAwait(true);
			if (popperAvailable == false)
			{
				ReportMissingPopper();
			}
		}
	}

	/// <summary>
	/// Imports the JavaScript modules and asks whether popper.js is loaded.
	/// </summary>
	/// <returns>Whether popper.js is loaded, or null when the JavaScript side could not be reached.</returns>
	private async Task<bool?> InitializeModulesAsync(IJSRuntime jsRuntime)
	{
		try
		{
			_module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDContextMenu.razor.js").ConfigureAwait(true);
			_commonModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl).ConfigureAwait(true);
			return await _module.InvokeAsync<bool>("hasPopperJs").ConfigureAwait(true);
		}
		catch
		{
			// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			return null;
		}
	}

	/// <summary>
	/// Reports that popper.js is missing. The error is logged rather than thrown, because throwing from
	/// OnAfterRenderAsync would take down a page that never opens the menu; the menu is not shown instead.
	/// </summary>
	private void ReportMissingPopper()
	{
		_popperMissing = true;
		var exception = new PDContextMenuException($"To use the {nameof(PDContextMenu)} component you must include the popper.js library");
		Logger.LogError(exception, "{Message} The context menu {ContextMenuId} will not be shown.", exception.Message, Id);
	}

	/// <summary>
	/// Whether popper.js can be used to show the menu. Without it the script would throw on every right-click.
	/// When it was missing at start-up it is looked for again, in case the page loaded it later.
	/// </summary>
	private async Task<bool> IsPopperAvailableAsync(IJSObjectReference module)
	{
		if (_popperMissing && await module.InvokeAsync<bool>("hasPopperJs").ConfigureAwait(true))
		{
			_popperMissing = false;
		}

		return !_popperMissing;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				await _module.InvokeVoidAsync("hideMenu", Id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
			// Too bad...
		}
	}
}
