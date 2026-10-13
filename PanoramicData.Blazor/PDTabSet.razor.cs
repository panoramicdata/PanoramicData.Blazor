namespace PanoramicData.Blazor;

/// <summary>
/// Specifies where the create-tab button is rendered within a <see cref="PDTabSet"/>.
/// </summary>
public enum CreateTabPosition
{
	/// <summary>Render the create-tab button at the start (left) of the tab strip.</summary>
	Start,
	/// <summary>Render the create-tab button at the end (right) of the tab strip.</summary>
	End,
	/// <summary>Render the create-tab button at both ends of the tab strip.</summary>
	Both
}

/// <summary>
/// A Blazor component that provides a tabbed container supporting dynamic tab management.
/// </summary>
public partial class PDTabSet : ComponentBase
{
	internal List<PDTab> Tabs { get; } = [];
	internal PDTab? ActiveTab { get; set; }

	private PDTab? _dragTab;
	private PDTab? _dragOverTab;
	/// <summary>Index at which the next <see cref="AddTab"/> call should insert; -1 means append.</summary>
	private int _pendingInsertIndex = -1;
	/// <summary>
	/// The tab id the host asked for through <see cref="SelectedTabId"/> that has not been honoured yet,
	/// because no tab with that id has been added so far. Cleared once that tab is selected, or when a tab is
	/// selected by the user.
	/// </summary>
	private Guid? _requestedTabId;
	/// <summary>The last <see cref="SelectedTabId"/> received, so a re-render with the same value is not treated as a new request.</summary>
	private Guid? _lastSelectedTabId;

	/// <summary>
	/// Gets or sets the child content of the component.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the CSS class for the component.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether tabs can be closed.
	/// </summary>
	[Parameter] public bool IsTabClosingEnabled { get; set; }

	/// <summary>
	/// Gets or sets the minimum width of a tab.
	/// </summary>
	[Parameter] public string TabMinWidth { get; set; } = "100px";

	/// <summary>
	/// Gets or sets the maximum width of a tab.
	/// </summary>
	[Parameter] public string TabMaxWidth { get; set; } = "240px";

	/// <summary>
	/// Gets or sets whether new tabs can be added.
	/// </summary>
	[Parameter] public bool IsTabAdditionEnabled { get; set; }

	/// <summary>
	/// Gets or sets the position of the create tab button.
	/// </summary>
	[Parameter] public CreateTabPosition CreateTabPosition { get; set; } = CreateTabPosition.Start;

	/// <summary>
	/// Gets or sets whether tabs can be renamed.
	/// </summary>
	[Parameter] public bool IsTabRenamingEnabled { get; set; }

	/// <summary>
	/// Gets or sets whether tabs can be reordered by dragging. When enabled, the underlying <see cref="Tabs"/> list order is updated on drop.
	/// </summary>
	[Parameter] public bool IsTabReorderingEnabled { get; set; }

	/// <summary>
	/// An event callback that is invoked when the tab order changes after a drag-reorder operation.
	/// </summary>
	[Parameter] public EventCallback<IReadOnlyList<PDTab>> OnTabsReordered { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="PDTab.Id"/> of the selected tab, so a host can choose which tab is selected
	/// (for example from a URL) and use <c>@bind-SelectedTabId</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A new value selects the tab with that id. If no such tab has been added yet - because it is rendered
	/// later, for example inside an <c>@if</c> - the request is kept and honoured when the tab is added.
	/// An id that never matches a tab leaves the current selection unchanged.
	/// </para>
	/// <para>
	/// Give each tab the host needs to select a stable <see cref="PDTab.Id"/>; a tab without one is assigned
	/// a new id every time it is created.
	/// </para>
	/// </remarks>
	[Parameter] public Guid? SelectedTabId { get; set; }

	/// <summary>
	/// An event callback that is invoked with the <see cref="PDTab.Id"/> of the tab the user selected, or
	/// <c>null</c> when that tab has no id (<see cref="Guid.Empty"/>). Enables <c>@bind-SelectedTabId</c>.
	/// </summary>
	/// <remarks>
	/// It is invoked alongside <see cref="OnTabSelected"/>, but not for a selection made by setting
	/// <see cref="SelectedTabId"/>, so binding it back to the same property does not loop.
	/// </remarks>
	[Parameter] public EventCallback<Guid?> SelectedTabIdChanged { get; set; }

	/// <summary>
	/// An event callback that is invoked when a tab is selected.
	/// </summary>
	[Parameter] public EventCallback<PDTab> OnTabSelected { get; set; }

	/// <summary>
	/// An event callback that is invoked when a tab is closed.
	/// </summary>
	[Parameter] public EventCallback<PDTab> OnTabClosed { get; set; }

	/// <summary>
	/// An event callback that is invoked when a new tab is added.
	/// The <see cref="CreateTabPosition"/> argument indicates which + button was pressed.
	/// </summary>
	[Parameter] public EventCallback<CreateTabPosition> OnTabAdded { get; set; }

	/// <summary>
	/// An event callback that is invoked when a tab is renamed.
	/// </summary>
	[Parameter] public EventCallback<PDTab> OnTabRenamed { get; set; }

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		// Only a changed value is a new request: the host re-rendering with the value it already gave must
		// not undo a tab the user has picked since.
		if (SelectedTabId == _lastSelectedTabId)
		{
			return;
		}

		_lastSelectedTabId = SelectedTabId;
		_requestedTabId = SelectedTabId is { } id && id != Guid.Empty ? id : null;
		ApplyRequestedTab();
	}

