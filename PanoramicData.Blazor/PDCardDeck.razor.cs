namespace PanoramicData.Blazor;

public partial class PDCardDeck<TCard> where TCard : ICard
{
	private static int _sequence;

	/// <summary>
	/// Indicates whether a user is currently dragging cards
	/// </summary>
	private bool _isDragging;

	/// <summary>
	/// The Card(s) that have been selected by the user
	/// </summary>
	private List<TCard?> _selection = [];

	/// <summary>
	/// A backup of the original cards, used to restore the deck when the user drags cards out of bounds
	/// </summary>
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

	public IEnumerable<TCard?> GetSelection() => _selection;

	private Dictionary<string, object?> GetCardAttributes(TCard card)
	{
		var dict = new Dictionary<string, object?>
		{
			{ "class", $"card {(_selection.Contains(card) ? "selected" : "")} {(_selection?.Contains(card) == true && _isDragging ? "dragging" : "")}" },
			{ "ondragend", (DragEventArgs e)=> OnDragEnd(e) },
			{ "ondragover", (DragEventArgs e) => OnDragOver(e, card) },
			{ "ondragstart", (DragEventArgs e) => OnDragStart(e, card) },
			{ "onmouseup", (MouseEventArgs e) => OnItemMouseUpAsync(card, e) }
		};
		if (IsEnabled)
		{
			dict.Add("draggable", @"true");
		}
		return dict;
	}

	private Dictionary<string, object?> GetDeckAttributes()
	{
		var dict = new Dictionary<string, object?>
		{
			{ "class", $"pdcarddeck {CssClass}{(IsEnabled ? "" : " disabled")} {SizeCssClass}" },
			{ "id", CardDeckId },
			{ "onmouseleave", (MouseEventArgs e) => OnDragEnd(new DragEventArgs()) }, // End dragging when mouse leaves the deck
		};
		return dict;
	}

	private Dictionary<string, object?> GetDropAreaAttributes()
	{
		return new Dictionary<string, object?>
		{
			{ "class", "droparea" },
			{ "ondrop", (DragEventArgs e) => OnDropAsync(e) },
		};
	}

	private Task OnDropAsync(DragEventArgs args)
	{
		// Cannot move cards if it is not dragging
		if (!_isDragging)
		{
			return Task.CompletedTask;
		}

		MoveCards();

		// TODO: notify app to add items from source
		return Task.CompletedTask;
	}

	/// <summary>
	/// Ends the dragging action
	/// </summary>
	/// <param name="args"></param>
	/// <param name="card"></param>
	private void OnDragEnd(DragEventArgs args)
	{
		MoveCards();

		StateHasChanged();
	}

	private void MoveCards()
	{
		_isDragging = false;

		// Cannot move if no card is being dragged or no target is set
		if (_selection.Count == 0)
		{
			return;
		}

		// Replace each null with the respective card
		foreach (var selectedCard in _selection)
		{
			var index = _cards.IndexOf(default);

			if (index >= 0)
			{
				_cards[index] = selectedCard;
			}
		}
	}

	/// <summary>
	/// Updates destination for dropping cards
	/// </summary>
	/// <param name="args"></param>
	/// <param name="card"></param>
	private void OnDragOver(DragEventArgs args, TCard card)
	{
		// Cannot move cards if it is not dragging
		if (!_isDragging)
		{
			return;
		}

		// Shift the null values to the card
		var hoveredIndex = _cards.IndexOf(card);

		if (hoveredIndex == -1)
		{
			hoveredIndex = _cards.Count - 1; // If the card is not found, place it at the end
		}

		// Remove nulls, put them under the hovered card
		while (_cards.Contains(default))
		{
			_cards.Remove(default);
		}

		foreach (var _ in _selection)
		{
			// Ensures that it is within bounds
			var minIndex = Math.Min(hoveredIndex, _cards.Count);

			_cards.Insert(minIndex, default);
		}

		StateHasChanged();
	}

	private void OnDragStart(DragEventArgs args, TCard card)
	{
		_isDragging = true;

		// ensure dragged item is selected
		if (_selection.Count == 0 || !_selection.Contains(card))
		{
			// Add the card to the selection
			UpdateSelection(card, args.CtrlKey, args.ShiftKey);
		}

		// Replace the moving cards with nulls to indicate they have been moved
		foreach (var selectedCard in _selection)
		{
			var index = _cards.IndexOf(selectedCard!);
			if (index >= 0)
			{
				_cards[index] = default;
			}
		}

		StateHasChanged();
	}

	/// <summary>
	/// Moves the selected cards to their new positions in the deck
	/// </summary>
	/// <returns></returns>

	/// <summary>
	/// Update Selection
	/// </summary>
	/// <param name="card"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	private Task OnItemMouseUpAsync(TCard card, MouseEventArgs args)
	{
		UpdateSelection(card, args.CtrlKey, args.ShiftKey);
		StateHasChanged();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Set the Data Provider and get the initial data set
	/// </summary>
	/// <returns></returns>
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
					var firstCardIndex = _cards.IndexOf(_selection.First() ?? card);
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

			// Single Selection
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
