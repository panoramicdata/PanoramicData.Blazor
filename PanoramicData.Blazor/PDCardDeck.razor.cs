using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

public partial class PDCardDeck<TCard> where TCard : ICard
{
	private static int _sequence;

	/// <inheritdoc/>
	private readonly PDCardDeckSelectionHelper<TCard> _selectionHelper = new();

	/// <inheritdoc/>
	public PDCardDragDropInformation DragState { get; private set; } = new();

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
	#endregion

	/// <summary>
	/// The Event Handlers that are used to handle drag and drop events for this deck.
	/// </summary>
	[CascadingParameter(Name = "cardDeckGroupContext")]
	/// <inheritdoc cref="DeckGroupContext{TCard}"/>
	public DeckGroupContext<TCard> GroupContext { get; set; } = new(false);

	private bool _isGroupMember
		=> GroupContext.IsMemberOfGroup;


	private Dictionary<string, object?> GetDeckAttributes()
	{
		var dict = new Dictionary<string, object?>
		{
			{ "class", $"{CssClass ?? "pdcarddeck"} {(IsEnabled ? "" : " disabled")} {SizeCssClass}" },
			{ "id", Id },
			{ "ondrop", (MouseEventArgs _) => DragState.IsDragging = false }
		};

		// If part of a group, add the drag enter and drag leave methods
		if (_isGroupMember)
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

		var request = new DataRequest<TCard>();
		var cards = await _dataProviderService.GetDataAsync(request, default).ConfigureAwait(true);

		Cards.Clear();

		for (int index = 0; index < cards.Count; index++)
		{
			var card = cards.Items.ElementAt(index);

			Cards.Add(card);
		}

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
		if (_isGroupMember && GroupContext.OnSelect != null)
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
		if (_isGroupMember && GroupContext.OnSelect != null)
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

	internal void AddCards(List<TCard> selection)
	{
		// Ensure correct bounds
		var dragIndex = ClampBounds();

		Cards.InsertRange(dragIndex, selection);
		Selection.AddRange(selection);
	}

	private int ClampBounds()
	{
		var dragIndex = Math.Min(DragState.TargetIndex, Cards.Count);
		dragIndex = Math.Max(dragIndex, 0);

		return dragIndex;
	}

	internal void RemoveSelectedCards()
	{
		foreach (var card in Selection)
		{
			Cards.Remove(card);
		}

		Selection.Clear();
	}

	private string SizeCssClass => Size switch
	{
		ButtonSizes.Small => "sm",
		ButtonSizes.Large => "lg",
		_ => "md"
	};
}
