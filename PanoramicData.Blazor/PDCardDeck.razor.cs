using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

public partial class PDCardDeck<TCard> where TCard : ICard
{
	private static int _sequence;

	/// <inheritdoc/>
	private readonly PDCardDeckSelectionHelper<TCard> _selectionHelper = new();

	/// <inheritdoc/>
	public PDCardDragDropInformation DragState { get; private set; } = new();

	// Reference Capture for Blazor Sub Components
	private List<PDCard<TCard>> _cards = [];

	public PDCard<TCard> Ref { set => _cards.Add(value); }

	/// <summary>
	/// The Card(s) that have been selected by the user
	/// </summary>
	public List<TCard> Selection { get; private set; } = [];

	/// <summary>
	/// A collection of cards that are currently in the deck.
	/// </summary>
	public List<TCard> Cards { get; private set; } = [];

	private IDataProviderService<TCard> _dataProviderService = new EmptyDataProviderService<TCard>();

	#region Parameters

	[Parameter]
	public override string Id { get; set; } = $"pd-carddeck-{++_sequence}";

	/// <summary>
	/// Whether the deck has animations enabled or not. Defaults to false.
	/// </summary>
	[Parameter]
	public bool IsAnimated { get; set; }

	[Parameter]
	public RenderFragment<TCard>? CardTemplate { get; set; }

	/// <summary>
	/// Global CSS Class to outline the styling of each Card
	/// </summary>
	[Parameter]
	public Func<TCard, string>? CardCss { get; set; }

	[EditorRequired]
	[Parameter]
	public IDataProviderService<TCard> DataProvider { get; set; } = new EmptyDataProviderService<TCard>();

	/// <summary>
	/// Whether the deck has multiple selection enabled or not. Defaults to false.
	/// </summary>
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

	/// <summary>
	/// The Event Handlers that are used to handle drag and drop events for this deck.
	/// </summary>
	[CascadingParameter(Name = "cardDeckGroupContext")]
	/// <inheritdoc cref="DeckGroupContext{TCard}"/>
	public DeckGroupContext<TCard> GroupContext { get; set; } = new(false);

	#endregion

	private Dictionary<string, object?> GetDeckAttributes()
	{
		var dict = new Dictionary<string, object?>
		{
			{ "class", $"{CssClass ?? "pdcarddeck"} {(IsEnabled ? "" : " disabled")} {SizeCssClass}" },
			{ "id", Id },
			{ "ondrop", (MouseEventArgs _) => DragState.IsDragging = false }
		};

		// If part of a group, add the drag enter and drag leave methods
		if (GroupContext.IsMemberOfGroup)
		{

			if (GroupContext.OnDragEntered is null)
			{
				// TODO: Log that the events haven't been set up correctly
				return dict;
			}

			// Add the on drag enter event handler
			dict.Add("ondragenter",
			async (MouseEventArgs _) => await GroupContext.OnDragEntered!(this));


		}
		return dict;
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

		await GetCardsFromDataProviderAsync();
	}

	internal async Task AddToSelectionAsync(MouseEventArgs args, TCard card)
	{
		// Cannot be dragging while adding to selection
		DragState.IsDragging = false;

		// No point in adding to selection if multiple selection is not enabled
		if (!MultipleSelection)
		{
			return;
		}

		if (args.ShiftKey)
		{
			Selection = _selectionHelper
				.HandleAddRange(Selection, Cards, card);
		}

		else if (args.CtrlKey)
		{
			Selection = _selectionHelper
				.HandleIndividualAddRemove(Selection, Cards, card);
		}
		else
		{
			Selection = _selectionHelper
				.HandleSingleSelect(Selection, Cards, card);
		}
		StateHasChanged();

		// Notify the parent component that a selection has been made (is part of a group)
		if (GroupContext.IsMemberOfGroup && GroupContext.OnSelect != null)
		{
			await GroupContext.OnSelect(this);
		}

		await OnSelect.InvokeAsync(args);
	}

