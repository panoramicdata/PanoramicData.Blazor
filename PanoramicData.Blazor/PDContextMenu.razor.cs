namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that displays a context menu when the user right-clicks on the wrapped content.
/// </summary>
public partial class PDContextMenu
{
	private static int _idSequence;

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

	private Task OnMouseDownAsync(MouseEventArgs args) => OnMouseButtonAsync(args, isMouseUp: false);

	private Task OnMouseUpAsync(MouseEventArgs args) => OnMouseButtonAsync(args, isMouseUp: true);

	// Shows the menu on a right-click, on the mouse down or up event as configured by ShowOnMouseUp
	private Task OnMouseButtonAsync(MouseEventArgs args, bool isMouseUp)
	{
		if (Enabled && args.Button == 2 && ShowOnMouseUp == isMouseUp)
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
}
