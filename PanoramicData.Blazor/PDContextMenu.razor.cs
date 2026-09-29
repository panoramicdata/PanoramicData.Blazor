namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that displays a context menu when the user right-clicks on the wrapped content.
/// </summary>
public partial class PDContextMenu : IAsyncDisposable
{
	private static int _idSequence;
	private IJSObjectReference? _module;
	private IJSObjectReference? _commonModule;
	private bool _popperMissing;

	/// <summary>
	/// Gets the injected JavaScript runtime.
	/// </summary>
	[Inject] public IJSRuntime? JSRuntime { get; set; }

	/// <summary>
	/// Gets or sets the menu items to be displayed in the context menu.
	/// </summary>
	[Parameter] public List<MenuItem> Items { get; set; } = [];

	/// <summary>
	/// Gets or sets the child content that the COntextMenu wraps.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets an event that is raised just prior to the context menu being shown and allowing
	/// the application to refresh the state of the items.
	/// </summary>
	[Parameter] public EventCallback<MenuItemsEventArgs> UpdateState { get; set; }

	/// <summary>
	/// Gets or sets an event callback delegate fired when the user selects clicks one of the items.
	/// </summary>
	[Parameter] public EventCallback<MenuItem> ItemClick { get; set; }

	/// <summary>
	/// Sets whether the context menu is enabled or disabled.
	/// </summary>
	[Parameter] public bool Enabled { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the menu is displayed on the mouse up event instead of the default mouse down event.
	/// </summary>
	[Parameter] public bool ShowOnMouseUp { get; set; }

	/// <summary>
	/// Gets the unique identifier of this panel.
	/// </summary>
	/// <remarks>
	/// Assigned when the component is created, so the menu element carries it from the first render and the
	/// first right-click can find it.
	/// </remarks>
	public string Id { get; private set; } = $"pdcm{Interlocked.Increment(ref _idSequence)}";

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
	/// Handles a click on a context menu item, hiding the menu and invoking the <see cref="ItemClick"/> callback.
	/// </summary>
	public async Task ClickHandler(MenuItem item)
	{
		if (!item.IsDisabled)
		{
			if (_module != null)
			{
				await _module.InvokeVoidAsync("hideMenu", Id).ConfigureAwait(true);
			}

			await ItemClick.InvokeAsync(item).ConfigureAwait(true);
		}
	}

	private Task OnMouseDownAsync(MouseEventArgs args)
	{
		if (Enabled && args.Button == 2 && !ShowOnMouseUp)
		{
			return ShowMenuAsync(args);
		}

		return Task.CompletedTask;
	}

	private Task OnMouseUpAsync(MouseEventArgs args)
	{
		if (Enabled && args.Button == 2 && ShowOnMouseUp)
		{
			return ShowMenuAsync(args);
		}

		return Task.CompletedTask;
	}

	private async Task ShowMenuAsync(MouseEventArgs args)
	{
		var cancelArgs = new MenuItemsEventArgs(this, Items)
		{
			// get details of element that was clicked on
			SourceElement = _commonModule != null ? (await _commonModule.InvokeAsync<ElementInfo>("getElementAtPoint", args.ClientX, args.ClientY).ConfigureAwait(true)) : null
		};

		await UpdateState.InvokeAsync(cancelArgs).ConfigureAwait(true);
		if (!cancelArgs.Cancel && _module != null && await IsPopperAvailableAsync(_module).ConfigureAwait(true))
		{
			await _module.InvokeVoidAsync("showMenu", Id, args.ClientX, args.ClientY).ConfigureAwait(true);
		}
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
