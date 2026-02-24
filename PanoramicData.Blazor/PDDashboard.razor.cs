using Microsoft.AspNetCore.Components.Routing;

namespace PanoramicData.Blazor;

/// <summary>
/// A tabbed dashboard container with CSS grid layout, kiosk/display mode,
/// tab rotation, and drag-and-drop tile editing.
/// </summary>
public partial class PDDashboard : PDComponentBase, IAsyncDisposable
{
	private static int _idSequence;
	private int _activeTabIndex;
	private Timer? _rotationTimer;
	private PDDashboardTile? _draggedTile;
	private bool _isUserInteracting;

	// Resize state
	private PDDashboardTile? _resizingTile;
	private double _resizeStartX;
	private double _resizeStartY;
	private int _resizeOriginalColSpan;
	private int _resizeOriginalRowSpan;

	[Inject] private NavigationManager NavigationManager { get; set; } = null!;

	/// <summary>
	/// Gets or sets the dashboard tabs.
	/// </summary>
	[Parameter]
	public List<PDDashboardTab> Tabs { get; set; } = [];

	/// <summary>
	/// Gets or sets the number of grid columns. Default 12.
	/// </summary>
	[Parameter]
	public int ColumnCount { get; set; } = 12;

	/// <summary>
	/// Gets or sets the height of each grid row in pixels.
	/// </summary>
	[Parameter]
	public int TileRowHeightPx { get; set; } = 120;

	/// <summary>
	/// Gets or sets dashboard-level CSS classes.
	/// </summary>
	[Parameter]
	public string? Css { get; set; }

	/// <summary>
	/// Gets or sets whether to show the tab bar.
	/// </summary>
	[Parameter]
	public bool ShowTabs { get; set; } = true;

	/// <summary>
	/// Gets or sets the index of the initially selected tab.
	/// </summary>
	[Parameter]
	public int StartTab { get; set; }

	/// <summary>
	/// Gets or sets the tab auto-rotation interval in seconds. 0 = disabled.
	/// </summary>
	[Parameter]
	public int TabRotationIntervalSeconds { get; set; }

	/// <summary>
	/// Gets or sets kiosk/display mode. When true, hides all editing chrome.
	/// </summary>
	[Parameter]
	public bool DisplayMode { get; set; }

	/// <summary>
	/// Gets or sets whether editing controls are enabled.
	/// </summary>
	[Parameter]
	public bool IsEditable { get; set; }

	/// <summary>
	/// Fired when a tile is moved via drag-and-drop.
	/// </summary>
	[Parameter]
	public EventCallback<(PDDashboardTile Tile, int NewRow, int NewColumn)> OnTileMove { get; set; }

	/// <summary>
	/// Fired when a tile is resized via the resize handle.
	/// </summary>
	[Parameter]
	public EventCallback<(PDDashboardTile Tile, int NewRowSpan, int NewColumnSpan)> OnTileResize { get; set; }

	/// <summary>
	/// Fired when a tab is added.
	/// </summary>
	[Parameter]
	public EventCallback<PDDashboardTab> OnTabAdd { get; set; }

	/// <summary>
	/// Fired when a tab is removed.
	/// </summary>
	[Parameter]
	public EventCallback<PDDashboardTab> OnTabRemove { get; set; }

	/// <summary>
	/// Fired when settings change.
	/// </summary>
	[Parameter]
	public EventCallback OnSettingsChanged { get; set; }

	/// <summary>
	/// Fired when the active tab changes.
	/// </summary>
	[Parameter]
	public EventCallback<int> ActiveTabChanged { get; set; }