	internal async Task OnDragStartAsync(DragEventArgs e, TCard card)
	{
		// Handles edge case where the user drags a card that is not in the selection
		if (!MultipleSelection || !Selection.Contains(card))
		{
			Selection.Clear();
			Selection.Add(card);
		}

		DragState.IsDragging = true;

		// Notify the parent component that a selection has been made (is part of a group)
		if (GroupContext.IsMemberOfGroup && GroupContext.OnSelect != null)
		{
			await GroupContext.OnSelect(this);
		}

		// To Prevent the elements using incorrect positions (E.g browser window scroll, resizing)
		ClearAnimationPositions();

		await InvokeAsync(StateHasChanged);
	}

	internal Task OnDragEndAsync(DragEventArgs e, TCard card)
	{
		DragState.Reset();
		StateHasChanged();

		return OnRearrange.InvokeAsync(e);
	}

	internal async Task NotifyDragPositionAsync(DragEventArgs e, TCard card)
	{
		var cardIndex = Cards.IndexOf(card);

		// Cannot find Card
		if (cardIndex == -1 || DragState.TargetIndex == cardIndex)
		{
			return;
		}

		var movingDown = false;

		// Check direction of the change of drop target
		if (DragState.TargetIndex < cardIndex)
		{
			movingDown = true;
		}

		DragState.TargetIndex = cardIndex;

		await UpdateCardPositionsAsync(movingDown);
	}
	/// <summary>
	/// Updates the positions of the selected cards in the deck based on the current drag state.
	/// </summary>
	/// <returns></returns>
	private async Task UpdateCardPositionsAsync(bool movingDown)
	{
		await UpdateAnimationPositionsAsync()
			.ConfigureAwait(false);

		var destination = DragState.TargetIndex;

		if (destination < 0 || destination >= Cards.Count)
		{
			return; // Invalid index
		}

		// If the target index is contained in the selection, do nothing
		if (Selection.Any(card => Cards.IndexOf(card) == destination))
		{
			return;
		}

		// Remove Moving Cards
		var movingCards = Selection
			.Where(card => Cards.IndexOf(card) != -1)
			.ToList();

		// Check if the cards being removed are contiguous and we are moving down, if so, we need to adjust the destination as cards have been removed
		if (IsContiguous(movingCards) && movingDown)
		{
			destination = Math.Max(0, destination - (movingCards.Count - 1));
		}

		foreach (var card in movingCards)
		{
			Cards.Remove(card);
		}

		// Swap positions of the selected cards with the card at the target index
		for (int iteration = 0; iteration < Selection.Count; iteration++)
		{
			var card = Selection[iteration];

			// Clamp the destination index to ensure it does not exceed the bounds of the list
			var destinationIndex = Math.Min(destination + iteration, Cards.Count);

			Cards.Insert(destinationIndex, card);
		}

		await InvokeAsync(StateHasChanged)
			.ConfigureAwait(false);

		UpdateCardDeckPositions();

		await AnimateToPositionsAsync();

		await SyncDataProviderCardsAsync();
	}

	private bool IsContiguous(List<TCard> movingCards)
	{
		var orderedCards = movingCards
			.OrderBy(Cards.IndexOf)
			.ToList();

		for (int i = 0; i < movingCards.Count; i++)
		{
			if (!movingCards[i].Equals(orderedCards[i]))
			{
				return false; // Cards are not in order
			}
		}

		return true;
	}

	internal async Task AddCardsAsync(List<TCard> selection)
	{
		// Create the new cards
		for (int index = 0; index < selection.Count; index++)
		{
			var card = selection[index];

			Cards.Insert(ClampBounds(index + DragState.TargetIndex), card);
			Selection.Add(card);
		}

		UpdateCardDeckPositions();
		ClearAnimationPositions();
		await SyncDataProviderCardsAsync();

		await InvokeAsync(StateHasChanged);
	}

	/// <summary>
	/// Clamps the bounds of an index to ensure it does not exceed the bounds of the card list.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	private int ClampBounds(int index)
	{
		var dragIndex = Math.Min(index, Cards.Count);
		dragIndex = Math.Max(dragIndex, 0);

		return dragIndex;
	}

	private void UpdateCardDeckPositions()
	{
		for (int index = 0; index < Cards.Count; index++)
		{
			var card = Cards[index];
			card.DeckPosition = index;
		}
	}

