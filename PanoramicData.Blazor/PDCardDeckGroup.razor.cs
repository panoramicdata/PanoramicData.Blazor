


namespace PanoramicData.Blazor;

public partial class PDCardDeckGroup<TCard> where TCard : ICard
{
	/// <summary>
	/// A unique sequence number for each instance of this component.`
	/// </summary>
	private static int _sequence;

	/// <summary>
	/// Holds a list of destination deck IDs that are part of the current card migration process (this is for UI only).
	/// </summary>
	private List<string> _destinations = [];

	/// <summary>
	/// The original deck that holds the cards that are moving
	/// </summary>
	private PDCardDeck<TCard> _sourceDeck = null!;

	/// <summary>
	/// Reference to the loading icon that is displayed when the data is being loaded for the decks in this group.
	/// </summary>
	private PDCardDeckLoadingIcon _loadingIcon = new();

	/// <summary>
	/// Holds references to the decks that are part of this group
	/// </summary>
	private List<PDCardDeck<TCard>> _decks = [];


	/// <summary>
	/// Check if any of the decks in this group have cards that are currently being dragged.
	/// </summary>
	private bool _isDragging
		=> _decks.Any(deck => deck.DragState.IsDragging);

	#region Parameters

	/// <summary>
	/// Unique identifier for this card deck group.
	/// </summary>
	[Parameter]
	public override string Id { get; set; } = $"pd-carddeckgroup-{++_sequence}";

	/// <summary>
	/// Data Provider for this Card Deck Group.
	/// </summary>
	[EditorRequired]
	[Parameter]
	public IDataProviderService<TCard> DataProvider { get; set; } = null!;

	/// <summary>
	/// The decks that are to be rendered within this group.
	/// </summary>
	[EditorRequired]
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Determines if a card move is valid.
	/// </summary>
	/// <remarks>
	/// In the form Func{Source, Destination, outcome}
	/// <para>
	/// <list type="bullet">
	/// <item>Source - The deck that the moving card(s) have originated from</item>
	/// <item>Destination - The deck that the moving card(s) are moving to</item>
	/// <item>Outcome - Is the move allowed? True / False</item>
	/// </list></para>
	/// </remarks>
	[Parameter]
	public Func<PDCardDeck<TCard>, PDCardDeck<TCard>, bool>? ValidateCardMove { get; set; }

	/// <summary>
	/// Transformation that is applied to the cards when they are moved within this group. This can be a Reordering operation or Migration (cards moving from one deck to another)
	/// </summary>
	/// <remarks>
	/// In the form Func{DataProvider, Source, Destination, Cards}
	/// <para>
	/// <list type="bullet">
	/// <item>DataProvider - The data provider that is responsible for the data in these decks</item>
	/// <item>Source - The deck that the moving card(s) have originated from</item>
	/// <item>Destination - The deck that the moving card(s) are moving to</item>
	/// <item>Cards - List of Cards that are to be transformed</item>
	/// </list></para>
	/// </remarks>
	[Parameter]
	public Func<IDataProviderService<TCard>, PDCardDeck<TCard>, PDCardDeck<TCard>, List<TCard>, Task>? Transformation { get; set; }

	#endregion

	[Inject] IBlockOverlayService BlockOverlayService { get; set; } = null!;

	private IDataProviderService<TCard> _dataProviderService = new EmptyDataProviderService<TCard>();

	protected override void OnParametersSet()
	{
		if (DataProvider == _dataProviderService)
		{
			return;
		}

		_dataProviderService = DataProvider;
	}

	private Dictionary<string, object?> GetAttributes()
	{
		var dict = new Dictionary<string, object?>
		{
			{ "id", Id },
			{ "class", GetDeckGroupClass() },
		};

		return dict;
	}

	private string GetDeckGroupClass()
	{
		if (CssClass is null)
		{
			return "pd-carddeck-group-default";
		}

		return CssClass;
	}

	#region UI Indicators for Migration

	public void RegisterDestination(PDCardDeck<TCard> deck)
	{
		// Prevents interference between card deck groups
		if (!_isDragging)
		{
			return;
		}

		// Check the last deck in the list is not the same as the one being registered.
		if (_destinations.Count > 0 && _destinations[^1] == deck.Id)
		{
			return;
		}

		_destinations.Add(deck.Id);

		if (_destinations.Count > 2)
		{
			_destinations.RemoveAt(0);
		}
	}

