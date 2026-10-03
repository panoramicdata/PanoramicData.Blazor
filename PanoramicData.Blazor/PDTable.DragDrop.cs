namespace PanoramicData.Blazor;

public partial class PDTable<TItem>
{
	private async Task OnRowDragStart(DragEventArgs args, TItem? rowItem)
	{
		if (!IsEnabled || IsEditing)
		{
			return;
		}

		_dragging = true;

		// need to set the data being dragged
		if (DragContext != null && KeyField != null)
		{
			// if item that initiated drag is in selection then drag entire selection
			// otherwise change selection to single item and drag that
			if (rowItem != null)
			{
				var key = KeyField(rowItem).ToString() ?? string.Empty;
				if (!Selection.Contains(key))
				{
					await SelectItemAsync(key, args.ShiftKey, args.CtrlKey);
				}
			}

			DragContext.Payload = GetSelectedItemsInSelectionOrder();
		}
	}

	/// <summary>
	/// Gets the displayed items that are selected, in the order they were selected.
	/// </summary>
	private List<TItem> GetSelectedItemsInSelectionOrder()
	{
		var items = new List<TItem>();
		foreach (var key in Selection)
		{
			var item = ItemsToDisplay.Find(x => KeyField!(x)?.ToString() == key);
			if (item != null)
			{
				items.Add(item);
			}
		}

		return items;
	}

	private void OnDragEnd() => _dragging = false;

	private async Task OnRowDragDropAsync(MouseEventArgs args, TItem row)
	{
		if (IsEnabled && DragContext != null)
		{
			await Drop.InvokeAsync(new DropEventArgs(row, DragContext.Payload, args.CtrlKey)).ConfigureAwait(true);
		}
	}

	private async Task OnDragDropAsync(DragEventArgs args)
	{
		if (IsEnabled && DragContext != null)
		{
			await Drop.InvokeAsync(new DropEventArgs(null, DragContext.Payload, args.CtrlKey)).ConfigureAwait(true);
		}
	}
}
