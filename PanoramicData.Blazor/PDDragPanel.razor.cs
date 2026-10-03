namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that renders a list of draggable items and supports reordering via drag-and-drop.
/// </summary>
/// <typeparam name="TItem">The type of item in the panel.</typeparam>
public partial class PDDragPanel<TItem> where TItem : class
{
	private List<TItem> _localItems = [];

	/// <summary>
	/// Gets the injected JavaScript runtime.
	/// </summary>
	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets whether the order of items can be changed.
	/// </summary>
	[Parameter]
	public bool CanChangeOrder { get; set; } = true;

	/// <summary>
	/// Gets or sets whether items can be dragged.
	/// </summary>
	[Parameter]
	public bool CanDrag { get; set; } = true;

	/// <summary>
	/// Gets or sets the cascading parent drag container.
	/// </summary>
	[CascadingParameter]
	public PDDragContainer<TItem>? Container { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier for the panel.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-dragpanel-{PDDragPanelSequence.Next()}";

	/// <summary>
	/// An event callback that is invoked when the order of items changes.
	/// </summary>
	[Parameter]
	public EventCallback<DragOrderChangeArgs<TItem>> ItemOrderChanged { get; set; }

	/// <summary>
	/// A template for rendering each item.
	/// </summary>
	[Parameter]
	public RenderFragment<TItem>? Template { get; set; }

	/// <summary>
	/// A template for rendering the placeholder when an item is being dragged.
	/// </summary>
	[Parameter]
	public RenderFragment<TItem>? PlaceholderTemplate { get; set; }

	private Dictionary<string, object> GetItemAttributes(TItem? item)
	{
		var dict = new Dictionary<string, object>
		{
			{ "class", $"pd-dragitem {(item == Container?.Payload ? "dragging" : "")}" }
		};
		if (CanDrag)
		{
			dict.Add("draggable", "true");
			dict.Add("style", "cursor: move;");
		}

		return dict;
	}

	private IEnumerable<TItem> DisplayItems => _localItems;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (Container != null)
		{
			_localItems = [.. Container.Items]; // initial order
		}
	}

	private async Task OnSelectionChanged()
	{
		if (Container != null)
		{
			await Container.OnSelectionChangedAsync();
		}
	}
}
