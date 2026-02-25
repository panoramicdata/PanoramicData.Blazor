using Microsoft.AspNetCore.Components.Routing;

namespace PanoramicData.Blazor;

/// <summary>
/// A tabbed dashboard container with CSS grid layout, kiosk/display mode,
/// tab rotation, and drag-and-drop tile editing.
/// </summary>
public partial class PDDashboard : PDComponentBase, IAsyncDisposable
{
	private static int _idSequence;
	private Timer? _rotationTimer;
	private PDDashboardTile? _draggedTile;
	private PDDashboardTile? _dragOverTile;
	private bool _isUserInteracting;
	private List<(PDDashboardTile Tile, int RowIndex, int ColumnIndex)>? _dragStartSnapshot;
	private bool _dragDropCompleted;

	// Resize state
	private PDDashboardTile? _resizingTile;
	private double _resizeStartX;
	private double _resizeStartY;
	private int _resizeOriginalColSpan;
	private int _resizeOriginalRowSpan;

	// Maximize state
	private PDDashboardTile? _maximizedTile;

	// Delete state
	private PDDashboardTile? _pendingDeleteTile;
	private PDConfirm? _confirmDelete;

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
	/// Gets or sets the percentage of dashboard area used when a tile is maximized. Default 80.
	/// </summary>
	[Parameter]
	public int MaximizePercent { get; set; } = 80;

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
	/// Fired when the user requests to add a new tile. The consumer should create the tile and add it to the active tab.
	/// </summary>
	[Parameter]
	public EventCallback OnTileAdd { get; set; }

	/// <summary>
	/// Fired when a tile is deleted. The tile has already been removed from the active tab.
	/// </summary>
	[Parameter]
	public EventCallback<PDDashboardTile> OnTileDelete { get; set; }

	/// <summary>
	/// Gets or sets whether tile deletion requires a confirmation dialog. Default true.
	/// </summary>
	[Parameter]
	public bool ConfirmTileDelete { get; set; } = true;

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

