namespace PanoramicData.Blazor;

public partial class PDDragPanel<TItem> where TItem : class
{
	private IJSObjectReference? _module;
	private List<TItem> _localItems = new();
	private double _lastY;

	[Inject]
	private IJSRuntime? JSRuntime { get; set; }

	[Parameter]
	public bool CanChangeOrder { get; set; } = true;

	[CascadingParameter]
	public PDDragContainer<TItem>? Container { get; set; }

	[Parameter]
	public EventCallback<DragOrderChangeArgs<TItem>> ItemOrderChanged { get; set; }

	[Parameter]
	public RenderFragment<TItem>? Template { get; set; }

	[Parameter]
	public RenderFragment<TItem>? PlaceholderTemplate { get; set; }

	private Dictionary<string, object> GetItemAttributes(TItem? item)
	{
		var dict = new Dictionary<string, object>()
		{
			{ "class", $"pd-dragitem {(item == Container?.Payload ? "dragging" : "")}" }
		};
		if ((item is IDragItem dragItem && dragItem.CanDrag) || true)
		{
			dict.Add("draggable", "true");
			dict.Add("style", "cursor: move;");
		}
		return dict;
	}

	private IEnumerable<TItem> DisplayItems
	{
		get
		{
			return _localItems;
		}
	}

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime != null)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDDragPanel.razor.js").ConfigureAwait(true);
		}
	}

	protected override void OnParametersSet()
	{
		if (Container != null)
		{
			_localItems = Container.Items.ToList(); // initial order
		}
	}

	private void OnDragStart(DragEventArgs args, TItem? item)
	{
		if (Container != null && ((item is IDragItem dragItem && dragItem.CanDrag) || true))
		{
			Container.Payload = item;
			_lastY = args.ClientY;
		}
	}

	private void OnDragEnter(DragEventArgs args, TItem? item)
	{
		if (item != null && Container?.Payload != null)
		{
			// re-order?
			if (CanChangeOrder && Container.Payload != item)
			{
				// new location depends on whether dragging up or down?
				_localItems.Remove(Container.Payload);
				_localItems.Insert(_localItems.IndexOf(item) + (args.ClientY > _lastY ? 1 : 0), Container.Payload);
				_lastY = args.ClientY;
			}
		}
	}

	private async Task OnDragEndAsync(DragEventArgs args, TItem? item)
	{
		if (Container?.Payload != null)
		{
			// has order changed?
			if (CanChangeOrder)
			{
				var originalOrder = Container.Items.ToArray();
				if (!originalOrder.SequenceEqual(_localItems.ToArray()))
				{
					await ItemOrderChanged.InvokeAsync(new DragOrderChangeArgs<TItem>(_localItems, Container.Payload));
				}
			}

			// reset
			Container.Payload = null;
		}
	}

}
