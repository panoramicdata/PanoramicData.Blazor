namespace PanoramicData.Blazor;

/// <summary>
/// PDDragPanel: reordering the items by drag-and-drop.
/// </summary>
public partial class PDDragPanel<TItem> where TItem : class
{
	private double _lastY;

	private void OnDragStart(DragEventArgs args, TItem? item)
	{
		if (Container != null)
		{
			Container.Payload = item;
			_lastY = args.ClientY;
		}
	}

	private void OnDragEnter(DragEventArgs args, TItem? item)
	{
		if (item != null && Container?.Payload != null && CanChangeOrder && Container.Payload != item)
		{
			// new location depends on whether dragging up or down?
			_localItems.Remove(Container.Payload);
			_localItems.Insert(_localItems.IndexOf(item) + (args.ClientY > _lastY ? 1 : 0), Container.Payload);
			_lastY = args.ClientY;
		}
	}

	private async Task OnDragEndAsync()
	{
		if (Container?.Payload != null)
		{
			// has order changed?
			if (CanChangeOrder)
			{
				var originalOrder = Container.Items.ToArray();
				if (!originalOrder.SequenceEqual([.. _localItems]))
				{
					await ItemOrderChanged.InvokeAsync(new DragOrderChangeArgs<TItem>(_localItems, Container.Payload));
				}
			}

			// reset
			Container.Payload = null;
		}
	}
}