	/// <summary>
	/// Gets the index of the currently active tab.
	/// </summary>
	public int ActiveTabIndex { get; private set; }

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
			ActiveTabIndex = tabIndex;
		}
		else
		{
			ActiveTabIndex = StartTab;
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
		if (index < 0 || index >= Tabs.Count || index == ActiveTabIndex)
		{
			return;
		}

		ActiveTabIndex = index;
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
						ActiveTabIndex = (ActiveTabIndex + 1) % Tabs.Count;
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
		if (ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count)
		{
			var tabOverride = Tabs[ActiveTabIndex].TabRotationIntervalSecondsOverride;
			if (tabOverride.HasValue)
			{
				return tabOverride.Value;
			}
		}

		return TabRotationIntervalSeconds;
	}

	// Drag-and-drop
	private void OnTileDragStart(DragEventArgs _, PDDashboardTile tile)
	{
		if (!IsEditable)
		{
			return;
		}

		_draggedTile = tile;
		_dragDropCompleted = false;

		// Save snapshot of all tile positions for potential revert
		var activeTab = (ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count) ? Tabs[ActiveTabIndex] : null;
		if (activeTab is not null)
		{
			_dragStartSnapshot = activeTab.Tiles
				.Select(t => (Tile: t, t.RowIndex, t.ColumnIndex))
				.ToList();
		}
	}

	private void OnTileDragOver(DragEventArgs _, PDDashboardTile tile)
	{
		if (_draggedTile is null || _draggedTile == tile || _dragOverTile == tile)
		{
			return;
		}

		_dragOverTile = tile;

		// Live re-layout preview
		var activeTab = (ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count) ? Tabs[ActiveTabIndex] : null;
		if (activeTab is not null)
		{
			var cols = activeTab.ColumnCount ?? ColumnCount;
			_draggedTile.RowIndex = tile.RowIndex;
			_draggedTile.ColumnIndex = Math.Min(tile.ColumnIndex, Math.Max(0, cols - _draggedTile.ColumnSpanCount));
			CompactTiles(activeTab, _draggedTile);
			StateHasChanged();
		}
	}

	private void OnTileDragLeave(DragEventArgs _)
	{
		_dragOverTile = null;
	}

	private async Task OnTileDropAsync(DragEventArgs _, PDDashboardTile targetTile)
	{
		_dragOverTile = null;
		_dragDropCompleted = true;

		if (!IsEditable || _draggedTile is null || _draggedTile == targetTile)
		{
			// Drop on self or invalid — restore original positions if layout was previewed
			if (_dragStartSnapshot is not null)
			{
				foreach (var (tile, rowIndex, columnIndex) in _dragStartSnapshot)
				{
					tile.RowIndex = rowIndex;
					tile.ColumnIndex = columnIndex;
				}
			}

			_draggedTile = null;
			_dragStartSnapshot = null;
			StateHasChanged();
			return;
		}

		// Layout was already applied during dragover preview
		if (OnTileMove.HasDelegate)
		{
			await OnTileMove.InvokeAsync((_draggedTile, _draggedTile.RowIndex, _draggedTile.ColumnIndex)).ConfigureAwait(true);
		}

		_draggedTile = null;
		_dragStartSnapshot = null;
		StateHasChanged();
	}

	/// <summary>
	/// Compacts tiles to fill gaps by repositioning them to the earliest available positions.
	/// The anchor tile (if any) keeps its position; all others reflow around it.
	/// </summary>
	private void CompactTiles(PDDashboardTab tab, PDDashboardTile? anchor = null)
	{
		var cols = tab.ColumnCount ?? ColumnCount;

		// Order tiles: anchor first (to reserve its position), then by row then column
		var ordered = tab.Tiles
			.OrderBy(t => t == anchor ? 0 : 1)
			.ThenBy(t => t.RowIndex)
			.ThenBy(t => t.ColumnIndex)
			.ToList();

		// Build occupancy grid as we place each tile
		var occupied = new HashSet<(int Row, int Col)>();

		foreach (var tile in ordered)
		{
			if (tile == anchor)
			{
				// Reserve anchor's position
				for (var r = tile.RowIndex; r < tile.RowIndex + tile.RowSpanCount; r++)
				{
					for (var c = tile.ColumnIndex; c < tile.ColumnIndex + tile.ColumnSpanCount; c++)
					{
						occupied.Add((r, c));
					}
				}

				continue;
			}

			// Find earliest position for this tile
			var placed = false;
			for (var row = 0; !placed; row++)
			{
				for (var col = 0; col <= cols - tile.ColumnSpanCount && !placed; col++)
				{
					var fits = true;
					for (var dr = 0; dr < tile.RowSpanCount && fits; dr++)
					{
						for (var dc = 0; dc < tile.ColumnSpanCount && fits; dc++)
						{
							if (occupied.Contains((row + dr, col + dc)))
							{
								fits = false;
							}
						}
					}

					if (fits)
					{
						tile.RowIndex = row;
						tile.ColumnIndex = col;
						for (var dr = 0; dr < tile.RowSpanCount; dr++)
						{
							for (var dc = 0; dc < tile.ColumnSpanCount; dc++)
							{
								occupied.Add((row + dr, col + dc));
							}
						}

						placed = true;
					}
				}
			}
		}
	}

	private void OnTileDragEnd(DragEventArgs e)
	{
		if (!_dragDropCompleted && _dragStartSnapshot is not null)
		{
			// Drag was cancelled (e.g., Escape pressed) — restore original positions
			foreach (var (tile, rowIndex, columnIndex) in _dragStartSnapshot)
			{
				tile.RowIndex = rowIndex;
				tile.ColumnIndex = columnIndex;
			}
		}

		_draggedTile = null;
		_dragOverTile = null;
		_dragStartSnapshot = null;
		_dragDropCompleted = false;
		StateHasChanged();
	}

	private void OnDashboardKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Escape" && _draggedTile is not null && _dragStartSnapshot is not null)
		{
			foreach (var (tile, rowIndex, columnIndex) in _dragStartSnapshot)
			{
				tile.RowIndex = rowIndex;
				tile.ColumnIndex = columnIndex;
			}

			_draggedTile = null;
			_dragOverTile = null;
			_dragStartSnapshot = null;
			_dragDropCompleted = false;
			StateHasChanged();
		}
	}

	private async Task OnGridDropAsync(DragEventArgs _)
	{
		// Handle drop on empty grid space — finalize the previewed layout
		if (_dragDropCompleted || _draggedTile is null)
		{
			return;
		}

		_dragDropCompleted = true;
		_dragOverTile = null;

		if (OnTileMove.HasDelegate)
		{
			await OnTileMove.InvokeAsync((_draggedTile, _draggedTile.RowIndex, _draggedTile.ColumnIndex)).ConfigureAwait(true);
		}

		_draggedTile = null;
		_dragStartSnapshot = null;
		StateHasChanged();
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

		var activeTab = (ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count) ? Tabs[ActiveTabIndex] : null;
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

			// Compact other tiles around the resized tile
			var activeTab = (ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count) ? Tabs[ActiveTabIndex] : null;
			if (activeTab is not null)
			{
				CompactTiles(activeTab, tile);
			}

			if (OnTileResize.HasDelegate)
			{
				await OnTileResize.InvokeAsync((tile, tile.RowSpanCount, tile.ColumnSpanCount)).ConfigureAwait(true);
			}

			StateHasChanged();
		}
	}

	// Maximize/Restore
	private void MaximizeTile(PDDashboardTile tile)
	{
		_maximizedTile = tile;
	}

	private void RestoreTile()
	{
		_maximizedTile = null;
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

	private async Task RequestAddTileAsync()
	{
		if (OnTileAdd.HasDelegate)
		{
			await OnTileAdd.InvokeAsync().ConfigureAwait(true);
			StateHasChanged();
		}
	}

	private async Task RequestDeleteTileAsync(PDDashboardTile tile)
	{
		if (ConfirmTileDelete && _confirmDelete is not null)
		{
			_pendingDeleteTile = tile;
			var result = await _confirmDelete.ShowAndWaitResultAsync().ConfigureAwait(true);
			if (result == PDConfirm.Outcomes.Yes)
			{
				await PerformDeleteTileAsync(_pendingDeleteTile).ConfigureAwait(true);
			}

			_pendingDeleteTile = null;
		}
		else
		{
			await PerformDeleteTileAsync(tile).ConfigureAwait(true);
		}
	}

	private async Task PerformDeleteTileAsync(PDDashboardTile tile)
	{
		var activeTab = (ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count) ? Tabs[ActiveTabIndex] : null;
		if (activeTab is null)
		{
			return;
		}

		activeTab.Tiles.Remove(tile);
		CompactTiles(activeTab);

		if (OnTileDelete.HasDelegate)
		{
			await OnTileDelete.InvokeAsync(tile).ConfigureAwait(true);
		}

		StateHasChanged();
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

		if (ActiveTabIndex >= Tabs.Count)
		{
			ActiveTabIndex = Math.Max(0, Tabs.Count - 1);
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

	/// <summary>
	/// Finds the next available grid position in the active tab that can fit a tile
	/// with the given column and row span. Scans row-by-row, column-by-column.
	/// </summary>
	/// <param name="colSpan">Number of columns the tile needs.</param>
	/// <param name="rowSpan">Number of rows the tile needs.</param>
	/// <returns>The (RowIndex, ColumnIndex) for the tile, or the next empty row if no gap is found.</returns>
	public (int RowIndex, int ColumnIndex) FindNextAvailablePosition(int colSpan = 1, int rowSpan = 1)
	{
		var activeTab = (ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count) ? Tabs[ActiveTabIndex] : null;
		if (activeTab is null || activeTab.Tiles.Count == 0)
		{
			return (0, 0);
		}

		var cols = activeTab.ColumnCount ?? ColumnCount;
		var maxRow = activeTab.Tiles.Max(t => t.RowIndex + t.RowSpanCount);

		// Build an occupancy grid
		var occupied = new HashSet<(int Row, int Col)>();
		foreach (var tile in activeTab.Tiles)
		{
			for (var r = tile.RowIndex; r < tile.RowIndex + tile.RowSpanCount; r++)
			{
				for (var c = tile.ColumnIndex; c < tile.ColumnIndex + tile.ColumnSpanCount; c++)
				{
					occupied.Add((r, c));
				}
			}
		}

		// Scan for a gap that fits
		for (var row = 0; row <= maxRow; row++)
		{
			for (var col = 0; col <= cols - colSpan; col++)
			{
				var fits = true;
				for (var dr = 0; dr < rowSpan && fits; dr++)
				{
					for (var dc = 0; dc < colSpan && fits; dc++)
					{
						if (occupied.Contains((row + dr, col + dc)))
						{
							fits = false;
						}
					}
				}

				if (fits)
				{
					return (row, col);
				}
			}
		}

		// No gap found — place on the next row
		return (maxRow, 0);
	}

	// Dashboard configuration
	private bool _isConfiguringDashboard;
	private string _configTabName = string.Empty;
	private int _configColumnCount;
	private int _configRowHeight;

	private void OpenDashboardConfig()
	{
		if (ActiveTabIndex < 0 || ActiveTabIndex >= Tabs.Count)
		{
			return;
		}

		var activeTab = Tabs[ActiveTabIndex];
		_configTabName = activeTab.Name;
		_configColumnCount = activeTab.ColumnCount ?? ColumnCount;
		_configRowHeight = activeTab.TileRowHeightPx ?? TileRowHeightPx;
		_isConfiguringDashboard = true;
	}

	private void CancelDashboardConfig()
	{
		_isConfiguringDashboard = false;
	}

	private async Task ApplyDashboardConfigAsync()
	{
		if (ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count)
		{
			var activeTab = Tabs[ActiveTabIndex];
			activeTab.Name = _configTabName;
			activeTab.ColumnCount = _configColumnCount;
			activeTab.TileRowHeightPx = _configRowHeight;
		}

		_isConfiguringDashboard = false;

		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync().ConfigureAwait(true);
		}

		StateHasChanged();
	}

	public ValueTask DisposeAsync()
	{
		_rotationTimer?.Dispose();
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