	/// <summary>
	/// Selects the tab named by <see cref="_requestedTabId"/> if it has been added, which clears the request.
	/// If it has not been added yet the request is kept for <see cref="AddTab"/>.
	/// </summary>
	private void ApplyRequestedTab()
	{
		if (_requestedTabId is not { } id)
		{
			return;
		}

		var tab = Tabs.FirstOrDefault(candidate => candidate.Id == id);
		if (tab is null)
		{
			return;
		}

		_requestedTabId = null;
		if (!ReferenceEquals(tab, ActiveTab))
		{
			// Not reported through SelectedTabIdChanged: the host already holds this value.
			SelectTab(tab, raiseSelectedTabIdChanged: false);
		}
	}

	internal void AddTab(PDTab tab)
	{
		if (_pendingInsertIndex >= 0 && _pendingInsertIndex <= Tabs.Count)
		{
			Tabs.Insert(_pendingInsertIndex, tab);
		}
		else
		{
			Tabs.Add(tab);
		}

		_pendingInsertIndex = -1;
		ActiveTab ??= tab;
		ApplyRequestedTab();
		StateHasChanged();
	}

	/// <summary>
	/// Redraws the tab strip after one of the tabs received new values for its title, CSS class, icon or
	/// closing/renaming overrides.
	/// </summary>
	/// <remarks>
	/// The strip is rendered from each tab's values before the tabs themselves, which are part of the child
	/// content, receive their new parameters; without this the change only showed on the next render.
	/// Only a tab whose values actually changed calls this, so the extra render does not repeat.
	/// </remarks>
	internal void OnTabAppearanceChanged() => StateHasChanged();

	/// <summary>
	/// Removes a tab that has left the render tree, without treating it as a user closing it.
	/// </summary>
	/// <param name="tab">The tab being disposed.</param>
	/// <remarks>
	/// The counterpart to the self-registration in <see cref="PDTab.OnInitialized"/>. Without it a tab set
	/// driven from a changing collection accumulated tabs for items that no longer existed. It does not raise
	/// <see cref="OnTabClosed"/>, which means a user closed a tab - firing that here would raise it once per
	/// tab when the whole tab set is torn down.
	/// </remarks>
	internal void RemoveTab(PDTab tab)
	{
		if (!Tabs.Remove(tab))
		{
			return;
		}

		if (ReferenceEquals(ActiveTab, tab))
		{
			ActiveTab = Tabs.FirstOrDefault();
		}

		// Queued rather than called directly. This runs while Blazor is disposing the child component, part
		// way through a render it has already produced for the old tab list - so without asking for another
		// one the removed tab stays on screen until something else happens to trigger a render, and calling
		// StateHasChanged synchronously here would re-enter the renderer mid-teardown.
		_ = InvokeAsync(StateHasChanged);
	}

	/// <summary>
	/// Selects the tab carrying a given <see cref="PDTab.Id"/>, if it is present.
	/// </summary>
	/// <param name="id">The tab id to select.</param>
	/// <returns><c>true</c> if a tab with that id was found and selected.</returns>
	/// <remarks>
	/// Lets a host that owns the collection behind the tabs select one by the identity it already holds,
	/// rather than having to keep a reference to a component instance it did not create.
	/// Selecting a different tab raises <see cref="SelectedTabIdChanged"/> as well as <see cref="OnTabSelected"/>,
	/// so a host using <c>@bind-SelectedTabId</c> stays in step. An unknown id changes nothing and returns <c>false</c>.
	/// </remarks>
	public bool SelectTabById(Guid id)
	{
		var tab = Tabs.FirstOrDefault(candidate => candidate.Id == id);
		if (tab is null || ReferenceEquals(tab, ActiveTab))
		{
			return tab is not null;
		}

		SelectTab(tab);
		return true;
	}

	internal void SelectTab(PDTab tab) => SelectTab(tab, raiseSelectedTabIdChanged: true);

