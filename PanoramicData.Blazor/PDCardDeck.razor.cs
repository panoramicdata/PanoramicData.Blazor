namespace PanoramicData.Blazor;

public partial class PDCardDeck<TItem>
{
	private bool _dropAbove;
	private TItem? _dragTarget;
	private TItem? _draggedItem;
	private static int _sequence;
	private readonly List<TItem> _selection = new();

	[Parameter]
	public EventCallback<IEnumerable<TItem>> AddItems { get; set; }

	[Parameter]
	public string Id { get; set; } = $"pd-toggleswitch-{++_sequence}";

	[Parameter]
	public int CardHeight { get; set; } = 32;

	[Parameter]
	public RenderFragment<TItem>? CardTemplate { get; set; }

	[Parameter]
	public RenderFragment? DropAreaTemplate { get; set; }

	[EditorRequired]
	[Parameter]
	public IList<TItem> Items { get; set; } = new List<TItem>();

	[Parameter]
	public EventCallback<IEnumerable<TItem>> ItemsAdded { get; set; }

	[Parameter]
	public EventCallback<IEnumerable<TItem>> ItemsRemoved { get; set; }

	[Parameter]
	public bool MultipleSelection { get; set; }

	public IEnumerable<TItem> GetSelection() => _selection;

	private IDictionary<string, object?> GetCardAttributes(TItem item)
	{
		var dict = new Dictionary<string, object?>
		{
			{ "class", $"card {(_selection.Contains(item) ? "selected" : "")} {(_draggedItem?.Equals(item) == true ? "dragging" : "")}" },
			{ "ondragend", (DragEventArgs e)=> OnDragEnd(e, item) },
			{ "ondragenter", (DragEventArgs e)=> OnDragEnter(e, item) },
			{ "ondragover", (DragEventArgs e) => OnDragOver(e, item) },
			{ "ondragstart", (DragEventArgs e) => OnDragStart(e, item) },
			{ "ondrop", (DragEventArgs e) => OnDropAsync(e) },
			{ "onmousedown", (MouseEventArgs e) => OnItemMouseDownAsync(item, e) }
		};
		if (IsEnabled)
		{
			dict.Add("draggable", @"true");
		}
		return dict;
	}

	private IDictionary<string, object?> GetDeckAttributes()
	{
		var dict = new Dictionary<string, object?>
		{
			{ "class", $"pdcarddeck {CssClass}{(IsEnabled ? "" : " disabled")} {SizeCssClass}" },
			{ "id", Id }
		};
		return dict;
	}

	private IDictionary<string, object?> GetDropAreaAttributes()
	{
		return new Dictionary<string, object?>
		{
			{ "class", "droparea" },
			{ "ondrop", (DragEventArgs e) => OnDropAsync(e) }
		};
	}

	private void OnDragEnd(DragEventArgs args, TItem item)
	{
		_draggedItem = default;
		_dragTarget = default;
		StateHasChanged();
	}

	private void OnDragEnter(DragEventArgs args, TItem item)
	{
		_dragTarget = item;
		_dropAbove = args.OffsetY <= (CardHeight / 2d);
		StateHasChanged();
	}

	private void OnDragOver(DragEventArgs args, TItem item)
	{
		var newDropAbove = args.OffsetY <= (CardHeight / 2d);
		if (newDropAbove != _dropAbove)
		{
			_dropAbove = newDropAbove;
			StateHasChanged();
		}
	}

	private void OnDragStart(DragEventArgs args, TItem item)
	{
		// ensure dragged item is selected
		_draggedItem = item;
		_dragTarget = item;
		_dropAbove = args.OffsetY <= (CardHeight / 2d);
		StateHasChanged();
	}

	private async Task OnDropAsync(DragEventArgs args)
	{
		// default action is to re-order
		if (_draggedItem != null && _dragTarget != null)
		{
			var list = Items.ToList();

			// index to insert at
			var idx = list.IndexOf(_dragTarget);

			// remove selection
			foreach (var item in _selection)
			{
				list.Remove(item);
				Items.Remove(item);
			}

			// notify app
			await ItemsRemoved.InvokeAsync(_selection).ConfigureAwait(true);

			// validate bounds
			if (idx > list.Count - 1)
			{
				idx = list.Count;
			}
			if (idx < 0)
			{
				idx = 0;
			}

			foreach (var item in _selection.Reverse<TItem>())
			{
				Items.Insert(idx, item);
			}

			// notify app to add items from source
			await ItemsAdded.InvokeAsync(_selection).ConfigureAwait(true);
		}
	}

	private Task OnItemMouseDownAsync(TItem item, MouseEventArgs args)
	{
		UpdateSelection(item, args.CtrlKey, args.ShiftKey);
		StateHasChanged();
		return Task.CompletedTask;
	}

	private void UpdateSelection(TItem item, bool ctrl, bool shift)
	{
		if (IsEnabled)
		{
			if (MultipleSelection)
			{
				if (ctrl) // add/remove single
				{
					if (_selection.Contains(item))
					{
						_selection.Remove(item);
					}
					else
					{
						_selection.Add(item);
					}
				}
				else if (shift && _selection.Count > 0) // add range
				{
					var list = Items.ToList();
					var sIdx = list.IndexOf(_selection.First());
					var eIdx = list.IndexOf(item);
					var s = Math.Min(sIdx, eIdx);
					var e = Math.Max(sIdx, eIdx);
					_selection.Clear();
					_selection.AddRange(list.GetRange(s, e - s + 1));
				}
				else
				{
					_selection.Clear();
					_selection.Add(item);
				}
			}
			else
			{
				_selection.Clear();
				_selection.Add(item);
			}
		}
	}

	private string SizeCssClass => Size switch
	{
		ButtonSizes.Small => "sm",
		ButtonSizes.Large => "lg",
		_ => "md"
	};
}