	protected override void OnInitialized()
	{
		base.OnInitialized();
		if (Id == $"pd-component-{Sequence}")
		{
			Id = $"pd-dashboard-{++_idSequence}";
		}

		// Read tab from URL deep link
		var uri = new Uri(NavigationManager.Uri);
		if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("tab", out var tabValue) &&
			int.TryParse(tabValue, CultureInfo.InvariantCulture, out var tabIndex) &&
			tabIndex >= 0 && tabIndex < Tabs.Count)
		{
			_activeTabIndex = tabIndex;
		}
		else
		{
			_activeTabIndex = StartTab;
		}
	}

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			SetupRotationTimer();
		}
	}

	private async Task SelectTabAsync(int index)
	{
		if (index < 0 || index >= Tabs.Count || index == _activeTabIndex)
		{
			return;
		}

		_activeTabIndex = index;
		_isUserInteracting = true;

		// Update URL with deep link
		var uri = NavigationManager.GetUriWithQueryParameter("tab", index.ToString(CultureInfo.InvariantCulture));
		NavigationManager.NavigateTo(uri, replace: true);

		if (ActiveTabChanged.HasDelegate)
		{
			await ActiveTabChanged.InvokeAsync(index).ConfigureAwait(true);
		}

		ResetRotationTimer();
		StateHasChanged();
	}

	private void SetupRotationTimer()
	{
		var interval = GetEffectiveRotationInterval();
		if (interval > 0)
		{
			_rotationTimer = new Timer(_ =>
			{
				if (!_isUserInteracting)
				{
					InvokeAsync(() =>
					{
						_activeTabIndex = (_activeTabIndex + 1) % Tabs.Count;
						StateHasChanged();
					});
				}
			}, null, TimeSpan.FromSeconds(interval), TimeSpan.FromSeconds(interval));
		}
	}

	private void ResetRotationTimer()
	{
		_rotationTimer?.Dispose();
		_rotationTimer = null;
		_isUserInteracting = false;
		SetupRotationTimer();
	}

	private int GetEffectiveRotationInterval()
	{
		if (_activeTabIndex >= 0 && _activeTabIndex < Tabs.Count)
		{
			var tabOverride = Tabs[_activeTabIndex].TabRotationIntervalSecondsOverride;
			if (tabOverride.HasValue)
			{
				return tabOverride.Value;
			}
		}

		return TabRotationIntervalSeconds;
	}

	// Drag-and-drop
	private void OnTileDragStart(DragEventArgs e, PDDashboardTile tile)
	{
		if (!IsEditable)
		{
			return;
		}

		_draggedTile = tile;
	}

	private void OnTileDragOver(DragEventArgs e, PDDashboardTile tile)
	{
		// Allow drop by preventing default (handled in markup)
	}

	private async Task OnTileDropAsync(DragEventArgs e, PDDashboardTile targetTile)
	{
		if (!IsEditable || _draggedTile is null || _draggedTile == targetTile)
		{
			_draggedTile = null;
			return;
		}

		// Swap positions
		var (dragRow, dragCol) = (_draggedTile.RowIndex, _draggedTile.ColumnIndex);
		_draggedTile.RowIndex = targetTile.RowIndex;
		_draggedTile.ColumnIndex = targetTile.ColumnIndex;
		targetTile.RowIndex = dragRow;
		targetTile.ColumnIndex = dragCol;

		if (OnTileMove.HasDelegate)
		{
			await OnTileMove.InvokeAsync((_draggedTile, _draggedTile.RowIndex, _draggedTile.ColumnIndex)).ConfigureAwait(true);
		}

		_draggedTile = null;
		StateHasChanged();
	}

	private void OnTileDragEnd(DragEventArgs e)
	{
		_draggedTile = null;
	}

	// Resize via pointer events
	private void OnResizePointerDown(PointerEventArgs e, PDDashboardTile tile)
	{
		_resizingTile = tile;
		_resizeStartX = e.ClientX;
		_resizeStartY = e.ClientY;
		_resizeOriginalColSpan = tile.ColumnSpanCount;
		_resizeOriginalRowSpan = tile.RowSpanCount;
	}

	private void OnResizePointerMove(PointerEventArgs e)
	{
		if (_resizingTile is null)
		{
			return;
		}

		var activeTab = (_activeTabIndex >= 0 && _activeTabIndex < Tabs.Count) ? Tabs[_activeTabIndex] : null;
		if (activeTab is null)
		{
			return;
		}

		var cols = activeTab.ColumnCount ?? ColumnCount;
		var rowHeight = activeTab.TileRowHeightPx ?? TileRowHeightPx;

		// Estimate column width from row height and column count (assume roughly square-ish grid cells)
		// Use rowHeight as a baseline since we know it precisely; column width depends on container
		// A reasonable estimate: column width ≈ rowHeight (for typical dashboards)
		var colWidth = rowHeight;

		var deltaX = e.ClientX - _resizeStartX;
		var deltaY = e.ClientY - _resizeStartY;

		var newColSpan = Math.Max(1, _resizeOriginalColSpan + (int)Math.Round(deltaX / colWidth));
		var newRowSpan = Math.Max(1, _resizeOriginalRowSpan + (int)Math.Round(deltaY / rowHeight));

		// Clamp to grid bounds
		newColSpan = Math.Min(newColSpan, cols - _resizingTile.ColumnIndex);

		if (newColSpan != _resizingTile.ColumnSpanCount || newRowSpan != _resizingTile.RowSpanCount)
		{
			_resizingTile.ColumnSpanCount = newColSpan;
			_resizingTile.RowSpanCount = newRowSpan;
			StateHasChanged();
		}
	}

	private async Task OnResizePointerUp(PointerEventArgs e)
	{
		if (_resizingTile is not null)
		{
			var tile = _resizingTile;
			_resizingTile = null;

			if (OnTileResize.HasDelegate)
			{
				await OnTileResize.InvokeAsync((tile, tile.RowSpanCount, tile.ColumnSpanCount)).ConfigureAwait(true);
			}

			StateHasChanged();
		}
	}

	private async Task AddTabAsync()
	{
		var newTab = new PDDashboardTab { Name = $"Tab {Tabs.Count + 1}" };
		Tabs.Add(newTab);

		if (OnTabAdd.HasDelegate)
		{
			await OnTabAdd.InvokeAsync(newTab).ConfigureAwait(true);
		}

		await SelectTabAsync(Tabs.Count - 1).ConfigureAwait(true);
	}

	/// <summary>
	/// Removes a tab from the dashboard.
	/// </summary>
	public async Task RemoveTabAsync(int index)
	{
		if (index < 0 || index >= Tabs.Count)
		{
			return;
		}

		var tab = Tabs[index];
		Tabs.RemoveAt(index);

		if (OnTabRemove.HasDelegate)
		{
			await OnTabRemove.InvokeAsync(tab).ConfigureAwait(true);
		}

		if (_activeTabIndex >= Tabs.Count)
		{
			_activeTabIndex = Math.Max(0, Tabs.Count - 1);
		}

		StateHasChanged();
	}

	/// <summary>
	/// Programmatically selects a tab by index.
	/// </summary>
	public async Task GoToTabAsync(int index)
	{
		await SelectTabAsync(index).ConfigureAwait(true);
	}

	public ValueTask DisposeAsync()
	{
		_rotationTimer?.Dispose();
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
