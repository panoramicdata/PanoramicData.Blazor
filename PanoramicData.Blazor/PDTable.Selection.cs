namespace PanoramicData.Blazor;

public partial class PDTable<TItem>
{
	/// <summary>
	/// Returns an array of all currently selected items.
	/// </summary>
	public TItem[] GetSelectedItems() => KeyField is null
			? []
			: [.. ItemsToDisplay.Where(x => Selection.Contains(KeyField(x).ToString() ?? string.Empty))];

	/// <summary>
	/// Clears the current selection.
	/// </summary>
	public async Task ClearSelectionAsync()
	{
		if (Selection.Count > 0)
		{
			Selection.Clear();
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
			StateHasChanged();
		}
	}

	/// <summary>
	/// Determines whether a row item is selected.
	/// </summary>
	/// <param name="item">Row item.</param>
	/// <returns>True when selected.</returns>
	public bool IsSelected(TItem item)
	{
		if (SelectionMode != TableSelectionMode.None && KeyField != null)
		{
			return Selection.Contains(KeyField(item)?.ToString() ?? string.Empty);
		}

		return false;
	}

	private async Task OnDivMouseDown(MouseEventArgs args)
	{
		// if mouse down event occurred straight from Div then clear selection
		if (!_mouseDownOriginatedFromTable && (args.Button != 2 || RightClickSelectsRow))
		{
			Selection.Clear();
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}

		_mouseDownOriginatedFromTable = false;
	}

	private void OnTableMouseDown() =>
		// store fact that mouse down occurred from Table element
		_mouseDownOriginatedFromTable = true;

	/// <summary>
	/// Gets whether the given row can be selected.
	/// </summary>
	private bool CanSelectRow(TItem item) =>
		item is not null
		&& IsEnabled
		&& SelectionMode != TableSelectionMode.None
		&& RowIsEnabled(item);

	private async Task OnRowMouseUpAsync(MouseEventArgs args, TItem item)
	{
		// quit if selection not allowed
		if (!CanSelectRow(item))
		{
			return;
		}

		var key = KeyField!(item)?.ToString() ?? string.Empty;
		if (ShouldSelectOnMouseUp(args, key))
		{
			// begin edit mode?
			if (ShouldBeginEditOnMouseUp(args, key))
			{
				_editTimer?.Change(250, Timeout.Infinite);
			}
			else
			{
				await SelectItemAsync(key, args.ShiftKey, args.CtrlKey).ConfigureAwait(true);
			}
		}
	}

	/// <summary>
	/// Determines whether a mouse up selects the row: a left click always does, and a right click does when
	/// right-clicking selects rows and the row is not already selected.
	/// </summary>
	private bool ShouldSelectOnMouseUp(MouseEventArgs args, string key) =>
		args.Button == 0
		|| (args.Button == 2 && RightClickSelectsRow && _commonModule != null && !Selection.Contains(key));

	/// <summary>
	/// Determines whether a mouse up begins editing: a plain left click on the only selected row of an
	/// editable table that does not edit on double click.
	/// </summary>
	private bool ShouldBeginEditOnMouseUp(MouseEventArgs args, string key) =>
		AllowEdit
		&& !IsEditing
		&& Selection.Count == 1
		&& Selection.Contains(key)
		&& !args.CtrlKey
		&& args.Button == 0
		&& !EditOnDoubleClick;

	private void OnRowClick(TItem item)
	{
		if (IsEnabled)
		{
			Click.InvokeAsync(item);
		}
	}

	private async Task OnRowDoubleClick(TItem item)
	{
		// cancel pending edit mode
		_editTimer?.Change(Timeout.Infinite, Timeout.Infinite);

		if (EditOnDoubleClick)
		{
			await BeginEditAsync();
		}

		if (IsEnabled)
		{
			await DoubleClick.InvokeAsync(item);
		}
	}

