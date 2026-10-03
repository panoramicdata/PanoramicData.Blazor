namespace PanoramicData.Blazor;

public partial class PDTable<TItem>
{
	private async Task OnKeyDownAsync(KeyboardEventArgs args)
	{
		if (IsEditing)
		{
			await OnEditingKeyDownAsync(args).ConfigureAwait(true);
		}
		else if (IsEnabled)
		{
			await OnBrowsingKeyDownAsync(args).ConfigureAwait(true);
		}

		await KeyDown.InvokeAsync(args).ConfigureAwait(true);
	}

	/// <summary>
	/// Handles a key press while editing: escape cancels the edit and enter commits it.
	/// </summary>
	private async Task OnEditingKeyDownAsync(KeyboardEventArgs args)
	{
		if (args.Code == "Escape")
		{
			await CancelEdit().ConfigureAwait(true);
		}
		else if (args.Code is "Enter" or "Return")
		{
			await CommitEditAsync().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Handles a key press while not editing: F2 begins an edit, Ctrl+A selects all rows and the navigation
	/// keys move the selection.
	/// </summary>
	private async Task OnBrowsingKeyDownAsync(KeyboardEventArgs args)
	{
		switch (args.Code)
		{
			case "F2":
				await BeginEditAsync().ConfigureAwait(true);
				break;

			case "KeyA":
				await SelectAllRowsAsync(args).ConfigureAwait(true);
				break;

			default:
				if (IsNavigationKey(args.Code))
				{
					await MoveSelectionAsync(args.Code).ConfigureAwait(true);
				}

				break;
		}
	}

	/// <summary>
	/// Determines whether a key code is one of the keys that move the selection.
	/// </summary>
	private static bool IsNavigationKey(string code)
		=> code is "ArrowUp" or "ArrowDown" or "PageUp" or "PageDown" or "Home" or "End";

	/// <summary>
	/// Selects every displayed row when Ctrl+A is pressed in a multiple selection table.
	/// </summary>
	private async Task SelectAllRowsAsync(KeyboardEventArgs args)
	{
		if (args.CtrlKey && SelectionMode == TableSelectionMode.Multiple)
		{
			await ClearSelectionAsync().ConfigureAwait(true);
			Selection.AddRange(ItemsToDisplay.Select(x => KeyField!(x).ToString() ?? string.Empty));
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Moves a single selection in response to a navigation key.
	/// </summary>
	private async Task MoveSelectionAsync(string code)
	{
		if (Selection.Count == 1 && KeyField != null)
		{
			var idx = ItemsToDisplay.FindIndex(x => KeyField(x).ToString() == Selection[0]);
			if (idx > -1)
			{
				var target = GetNavigationTarget(code, idx, ItemsToDisplay.Count);
				if (target.HasValue)
				{
					await SelectAndScrollToAsync(ItemsToDisplay[target.Value]).ConfigureAwait(true);
				}

				await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
			}
		}
	}

	/// <summary>
	/// Gets the index of the row a navigation key moves the selection to, or null when it does not move.
	/// </summary>
	/// <param name="code">The navigation key code.</param>
	/// <param name="idx">The index of the selected row.</param>
	/// <param name="count">The number of rows.</param>
	private static int? GetNavigationTarget(string code, int idx, int count)
	{
		return code switch
		{
			"End" => idx < count - 1 ? count - 1 : null,
			"Home" => idx > 0 ? 0 : null,
			_ => GetStepTarget(code, idx, count)
		};
	}

	/// <summary>
	/// Gets the index of the row an up or down key (one row, or ten for page keys) moves the selection to,
	/// or null when that would move it past the first or last row.
	/// </summary>
	/// <param name="code">The up or down key code.</param>
	/// <param name="idx">The index of the selected row.</param>
	/// <param name="count">The number of rows.</param>
	private static int? GetStepTarget(string code, int idx, int count)
	{
		var stepSize = code.StartsWith("Page", StringComparison.Ordinal) ? 10 : 1;
		if (code.EndsWith("Up", StringComparison.Ordinal))
		{
			return idx >= stepSize ? idx - stepSize : null;
		}

		return idx < count - stepSize ? idx + stepSize : null;
	}

	/// <summary>
	/// Makes the given row the only selected one and scrolls it into view.
	/// </summary>
	private async Task SelectAndScrollToAsync(TItem item)
	{
		var id = KeyField!(item).ToString();
		Selection.Clear();
		if (!string.IsNullOrWhiteSpace(id))
		{
			Selection.Add(id);
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("scrollIntoView", id, false).ConfigureAwait(true);
			}
		}
	}
}
