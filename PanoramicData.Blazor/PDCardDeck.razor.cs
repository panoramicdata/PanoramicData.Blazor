using PanoramicData.Blazor.Helpers;
using System.Collections.Concurrent;

namespace PanoramicData.Blazor;

public partial class PDCardDeck<TCard> where TCard : ICard
{
	private static int _sequence;

	/// <inheritdoc cref="PDCardDeckSelectionHelper{TCard}"/>
	private readonly PDCardDeckSelectionHelper<TCard> _selectionHelper = new();

	/// <inheritdoc cref="PDCardDragDropInformation"/>
	public PDCardDragDropInformation DragState { get; private set; } = new();

	/// <summary>
	/// References to each PDCardComponent in the Deck. This is used for animations, without this, the cards will not animate correctly.
	/// </summary>
	private readonly ConcurrentBag<PDCard<TCard>> _cards = [];

	/// <summary>
	/// Reference to each PDCardComponent in the Deck. This is used for animations, without this, the cards will not animate correctly.
	/// </summary>
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

	// Javascript Interop
	private IJSObjectReference? _jsModule;
	private ElementReference _elementRef;
	private DotNetObjectReference<PDCardDeck<TCard>>? _dotNetRef;

	[Inject] private ILogger<PDCardDeck<TCard>> _logger { get; set; } = null!;

	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;

	#region Parameters

	/// <summary>
	/// Unique identifier for this Card Deck. If not set, a unique ID will be generated.
	/// </summary>
	[Parameter]
	public override string Id { get; set; } = $"pd-carddeck-{++_sequence}";

	/// <summary>
	/// Whether the deck has animations enabled or not. Defaults to false.
	/// </summary>
	[Parameter]
	public bool IsAnimated { get; set; }

	/// <summary>
	/// Template for rendering each individual Card within this Deck
	/// </summary>
	[Parameter]
	public RenderFragment<TCard>? CardTemplate { get; set; }

	/// <summary>
	/// Template for rendering this Deck
	/// </summary>
	[Parameter]
	public RenderFragment<PDCardDeck<TCard>>? DeckTemplate { get; set; }

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
				_logger.LogError("Drag event has not been correctly set");
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
		if (DataProvider == _dataProviderService)
		{
			return;
		}

		_dataProviderService = DataProvider;

