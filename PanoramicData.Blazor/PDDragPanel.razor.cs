namespace PanoramicData.Blazor;

public partial class PDDragPanel<TItem> where TItem : class
{
	private static int _sequence;

	private double _lastY;
	private List<TItem> _localItems = [];
	//private IJSObjectReference? _module;
	//private bool _disposedValue;

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter]
	public bool CanChangeOrder { get; set; } = true;

	[Parameter]
	public bool CanDrag { get; set; } = true;

	[CascadingParameter]
	public PDDragContainer<TItem>? Container { get; set; }

	[Parameter]
	public string Id { get; set; } = $"pd-dragpanel-{++_sequence}";

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
		if (CanDrag && ((item is IDragItem dragItem && dragItem.CanDrag) || true))
		{
			dict.Add("draggable", "true");
			dict.Add("style", "cursor: move;");
		}

		return dict;
	}

	private IEnumerable<TItem> DisplayItems => _localItems;

	//protected override async Task OnAfterRenderAsync(bool firstRender)
	//{
	//	if (firstRender)
	//	{
	//		_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDDragPanel.razor.js").ConfigureAwait(true);
	//		if (_module != null)
	//		{
	//			await _module.InvokeVoidAsync("init", Id);
	//		}
	//	}
	//}

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
				if (!originalOrder.SequenceEqual([.. _localItems]))
				{
					await ItemOrderChanged.InvokeAsync(new DragOrderChangeArgs<TItem>(_localItems, Container.Payload));
				}
			}

			// reset
			Container.Payload = null;
		}
	}


	private async Task OnSelectionChanged(ISelectable _)
	{
		if (Container != null)
		{
			await Container.OnSelectionChangedAsync();
		}
	}

	//#region IDisposable

	//public async ValueTask DisposeAsync()
	//{
	//	if (_module != null)
	//	{
	//		try
	//		{
	//			await _module.DisposeAsync();
	//		}
	//		catch
	//		{
	//		}
	//	}
	//	GC.SuppressFinalize(this);
	//}

	//#endregion
}
