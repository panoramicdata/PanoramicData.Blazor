namespace PanoramicData.Blazor;

public partial class PDCardDeck<TCard> where TCard : ICard
{
	private static int _sequence;

	/// <summary>
	/// Indicates whether a user is currently dragging cards
	/// </summary>
	private bool _isDragging;

	int _cursorX, _cursorY;
	string _cursorXpx => $"{_cursorX}px";
	string _cursorYpx => $"{_cursorY}px";

	/// <summary>
	/// The Card(s) that have been selected by the user
	/// </summary>
	private readonly List<TCard> _selection = [];

	/// <summary>
	/// A collection of cards that are currently in the deck.
	/// </summary>
	private List<TCard?> _cards = [];

	private IDataProviderService<TCard> _dataProviderService = new EmptyDataProviderService<TCard>();

	#region Parameters

	[Parameter]
	public override string Id { get; set; } = $"pd-carddeck-{++_sequence}";

	[Parameter]
	public string CardHeight { get; set; } = "64px";

	[Parameter]
	public RenderFragment<TCard>? CardTemplate { get; set; }

	[Parameter]
	public Func<TCard, string> CardCss { get; set; } = (card => "");

	[Parameter]
	public Func<string> DropzoneCss { get; set; } = () => "pdcard-dropzone";

	[EditorRequired]
	[Parameter]
	public IDataProviderService<TCard> DataProvider { get; set; } = new EmptyDataProviderService<TCard>();

	[Parameter]
	public RenderFragment? DropAreaTemplate { get; set; }

	[Parameter]
	public bool MultipleSelection { get; set; }

	/// <summary>
	/// Event that is invoked when the user rearranges cards in the deck.
	/// </summary>
	[Parameter]
	public EventCallback<DragEventArgs> OnRearrange { get; set; }

	/// <summary>
	/// Event that is invoked when the user selects a card / cards in the deck.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnSelect { get; set; }

	#endregion

	public IEnumerable<TCard?> GetSelection() => _selection;

	public IEnumerable<TCard?> GetCards() => _cards;

	private Dictionary<string, object?> GetCardAttributes(TCard card)
	{
		var dict = new Dictionary<string, object?>
		{
			{ "class", $"card {CardCss(card)} {(_selection.Contains(card) ? "selected" : "")} {(_selection?.Contains(card) == true && _isDragging ? "dragging" : "")}" },
			{ "ondragend", (DragEventArgs e)=> OnDragEnd(e) },
			{ "ondragover", (DragEventArgs e) => OnDragOver(e, card) },
			{ "ondragstart", (DragEventArgs e) => OnDragStartAsync(e, card) },
			{ "onmouseup", (MouseEventArgs e) => OnItemMouseUpAsync(card, e) },
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
			{ "id", Id },
			{ "onmouseleave", (MouseEventArgs e) => OnDragEnd(new DragEventArgs()) }, // End dragging when mouse leaves the deck
		};
		return dict;
	}

	private void UpdateIndicatorPosition(DragEventArgs e)
	{
		_cursorX = (int)e.ClientX;
		_cursorY = (int)e.ClientY;
	}

	private Dictionary<string, object?> GetDropAreaAttributes()
	{
		return new Dictionary<string, object?>
		{
			{ "class", $"{DropzoneCss()}" },
			{ "ondrop", (DragEventArgs e) => OnDropAsync(e) },
		};
	}

	private async Task OnDropAsync(DragEventArgs args)
	{
		// Cannot move cards if it is not dragging
		if (!_isDragging)
		{
			return;
		}

		MoveCards();

		await OnRearrange.InvokeAsync(args);

		// TODO: notify app to add items from source
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
			var index = _cards.IndexOf(default!);

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

		// Indicate to the user where the cards will be dropped
		UpdateIndicatorPosition(args);

		// Shift the null values to the card
		var hoveredIndex = _cards.IndexOf(card);

		if (hoveredIndex == -1)
		{
			hoveredIndex = _cards.Count - 1; // If the card is not found, place it at the end
		}

		// Remove nulls, put them under the hovered card
		while (_cards.Contains(default!))
		{
			_cards.Remove(default!);
		}

		foreach (var _ in _selection)
		{
			// Ensures that it is within bounds
			var minIndex = Math.Min(hoveredIndex, _cards.Count);

			_cards.Insert(minIndex, default!);
		}

		StateHasChanged();
	}

	private async Task OnDragStartAsync(DragEventArgs args, TCard card)
	{
		_isDragging = true;

		// Indicate to the user where the cards will be dropped
		UpdateIndicatorPosition(args);

		// ensure dragged item is selected
		if (_selection.Count == 0 || !_selection.Contains(card))
		{
			// Add the card to the selection
			await UpdateSelectionAsync(args, card);
		}

		// Replace the moving cards with nulls to indicate they have been moved
		foreach (var selectedCard in _selection)
		{
			var index = _cards.IndexOf(selectedCard!);
			if (index >= 0)
			{
				_cards[index] = default!;
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
	private async Task OnItemMouseUpAsync(TCard card, MouseEventArgs args)
	{
		await UpdateSelectionAsync(args, card);
		StateHasChanged();
	}

	/// <summary>
	/// Set the Data Provider and get the initial data set
	/// </summary>
	/// <returns></returns>
	protected override async Task OnParametersSetAsync()
	{
		// Cannot fetch data if the data provider is not set
		if (DataProvider == _dataProviderService)
		{
			return;
		}

		_dataProviderService = DataProvider;

		var request = new DataRequest<TCard>();
		var cards = await _dataProviderService.GetDataAsync(request, default).ConfigureAwait(true);

		_cards.Clear();
		_cards.AddRange(cards.Items);

	}

	private async Task UpdateSelectionAsync(MouseEventArgs args, TCard card)
	{
		// Not enabled, cannot select
		if (!IsEnabled)
		{
			return;
		}

		if (MultipleSelection)
		{
			if (args.CtrlKey) // add/remove single card to current selection
			{
				// If not in selection, add
				if (!_selection.Remove(card))
				{
					_selection.Add(card);
				}
			}
			else if (args.ShiftKey && _selection.Count > 0) // add range
			{
				// Get selected cards
				var firstCardIndex = _cards.IndexOf(_selection.First() ?? card);
				var lastCardIndex = _cards.IndexOf(card);

				var start = Math.Min(firstCardIndex, lastCardIndex);
				var end = Math.Max(firstCardIndex, lastCardIndex);

				_selection.Clear();
				_selection.AddRange(_cards.GetRange(start, end - start + 1)!);
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

		await OnSelect.InvokeAsync(args);
	}

	private string SizeCssClass => Size switch
	{
		ButtonSizes.Small => "sm",
		ButtonSizes.Large => "lg",
		_ => "md"
	};
}
