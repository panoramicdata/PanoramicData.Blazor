namespace PanoramicData.Blazor;

/// <summary>
/// The tile layout of <see cref="PDDashboard"/>: moving tiles by drag-and-drop, resizing them, adding and deleting
/// them, and packing the grid so that it has no gaps.
/// </summary>
public partial class PDDashboard
{
	private PDDashboardTile? _draggedTile;
	private PDDashboardTile? _dragOverTile;
	private List<(PDDashboardTile Tile, int RowIndex, int ColumnIndex)>? _dragStartSnapshot;
	private bool _dragDropCompleted;

	// Resize state
	private PDDashboardTile? _resizingTile;
	private double _resizeStartX;
	private double _resizeStartY;
	private int _resizeOriginalColSpan;
	private int _resizeOriginalRowSpan;

	// Delete state
	private PDDashboardTile? _pendingDeleteTile;

	/// <summary>Gets or sets the tile-delete confirmation dialog; set by the markup's <c>@ref</c>.</summary>
	internal PDConfirm? ConfirmDeleteDialog { get; set; }

	/// <summary>Gets the tab currently shown, or <c>null</c> when the active index is out of range.</summary>
	private PDDashboardTab? ActiveTab => Tabs.ElementAtOrDefault(ActiveTabIndex);

	// Drag-and-drop
	private void OnTileDragStart(PDDashboardTile tile)
	{
		if (!EffectiveIsEditable)
		{
			return;
		}

		_draggedTile = tile;
		_dragDropCompleted = false;

		// Save snapshot of all tile positions for potential revert
		if (ActiveTab is { } activeTab)
		{
			_dragStartSnapshot = [.. activeTab.Tiles.Select(t => (Tile: t, t.RowIndex, t.ColumnIndex))];
		}
	}

	private void OnTileDragOver(PDDashboardTile tile)
	{
		if (_draggedTile is null || _draggedTile == tile || _dragOverTile == tile)
		{
			return;
		}

		_dragOverTile = tile;

		// Live re-layout preview
		if (ActiveTab is { } activeTab)
		{
			var cols = activeTab.ColumnCount ?? ColumnCount;
			_draggedTile.RowIndex = tile.RowIndex;
			_draggedTile.ColumnIndex = Math.Min(tile.ColumnIndex, Math.Max(0, cols - _draggedTile.ColumnSpanCount));
			CompactTiles(activeTab, _draggedTile);
			StateHasChanged();
		}
	}

	private void OnTileDragLeave() => _dragOverTile = null;

	private async Task OnTileDropAsync(PDDashboardTile targetTile)
	{
		_dragOverTile = null;
		_dragDropCompleted = true;

		if (!EffectiveIsEditable || _draggedTile is null || _draggedTile == targetTile)
		{
			// Drop on self or invalid: restore original positions if layout was previewed
			RestoreDragStartPositions();
			_draggedTile = null;
			_dragStartSnapshot = null;
			StateHasChanged();
			return;
		}

		// Layout was already applied during dragover preview
		await CompleteTileMoveAsync(_draggedTile).ConfigureAwait(true);
	}

	private void OnTileDragEnd()
	{
		if (!_dragDropCompleted)
		{
			// Drag was cancelled (e.g., Escape pressed) — restore original positions
			RestoreDragStartPositions();
		}

		EndDrag();
	}

