namespace PanoramicData.Blazor;

public partial class PDCardDeck<TCard> where TCard : ICard
{
	private bool _dropAbove;
	private TCard? _dragTarget;
	private TCard? _draggedCard;
	private static int _sequence;
	private readonly List<TCard> _selection = [];
	private List<TCard> _cards = [];
	private IDataProviderService<TCard> _dataProviderService = new EmptyDataProviderService<TCard>();

	[Parameter]
	public string CardDeckId { get; set; } = $"pd-carddeck-{++_sequence}";

	[Parameter]
	public int CardHeight { get; set; } = 32;

	[Parameter]
	public RenderFragment<TCard>? CardTemplate { get; set; }

	[EditorRequired]
	[Parameter]
	public IDataProviderService<TCard> DataProvider { get; set; } = new EmptyDataProviderService<TCard>();

	[Parameter]
	public RenderFragment? DropAreaTemplate { get; set; }

	[Parameter]
	public bool MultipleSelection { get; set; }

	public IEnumerable<TCard> GetSelection() => _selection;

	private IDictionary<string, object?> GetCardAttributes(TCard card)
	{
		var dict = new Dictionary<string, object?>
		{
			{ "class", $"card {(_selection.Contains(card) ? "selected" : "")} {(_draggedCard?.Equals(card) == true ? "dragging" : "")}" },
			{ "ondragend", (DragEventArgs e)=> OnDragEnd(e, card) },
			{ "ondragenter", (DragEventArgs e)=> OnDragEnter(e, card) },
			{ "ondragover", (DragEventArgs e) => OnDragOver(e, card) },
			{ "ondragstart", (DragEventArgs e) => OnDragStart(e, card) },
			{ "ondrop", (DragEventArgs e) => OnDropAsync(e) },
			{ "onmousedown", (MouseEventArgs e) => OnItemMouseDownAsync(card, e) }
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
			{ "id", CardDeckId }
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

	private void OnDragEnd(DragEventArgs args, TCard card)
	{
		_draggedCard = default;
		_dragTarget = default;
		StateHasChanged();
	}

	private void OnDragEnter(DragEventArgs args, TCard card)
	{
		_dragTarget = card;
		_dropAbove = args.OffsetY <= (CardHeight / 2d);
		StateHasChanged();
	}

	private void OnDragOver(DragEventArgs args, TCard card)
	{
		var newDropAbove = args.OffsetY <= (CardHeight / 2d);
		if (newDropAbove != _dropAbove)
		{
			_dropAbove = newDropAbove;
			StateHasChanged();
		}
	}

	private void OnDragStart(DragEventArgs args, TCard card)
	{
		// ensure dragged item is selected
		_draggedCard = card;
		_dragTarget = card;
		_dropAbove = args.OffsetY <= (CardHeight / 2d);
		StateHasChanged();
	}

	private async Task OnDropAsync(DragEventArgs args)
	{
		// default action is to re-order
		if (_draggedCard != null && _dragTarget != null)
		{
			// index to insert at
			var idx = _cards.IndexOf(_dragTarget);

			// remove selection
			foreach (var card in _selection)
			{
				_cards.Remove(card);
			}

			// validate bounds
			if (idx > _cards.Count - 1)
			{
				idx = _cards.Count;
			}
			if (idx < 0)
			{
				idx = 0;
			}

			foreach (var card in _selection.Reverse<TCard>())
			{
				_cards.Insert(idx, card);
			}

			// TODO: notify app to add items from source
		}
	}

	private Task OnItemMouseDownAsync(TCard card, MouseEventArgs args)
	{
		UpdateSelection(card, args.CtrlKey, args.ShiftKey);
		StateHasChanged();
		return Task.CompletedTask;
	}

	protected override async Task OnParametersSetAsync()
	{
		if (DataProvider != _dataProviderService)
		{
			_dataProviderService = DataProvider;
			var request = new DataRequest<TCard>();
			var cards = await _dataProviderService.GetDataAsync(request, default).ConfigureAwait(true);

			_cards.Clear();
			_cards.AddRange(cards.Items);
		}
	}

	private void UpdateSelection(TCard card, bool ctrl, bool shift)
	{
		if (IsEnabled)
		{
			if (MultipleSelection)
			{
				if (ctrl) // add/remove single card to current selection
				{
					// If not in selection, add
					if (!_selection.Remove(card))
					{
						_selection.Add(card);
					}
				}
				else if (shift && _selection.Count > 0) // add range
				{
					// Get selected cards
					var firstCardIndex = _cards.IndexOf(_selection.First());
					var lastCardIndex = _cards.IndexOf(card);

					var start = Math.Min(firstCardIndex, lastCardIndex);
					var end = Math.Max(firstCardIndex, lastCardIndex);

					_selection.Clear();
					_selection.AddRange(_cards.GetRange(start, end - start + 1));
				}
				else // Single selection
				{
					_selection.Clear();
					_selection.Add(card);
				}
			}
			else
			{
				_selection.Clear();
				_selection.Add(card);
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
