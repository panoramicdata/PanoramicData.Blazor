namespace PanoramicData.Blazor;

public partial class PDContextMenu : IAsyncDisposable
{
	private static int _idSequence;
	private IJSObjectReference? _module;
	private IJSObjectReference? _commonModule;

	[Inject] public IJSRuntime? JSRuntime { get; set; }

	/// <summary>
	/// Gets or sets the menu items to be displayed in the context menu.
	/// </summary>
	[Parameter] public List<MenuItem> Items { get; set; } = new List<MenuItem>();

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
	/// Gets the unique identifier of this panel.
	/// </summary>
	public string Id { get; private set; } = string.Empty;

	protected async override Task OnInitializedAsync()
	{
		Id = $"pdcm{++_idSequence}";
		if (JSRuntime != null)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDContextMenu.razor.js");
			_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js");
			var available = await _module.InvokeAsync<bool>("hasPopperJs").ConfigureAwait(true);
			if (!available)
			{
				throw new PDContextMenuException($"To use the {nameof(PDContextMenu)} component you must include the popper.js library");
			}
		}
	}

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

	private async Task OnMouseDown(MouseEventArgs args)
	{
		if (Enabled && args.Button == 2)
		{
			var cancelArgs = new MenuItemsEventArgs(this, Items)
			{
				// get details of element that was clicked on
				SourceElement = _commonModule != null ? (await _commonModule.InvokeAsync<ElementInfo>("getElementAtPoint", args.ClientX, args.ClientY).ConfigureAwait(true)) : null
			};

			await UpdateState.InvokeAsync(cancelArgs).ConfigureAwait(true);
			if (!cancelArgs.Cancel && _module != null)
			{
				await _module.InvokeVoidAsync("showMenu", Id, args.ClientX, args.ClientY).ConfigureAwait(true);
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (_module != null)
		{
			await _module.InvokeVoidAsync("hideMenu", Id).ConfigureAwait(true);
			await _module.DisposeAsync().ConfigureAwait(true);
		}
	}
}