	private void OnDashboardKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Escape" && _draggedTile is not null && _dragStartSnapshot is not null)
		{
			RestoreDragStartPositions();
			EndDrag();
		}
	}

	private async Task OnGridDropAsync()
	{
		// Handle drop on empty grid space — finalize the previewed layout
		if (_dragDropCompleted || _draggedTile is null)
		{
			return;
		}

		_dragDropCompleted = true;
		_dragOverTile = null;

		await CompleteTileMoveAsync(_draggedTile).ConfigureAwait(true);
	}

	// Announces a tile's new position, then ends the drag.
	private async Task CompleteTileMoveAsync(PDDashboardTile tile)
	{
		if (OnTileMove.HasDelegate)
		{
			await OnTileMove.InvokeAsync((tile, tile.RowIndex, tile.ColumnIndex)).ConfigureAwait(true);
		}

		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync().ConfigureAwait(true);
		}

		_draggedTile = null;
		_dragStartSnapshot = null;
		StateHasChanged();
	}

	// Puts every tile back where it was when the drag started, if a layout was previewed.
	private void RestoreDragStartPositions()
	{
		foreach (var (tile, rowIndex, columnIndex) in _dragStartSnapshot ?? [])
		{
			tile.RowIndex = rowIndex;
			tile.ColumnIndex = columnIndex;
		}
	}

	private void EndDrag()
	{
		_draggedTile = null;
		_dragOverTile = null;
		_dragStartSnapshot = null;
		_dragDropCompleted = false;
		StateHasChanged();
	}

	/// <summary>
	/// Compacts tiles to fill gaps by repositioning them to the earliest available positions.
	/// The anchor tile (if any) keeps its position; all others reflow around it.
	/// </summary>
	private void CompactTiles(PDDashboardTab tab, PDDashboardTile? anchor = null)
	{
		var cols = tab.ColumnCount ?? ColumnCount;
		var occupied = new HashSet<(int Row, int Col)>();

		// Reserve the anchor's position first, then place the others by row then column
		if (anchor is not null && tab.Tiles.Contains(anchor))
		{
			Occupy(occupied, anchor.RowIndex, anchor.ColumnIndex, anchor.RowSpanCount, anchor.ColumnSpanCount);
		}

		var others = tab.Tiles
			.Where(t => t != anchor)
			.OrderBy(t => t.RowIndex)
			.ThenBy(t => t.ColumnIndex)
			.ToList();

		foreach (var tile in others)
		{
			var (row, col) = FindFirstFit(occupied, cols, tile.RowSpanCount, tile.ColumnSpanCount);
			tile.RowIndex = row;
			tile.ColumnIndex = col;
			Occupy(occupied, row, col, tile.RowSpanCount, tile.ColumnSpanCount);
		}
	}

	/// <summary>Marks the cells a tile covers as occupied.</summary>
	private static void Occupy(HashSet<(int Row, int Col)> occupied, int row, int col, int rowSpan, int colSpan)
	{
		for (var r = row; r < row + rowSpan; r++)
		{
			for (var c = col; c < col + colSpan; c++)
			{
				occupied.Add((r, c));
			}
		}
	}

	/// <summary>
	/// Finds the earliest position, row by row then column by column, where a tile of the given size covers no
	/// occupied cell.
	/// </summary>
	/// <remarks>
	/// Every row below the last occupied one is empty, so the search ends there: that row always takes the tile at
	/// column 0. That also bounds a tile wider than the grid, which fits no column at all, and would otherwise be
	/// searched for row after row without end.
	/// </remarks>
	private static (int Row, int Col) FindFirstFit(HashSet<(int Row, int Col)> occupied, int cols, int rowSpan, int colSpan)
	{
		var firstEmptyRow = occupied.Count == 0 ? 0 : occupied.Max(cell => cell.Row) + 1;
		for (var row = 0; row < firstEmptyRow; row++)
		{
			for (var col = 0; col <= cols - colSpan; col++)
			{
				if (Fits(occupied, row, col, rowSpan, colSpan))
				{
					return (row, col);
				}
			}
		}

		return (firstEmptyRow, 0);
	}

	private static bool Fits(HashSet<(int Row, int Col)> occupied, int row, int col, int rowSpan, int colSpan)
	{
		for (var r = row; r < row + rowSpan; r++)
		{
			for (var c = col; c < col + colSpan; c++)
			{
				if (occupied.Contains((r, c)))
				{
					return false;
				}
			}
		}

		return true;
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

	/// <summary>
	/// Resizes the tile being resized to follow the pointer. A move that arrives with no resize in progress -
	/// one queued behind the pointer-up that ended it, say - changes nothing.
	/// </summary>
	internal void OnResizePointerMove(PointerEventArgs e)
	{
		if (_resizingTile is null || ActiveTab is not { } activeTab)
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

	private async Task OnResizePointerUp()
	{
		if (_resizingTile is null)
		{
			return;
		}

		var tile = _resizingTile;
		_resizingTile = null;

		// Compact other tiles around the resized tile
		if (ActiveTab is { } activeTab)
		{
			CompactTiles(activeTab, tile);
		}

		if (OnTileResize.HasDelegate)
		{
			await OnTileResize.InvokeAsync((tile, tile.RowSpanCount, tile.ColumnSpanCount)).ConfigureAwait(true);
		}

		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync().ConfigureAwait(true);
		}

		StateHasChanged();
	}

	private async Task RequestAddTileAsync()
	{
		if (OnTileAdd.HasDelegate)
		{
			await OnTileAdd.InvokeAsync().ConfigureAwait(true);
		}
		else if (ActiveTab is { } activeTab)
		{
			var (row, col) = FindNextAvailablePosition();
			activeTab.Tiles.Add(new PDDashboardTile
			{
				RowIndex = row,
				ColumnIndex = col,
				ColumnSpanCount = 1,
				RowSpanCount = 1,
				ChildContent = builder =>
				{
					builder.OpenComponent<PDWidget>(0);
					builder.AddAttribute(1, nameof(PDWidget.Title), "New Widget");
					builder.AddAttribute(2, nameof(PDWidget.WidgetType), PDWidgetType.Html);
					builder.CloseComponent();
				}
			});
		}

		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync().ConfigureAwait(true);
		}

		StateHasChanged();
	}

	private async Task RequestDeleteTileAsync(PDDashboardTile tile)
	{
		if (ConfirmTileDelete && ConfirmDeleteDialog is not null)
		{
			_pendingDeleteTile = tile;
			var result = await ConfirmDeleteDialog.ShowAndWaitResultAsync().ConfigureAwait(true);
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
		if (ActiveTab is not { } activeTab)
		{
			return;
		}

		activeTab.Tiles.Remove(tile);
		CompactTiles(activeTab);

		if (OnTileDelete.HasDelegate)
		{
			await OnTileDelete.InvokeAsync(tile).ConfigureAwait(true);
		}

		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync().ConfigureAwait(true);
		}

		StateHasChanged();
	}

	/// <summary>
	/// Finds the next available grid position in the active tab that can fit a single-cell tile.
	/// </summary>
	/// <returns>The (RowIndex, ColumnIndex) for the tile, or the next empty row if no gap is found.</returns>
	public (int RowIndex, int ColumnIndex) FindNextAvailablePosition() => FindNextAvailablePosition(1, 1);

	/// <summary>
	/// Finds the next available grid position in the active tab that can fit a single-row tile with the given
	/// column span.
	/// </summary>
	/// <param name="colSpan">Number of columns the tile needs.</param>
	/// <returns>The (RowIndex, ColumnIndex) for the tile, or the next empty row if no gap is found.</returns>
	public (int RowIndex, int ColumnIndex) FindNextAvailablePosition(int colSpan) => FindNextAvailablePosition(colSpan, 1);

	/// <summary>
	/// Finds the next available grid position in the active tab that can fit a tile
	/// with the given column and row span. Scans row-by-row, column-by-column.
	/// </summary>
	/// <param name="colSpan">Number of columns the tile needs.</param>
	/// <param name="rowSpan">Number of rows the tile needs.</param>
	/// <returns>The (RowIndex, ColumnIndex) for the tile, or the next empty row if no gap is found.</returns>
	public (int RowIndex, int ColumnIndex) FindNextAvailablePosition(int colSpan, int rowSpan)
	{
		if (ActiveTab is not { } activeTab)
		{
			return (0, 0);
		}

		var occupied = new HashSet<(int Row, int Col)>();
		foreach (var tile in activeTab.Tiles)
		{
			Occupy(occupied, tile.RowIndex, tile.ColumnIndex, tile.RowSpanCount, tile.ColumnSpanCount);
		}

		return FindFirstFit(occupied, activeTab.ColumnCount ?? ColumnCount, rowSpan, colSpan);
	}
}