	/// <summary>
	/// Updates the locations of cards that are being dragged between decks in this group.
	/// </summary>
	/// <returns></returns>
	internal async Task UpdateGraphicalCardLocationsAsync()
	{
		// Must have two destinations to perform a migration.
		if (_destinations.Count < 2)
		{
			return;
		}

		// Check if the move is valid
		var source = _decks.FirstOrDefault(c => _destinations[0] == c.Id);
		var destination = _decks.FirstOrDefault(c => _destinations[1] == c.Id);

		if (source == null || destination == null)
		{
			return;
		}

		if (ValidateCardMove is not null && !ValidateCardMove(_sourceDeck, destination))
		{
			// If the move is not valid, clear the destinations and return.
			_destinations.Clear();
			await InvokeAsync(StateHasChanged);
			return;
		}

		// Perform the migration
		List<TCard> movingCards = [.. source.Selection];

		await source.RemoveSelectedCardsAsync();
		await destination.AddCardsAsync(movingCards);

		_destinations.Clear();

		await InvokeAsync(StateHasChanged);
	}

	#endregion

	internal async Task InitiateTransformAsync()
	{
		await Task.CompletedTask;

		// Refresh if a transform has occurred
		if (Transformation is null || _destinations.Count == 0)
		{
			return;
		}

		// Perform the transformation of cards from the source deck to the destination deck.
		var movingCards = _decks.FirstOrDefault(d
			=> d.Id == _destinations.First())?.Selection;

		var destination = _decks.FirstOrDefault(d
			=> d.Id == _destinations.Last());

		if (destination is null || movingCards is null)
		{
			return;
		}

		// Update the card deck positions of _source and destinations before Transformation
		destination.UpdateCardDeckPositions();
		_sourceDeck.UpdateCardDeckPositions();

		await Transformation(_dataProviderService, _sourceDeck, destination, movingCards);

		// Refresh the source deck and destination deck
		await EndDragOperationAsync();
	}

	/// <summary>
	/// Checks if all decks in this group have loaded their data. Indicates using Blocker overlay if data is not fully loaded
	/// </summary>
	/// <returns></returns>
	public bool AllDataLoaded()
	{
		if (_decks.Count == 0)
		{
			// Show the Blocker overlay only when the icon is active, to avoid flickering.
			if (_loadingIcon.IsActive)
			{
				BlockOverlayService.Show();
			}
			return false;
		}
		foreach (var deck in _decks)
		{
			if (!deck.DataLoaded)
			{
				// Show the Blocker overlay only when the icon is active, to avoid flickering.
				if (_loadingIcon.IsActive)
				{
					BlockOverlayService.Show();
				}
				return false;
			}
		}

		BlockOverlayService.Hide();
		return true;
	}

	/// <summary>
	/// Clears the selection of all decks in this group, except for the active deck. 
	/// </summary>
	/// <param name="activeDeck"></param>
	/// <remarks>
	/// Prevents cases where selections are incorrect when dragging across multiple decks
	/// </remarks>
	internal void ClearAllSelectionExceptActive(PDCardDeck<TCard> activeDeck)
	{
		foreach (var deck in _decks)
		{
			if (deck != activeDeck)
			{
				deck.Selection.Clear();
			}
		}
	}

	/// <summary>
	/// Registers a deck as a child of this group.
	/// </summary>
	/// <param name="deck"></param>
	internal void RegisterDeckAsChild(PDCardDeck<TCard> deck)
	{
		if (deck == null || _destinations.Contains(deck.Id))
		{
			return;
		}

		_decks.Add(deck);

		StateHasChanged();
	}

	internal void SetSourceDeck(PDCardDeck<TCard> source)
	{
		_sourceDeck = source;
		_destinations.Add(source.Id);
	}

	internal async Task EndDragOperationAsync()
	{

		var destination = _decks.FirstOrDefault(d => _destinations.Count > 0 && d.Id == _destinations.Last());

		if (destination is not null)
		{
			await destination.RefreshAsync();
		}

		if (_sourceDeck is not null)
		{
			await _sourceDeck.RefreshAsync();
		}

		_destinations.Clear();
		_sourceDeck = null!;
		await InvokeAsync(StateHasChanged);
	}
}