	private void SelectTab(PDTab tab, bool raiseSelectedTabIdChanged)
	{
		ActiveTab = tab;
		if (raiseSelectedTabIdChanged)
		{
			// A choice made by the user (or by method) wins over a request through the parameter that has
			// not found its tab yet, which must not override it when that tab turns up later.
			_requestedTabId = null;
			if (SelectedTabIdChanged.HasDelegate)
			{
				_ = SelectedTabIdChanged.InvokeAsync(tab.Id == Guid.Empty ? null : tab.Id);
			}
		}

		if (OnTabSelected.HasDelegate)
		{
			OnTabSelected.InvokeAsync(tab);
		}

		tab.OnSelected.InvokeAsync();
		StateHasChanged();
	}

	internal void CloseTab(PDTab tab, MouseEventArgs? e = null)
	{
		if (Tabs.Remove(tab))
		{
			if (_requestedTabId == tab.Id)
			{
				_requestedTabId = null;
			}

			if (ActiveTab == tab)
			{
				ActiveTab = Tabs.FirstOrDefault();

				// The selection moved without the user choosing a tab, so tell a bound host which one is
				// selected now (null when none is left or it has no id).
				if (SelectedTabIdChanged.HasDelegate)
				{
					var newId = ActiveTab?.Id;
					_ = SelectedTabIdChanged.InvokeAsync(newId == Guid.Empty ? null : newId);
				}
			}

			if (OnTabClosed.HasDelegate)
			{
				OnTabClosed.InvokeAsync(tab);
			}

			StateHasChanged();
		}
	}

	internal bool GetTabCanBeClosed(PDTab tab)
		=> tab.IsClosingEnabled ?? IsTabClosingEnabled;

	internal bool GetTabCanBeRenamed(PDTab tab)
		=> tab.IsRenamingEnabled ?? IsTabRenamingEnabled;

	internal void OnDragStart(PDTab tab)
	{
		_dragTab = tab;
		StateHasChanged();
	}

	internal void OnDragOver(PDTab tab)
	{
		if (_dragTab == null || object.ReferenceEquals(_dragTab, tab))
		{
			return;
		}

		if (!object.ReferenceEquals(_dragOverTab, tab))
		{
			_dragOverTab = tab;
			StateHasChanged();
		}
	}

	internal void OnDrop(PDTab tab)
	{
		if (_dragTab != null && !object.ReferenceEquals(_dragTab, tab))
		{
			var fromIndex = Tabs.IndexOf(_dragTab);
			var toIndex = Tabs.IndexOf(tab);
			if (fromIndex >= 0 && toIndex >= 0)
			{
				Tabs.RemoveAt(fromIndex);
				// When dragging right, removing the source shifts all subsequent
				// indices down by one, so compensate to land in the correct slot.
				var insertIndex = toIndex > fromIndex ? toIndex - 1 : toIndex;
				Tabs.Insert(insertIndex, _dragTab);
				if (OnTabsReordered.HasDelegate)
				{
					_ = OnTabsReordered.InvokeAsync(Tabs.AsReadOnly());
				}
			}
		}

		_dragTab = null;
		_dragOverTab = null;
		StateHasChanged();
	}

	internal void OnDragEnd()
	{
		_dragTab = null;
		_dragOverTab = null;
		StateHasChanged();
	}

	internal void OnAddTabClicked(CreateTabPosition position)
	{
		_pendingInsertIndex = position == CreateTabPosition.Start ? 0 : -1;
		if (OnTabAdded.HasDelegate)
		{
			_ = OnTabAdded.InvokeAsync(position);
		}
	}

	/// <inheritdoc />
	protected override Task OnAfterRenderAsync(bool firstRender)
	{
		// Clear any stale pending insert index once the render cycle completes.
		_pendingInsertIndex = -1;
		return base.OnAfterRenderAsync(firstRender);
	}

	internal void StartRenamingTab(PDTab tab)
	{
		if (GetTabCanBeRenamed(tab))
		{
			tab.IsRenaming = true;
			StateHasChanged();
		}
	}

	internal static void OnRenameTabInput(PDTab tab, ChangeEventArgs e)
	{
		tab.TempTitle = e.Value?.ToString() ?? string.Empty;
	}

	internal void OnRenameTabBlur(PDTab tab, FocusEventArgs e)
	{
		CommitTabRename(tab);
	}

	internal void OnRenameTabKeyDown(PDTab tab, KeyboardEventArgs e)
	{
		if (e.Key == "Enter")
		{
			CommitTabRename(tab);
		}
		else if (e.Key == "Escape")
		{
			tab.IsRenaming = false;
			tab.TempTitle = tab.Title;
			StateHasChanged();
		}
	}

	private void CommitTabRename(PDTab tab)
	{
		if (tab.TempTitle != tab.Title)
		{
#pragma warning disable BL0005 // Parent component manages tab title during rename
			tab.Title = tab.TempTitle;
#pragma warning restore BL0005

			if (OnTabRenamed.HasDelegate)
			{
				OnTabRenamed.InvokeAsync(tab);
			}
		}

		tab.IsRenaming = false;

		StateHasChanged();
	}
}