	/// <summary>
	/// Toggles selection for all currently displayed rows.
	/// </summary>
	/// <param name="on">True to select all; false to clear selection.</param>
	public async Task OnToggleAllSelection(bool on)
	{
		if (IsEnabled && KeyField != null)
		{
			foreach (var item in ItemsToDisplay.Where(RowIsEnabled))
			{
				SetKeySelected(KeyField(item).ToString(), on);
			}

			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Adds the key to, or removes it from, the selection.
	/// </summary>
	private void SetKeySelected(string? key, bool on)
	{
		if (key != null)
		{
			if (on && !Selection.Contains(key))
			{
				Selection.Add(key);
			}
			else if (!on)
			{
				Selection.Remove(key);
			}
		}
	}

	/// <summary>
	/// Toggles selection for a specific row.
	/// </summary>
	/// <param name="item">Row item.</param>
	/// <param name="on">True to select; false to deselect.</param>
	public async Task OnToggleSelection(TItem item, bool on)
	{
		if (IsEnabled && KeyField != null && RowIsEnabled(item))
		{
			var key = KeyField(item).ToString();
			if (key != null)
			{
				ToggleKey(key, on);
			}

			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Selects the key when switching on a key that is not selected; otherwise deselects it.
	/// </summary>
	private void ToggleKey(string key, bool on)
	{
		if (on && !Selection.Contains(key))
		{
			Selection.Add(key);
		}
		else
		{
			Selection.Remove(key);
		}
	}

	/// <summary>
	/// Selects a row item key using current selection mode semantics, as a plain click would.
	/// </summary>
	/// <param name="key">Row key value.</param>
	public Task SelectItemAsync(string key) => SelectItemAsync(key, false, false);

	/// <summary>
	/// Selects a row item key using current selection mode semantics, optionally as a shift-click.
	/// </summary>
	/// <param name="key">Row key value.</param>
	/// <param name="shiftKey">True when shift range-selection behavior should be applied.</param>
	public Task SelectItemAsync(string key, bool shiftKey) => SelectItemAsync(key, shiftKey, false);

	/// <summary>
	/// Selects a row item key using current selection mode semantics.
	/// </summary>
	/// <param name="key">Row key value.</param>
	/// <param name="shiftKey">True when shift range-selection behavior should be applied.</param>
	/// <param name="ctrlKey">True when ctrl toggle-selection behavior should be applied.</param>
	public async Task SelectItemAsync(string key, bool shiftKey, bool ctrlKey)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}

		if (SelectionMode == TableSelectionMode.Single)
		{
			await SelectSingleItemAsync(key).ConfigureAwait(true);
		}
		else if (SelectionMode == TableSelectionMode.Multiple)
		{
			await SelectMultipleItemAsync(key, shiftKey, ctrlKey).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Selects only the given key, unless it is already selected.
	/// </summary>
	private async Task SelectSingleItemAsync(string key)
	{
		if (!Selection.Contains(key))
		{
			Selection.Clear();
			Selection.Add(key);
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Applies multiple selection: shift selects a range, ctrl toggles the key, otherwise only the key is selected.
	/// </summary>
	private async Task SelectMultipleItemAsync(string key, bool shiftKey, bool ctrlKey)
	{
		if (shiftKey && Selection.Count > 0) // range selection (from last selected to row clicked on)
		{
			await SelectRangeAsync(key).ConfigureAwait(true);
			return;
		}

		if (ctrlKey) // toggle selection
		{
			if (!Selection.Remove(key))
			{
				Selection.Add(key);
			}
		}
		else // single selection
		{
			Selection.Clear();
			Selection.Add(key);
		}

		await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
	}

	/// <summary>
	/// Selects the rows from the last selected one to the one with the given key.
	/// </summary>
	private async Task SelectRangeAsync(string key)
	{
		Selection.RemoveRange(0, Selection.Count - 1);
		var idxFrom = ItemsToDisplay.FindIndex(x => KeyField!(x)?.ToString() == Selection[0]);
		var idxTo = ItemsToDisplay.FindIndex(x => KeyField!(x)?.ToString() == key);
		if (idxFrom > -1 && idxTo > -1)
		{
			var start = Math.Min(idxFrom, idxTo);
			var count = Math.Max(idxFrom, idxTo) - start + 1;
			Selection.Clear();
			Selection.AddRange(ItemsToDisplay
				.GetRange(start, count)
				.Select(x => KeyField!(x).ToString() ?? string.Empty));
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}
}