		await GetCardsFromDataProviderAsync();
	}

	protected override async Task OnInitializedAsync()
	{
		_dotNetRef = DotNetObjectReference.Create(this);
		_jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
			"import", "./_content/PanoramicData.Blazor/PDCardDeck.razor.js");

		if (_jsModule != null)
		{
			await _jsModule.InvokeVoidAsync("registerEndDragOperation", _elementRef, _dotNetRef);
		}

		await base.OnInitializedAsync();
	}

	/// <summary>
	/// Ends the drag operation
	/// </summary>
	[JSInvokable]
	public void EndDragOperation()
	{
		DragState.IsDragging = false;
		StateHasChanged();
	}

	#region Card Called Events

	internal async Task AddToSelectionAsync(MouseEventArgs args, TCard card)
	{
		// Cannot be dragging while adding to selection
		DragState.IsDragging = false;

		// No point in adding to selection if multiple selection is not enabled
		if (!MultipleSelection)
		{
			return;
		}

		// Contiguous Multiple Selection
		if (args.ShiftKey)
		{
			Selection = _selectionHelper
				.HandleAddRange(Selection, Cards, card);
		}

		// Single Card Selection / Deselection
		else if (args.CtrlKey)
		{
			Selection = _selectionHelper
				.HandleIndividualAddRemove(Selection, Cards, card);
		}

		// Single Card override selection
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
		ClearAnimationPositions();

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
		if (!DragState.IsDragging)
		{
			return;
		}

		ClearAnimationPositions();
		await UpdateAnimationPositionsAsync();
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

		await AnimateToPositionsAsync();
	}

	#endregion

	/// <summary>
	/// Updates the positions of the selected cards in the deck based on the current drag state.
	/// </summary>
	/// <returns></returns>
	private async Task UpdateCardPositionsAsync(bool movingDown)
	{

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
		await SyncDataProviderCardsAsync();
	}

	/// <summary>
	/// Checks if the Selection is contiguous in the deck.
	/// </summary>
	/// <param name="movingCards"></param>
	/// <returns></returns>
	private bool IsContiguous(List<TCard> movingCards)
	{
		// order the cards by their index in the main Cards list
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

	/// <summary>
	/// Adds a set of cards to the deck at the specified index, updating the selection and card positions accordingly. 
	/// </summary>
	/// <param name="selection"></param>
	/// <returns></returns>
	/// <remarks>
	/// Used for migrating cards between decks in the  <see cref="PDCardDeckGroup{TCard}"/> component.
	/// </remarks>
	internal async Task AddCardsAsync(List<TCard> selection)
	{
		DragState.IsDragging = true;
		await CancelAnimations();
		// Create the new cards
		for (int index = 0; index < selection.Count; index++)
		{
			var card = selection[index];

			Cards.Insert(ClampBounds(index + DragState.TargetIndex), card);
			Selection.Add(card);
		}
		ClearAnimationPositions();
		UpdateCardDeckPositions();
		await SyncDataProviderCardsAsync();

		await InvokeAsync(StateHasChanged);
	}

	/// <summary>
	/// Removes the currently selected cards in this deck and  updates card positions accordingly. 
	/// </summary>
	/// <param name="selection"></param>
	/// <returns></returns>
	/// <remarks>
	/// Used for migrating cards between decks in the  <see cref="PDCardDeckGroup{TCard}"/> component.
	/// </remarks>
	internal async Task RemoveSelectedCardsAsync()
	{
		DragState.IsDragging = false;
		await CancelAnimations();
		ClearAnimationPositions();
		foreach (var card in Selection)
		{
			Cards.Remove(card);
		}

		Selection.Clear();

		UpdateCardDeckPositions();
		await SyncDataProviderCardsAsync();
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

	/// <summary>
	/// Updates the deck positions of all cards in the deck based on their current order - locally.
	/// </summary>
	/// <remarks>
	/// To push these changes to the data provider, you must call <see cref="SyncDataProviderCardsAsync"/> after this method.
	/// </remarks>
	private void UpdateCardDeckPositions()
	{
		for (int index = 0; index < Cards.Count; index++)
		{
			var card = Cards[index];
			card.DeckPosition = index;
		}
	}

	#region Animation Methods
	private async Task UpdateAnimationPositionsAsync()
	{
		// Not an animated component, so no need to update positions
		if (!IsAnimated)
		{
			return;
		}

		List<Task> updateTasks = [];

		foreach (var card in _cards)
		{
			updateTasks.Add(card.AnimationHandler.UpdatePositionAsync());
		}

		await Task.WhenAll(updateTasks);
	}

	/// <summary>
	/// Animates each card to their new positions
	/// </summary>
	/// <returns></returns>
	private async Task AnimateToPositionsAsync()
	{
		// Not an animated component, so do not animate
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

	/// <summary>
	/// Clears the animation positions of all cards in the deck that are not selected.
	/// </summary>
	/// <remarks>Used when changes to the contents of the deck have been made, like in the <see cref="PDCardDeckGroup{TCard}"/> component</remarks>
	private void ClearAnimationPositions()
	{
		if (!IsAnimated)
		{
			return;
		}

		foreach (var card in _cards)
		{
			if (!Selection.Contains(card.Card))
			{
				card.AnimationHandler.ClearPositions();
			}
		}
	}

	private async Task CancelAnimations()
	{
		if (!IsAnimated)
		{
			return;
		}
		foreach (var card in _cards)
		{
			await card.AnimationHandler.CancelAnimationAsync();
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

	/// <summary>
	/// Synchronizes the local card deck with the data provider
	/// </summary>
	/// <returns></returns>
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
				_logger.LogError("Failed to delete card [{CardId}] on data provider.", card.Id);
				continue;
			}

		}
	}

	/// <summary>
	/// Updates the deck position of a card in the data provider.
	/// </summary>
	/// <param name="card"></param>
	/// <param name="deckPosition"></param>
	/// <returns></returns>
	private async Task UpdateCardDeckPositionAsync(TCard card, int? deckPosition)
	{
		var updateResponse = await _dataProviderService.UpdateAsync(card, new Dictionary<string, object?>
		{
			{ nameof(card.DeckPosition), deckPosition }
		}, default);

		if (!updateResponse.Success)
		{
			_logger.LogError("Failed to update card [{CardId}] on data provider.", card.Id);
		}
	}

	/// <summary>
	/// Creates a new card in the data provider and sets its deck position.
	/// </summary>
	/// <param name="card"></param>
	/// <returns></returns>
	private async Task AddCardToDataProviderAsync(TCard card)
	{
		// Need to add it and update its position
		var createResponse = await _dataProviderService.CreateAsync(card, default);

		if (!createResponse.Success)
		{
			_logger.LogError("Failed to create card [{CardId}] on data provider.", card.Id);
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
			_logger.LogError("DataProvider is not set");
			return false;
		}

		// Check for duplicate DeckPositions in the current deck, ignoring nulls
		var duplicatedPosition = Cards
			.Where(c => c.DeckPosition != null)
			.GroupBy(c => c.DeckPosition)
			.Any(g => g.Count() > 1);

		if (duplicatedPosition)
		{
			_logger.LogError("Local Data contains duplicated positions on Card Deck [{CardDeck}]", Id);
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