	internal async Task RemoveSelectedCardsAsync()
	{
		foreach (var card in Selection)
		{
			Cards.Remove(card);
		}

		Selection.Clear();
		UpdateCardDeckPositions();
		ClearAnimationPositions();

		await SyncDataProviderCardsAsync();
	}

	#region Animation Methods
	private async Task UpdateAnimationPositionsAsync()
	{
		if (!IsAnimated)
		{
			return;
		}

		foreach (var card in _cards)
		{
			await card.AnimationHandler.UpdatePositionAsync();
		}
	}

	private void ClearAnimationPositions()
	{
		if (!IsAnimated)
		{
			return;
		}

		foreach (var card in _cards)
		{
			card.AnimationHandler.ClearPositions();
		}
	}

	private async Task AnimateToPositionsAsync()
	{
		if (!IsAnimated)
		{
			return;
		}

		foreach (var card in _cards)
		{
			if (!Selection.Contains(card.Card))
			{
				await card.AnimationHandler.AnimateElementAsync();
			}
		}
	}

	#endregion

	#region Data Provider CRUD Methods

	/// <summary>
	/// Fetches the set of cards that are associated with the Data Provider
	/// </summary>
	/// <returns></returns>
	private async Task GetCardsFromDataProviderAsync()
	{
		if (!ValidDataContext())
		{
			return;
		}

		var dataResponse = await _dataProviderService.GetDataAsync(new(), default);

		Cards = [.. dataResponse.Items];
	}

	private async Task SyncDataProviderCardsAsync()
	{
		if (!ValidDataContext())
		{
			return;
		}

		var dataResponse = await _dataProviderService.GetDataAsync(new(), default);

		var dataProviderCards = dataResponse.Items;

		// Checks against Local Data
		foreach (var card in Cards)
		{
			var dataProviderCard = dataProviderCards.FirstOrDefault(c => c.Id == card.Id);

			// Handle the card being present on the local but not the data provider
			if (dataProviderCard is null)
			{
				await AddCardToDataProviderAsync(card);
				continue;
			}

			// Handle the card being present on both but the position is different, update the position
			if (dataProviderCard.DeckPosition != card.DeckPosition)
			{
				await UpdateCardDeckPositionAsync(dataProviderCard, card.DeckPosition);
				continue;
			}
			// Position is the same, no need to update it
		}

		// Cards Present in dataprovider that aren't in local
		var missingCards = dataProviderCards
			.Where(c => !Cards.Any(localCard => localCard.Id == c.Id))
			.ToList();

		foreach (var card in missingCards)
		{
			// Remove cards from the data provider that are not in the local deck
			var deleteResponse = await _dataProviderService.DeleteAsync(card, default);

			if (!deleteResponse.Success)
			{
				// TODO: Log the delete failure
				continue;
			}

		}
	}

	private async Task UpdateCardDeckPositionAsync(TCard card, int? deckPosition)
	{
		var updateResponse = await _dataProviderService.UpdateAsync(card, new Dictionary<string, object?>
		{
			{ nameof(card.DeckPosition), deckPosition }
		}, default);

		if (!updateResponse.Success)
		{
			// TODO: Log the update failure
		}
	}

	private async Task AddCardToDataProviderAsync(TCard card)
	{
		// Need to add it and update its position
		var createResponse = await _dataProviderService.CreateAsync(card, default);

		if (!createResponse.Success)
		{
			// TODO: Log the create failure
			return;
		}

		await UpdateCardDeckPositionAsync(card, card.DeckPosition);
	}

	/// <summary>
	/// Ensures that Local Data is valid before performing any operations on the data source
	/// </summary>
	/// <returns></returns>
	private bool ValidDataContext()
	{
		if (_dataProviderService is null)
		{
			// TODO: Log that the data provider service is not set
			return false;
		}

		// Check for duplicate DeckPositions in the current deck, ignoring nulls
		var duplicatedPosition = Cards
			.Where(c => c.DeckPosition != null)
			.GroupBy(c => c.DeckPosition)
			.Any(g => g.Count() > 1);

		if (duplicatedPosition)
		{
			// TODO: Log that the local data is invalid
			return false;
		}

		return true;
	}

	#endregion

	private string SizeCssClass => Size switch
	{
		ButtonSizes.Small => "sm",
		ButtonSizes.Large => "lg",
		_ => "md"
	};
}
