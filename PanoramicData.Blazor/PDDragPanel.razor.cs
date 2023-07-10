namespace PanoramicData.Blazor;

public partial class PDDragPanel<TItem> where TItem : class
{
	private IJSObjectReference? _module;
	private List<TItem> _localItems = new();
	private double _lastY = 0;

	[Inject]
	private IJSRuntime? JSRuntime { get; set; }

	[CascadingParameter]
	public PDDragContainer<TItem>? Container { get; set; }

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

	private void OnMouseDown(MouseEventArgs args, TItem? item)
	{

	}

	protected override void OnParametersSet()
	{
		if (Container != null)
		{
			_localItems = Container.Items.ToList(); // initial order
		}
	}

	private async Task OnDragStartAsync(DragEventArgs args, TItem? item)
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
			if (Container.Payload != item)
			{
				Console.WriteLine($"dragging {Container.Payload} over: {item} ({args.ClientY})");

				// re-order
				_localItems.Remove(Container.Payload);

				// dragging up or down?
				if (args.ClientY > _lastY)
				{
					_localItems.Insert(_localItems.IndexOf(item) + 1, Container.Payload);
				}
				else
				{
					_localItems.Insert(_localItems.IndexOf(item), Container.Payload);
				}

				_lastY = args.ClientY;
			}
		}
	}

	private async Task OnDragEnd(DragEventArgs args, TItem? item)
	{
		Console.WriteLine($"drag ended");

		if (Container != null)
		{
			Container.Payload = null;
			if (item != null)
			{
				await Container.ItemReOrderedAsync(item);
			}
		}

	}

}
