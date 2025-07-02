namespace PanoramicData.Blazor
{
	public partial class PDCardDeckGroup<TCard> where TCard : ICard
	{

		private static int _sequence;

		private DeckGroupContext<TCard> _context = null!;

		private readonly List<int> _userTrail = [];

		// Reference Capture
		public List<PDCardDeck<TCard>> Decks { get; private set; } = [];

		public PDCardDeck<TCard> Ref { set => Decks.Add(value); }

		#region Parameters

		/// <summary>
		/// Unique identifier for this card deck group.
		/// </summary>
		[Parameter]
		public override string Id { get; set; } = $"pd-carddeck-{++_sequence}";

		/// <summary>
		/// Whether the decks have animations enabled or not. Defaults to false.
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

		/// <summary>
		/// Holds styling for each individual Deck
		/// </summary>
		[Parameter]
		public string DeckCss { get; set; } = "pdcarddeck";

		/// <summary>
		/// Data Providers for this card deck group
		/// </summary>
		[EditorRequired]
		[Parameter]
		public required IEnumerable<IDataProviderService<TCard>> DataProviders { get; set; }

		/// <summary>
		/// Whether the deck has multiple selection enabled or not. Defaults to false.
		/// </summary>
		[Parameter]
		public bool MultipleSelection { get; set; }

		/// <summary>
		/// Event that is invoked when the user rearranges cards in within a deck in this group.
		/// </summary>
		[Parameter]
		public EventCallback<DragEventArgs> OnRearrangeCards { get; set; }

		/// <summary>
		/// Event that is invoked when the user selects a card / cards within a deck in this group.
		/// </summary>
		[Parameter]
		public EventCallback<MouseEventArgs> OnCardSelection { get; set; }

		/// <summary>
		/// The Event that is invoked when the user migrates cards from one deck to another within this group.
		/// </summary>
		[Parameter]
		public EventCallback OnCardMigration { get; set; }

		#endregion

		private Dictionary<string, object?> GetAttributes()
		{
			var dict = new Dictionary<string, object?>
			{
				{ "id", Id },
				{ "class", "pd-carddeck-group-default" },
			};

			return dict;
		}

		protected override void OnInitialized()
		{
			_context = new(true)
			{
				OnSelect = OnSelectDeckAsync,
				OnDragEntered = OnDragAsync,

			};
		}

		#region Event Handlers

		private async Task OnSelectDeckAsync(PDCardDeck<TCard> sourceDeck)
		{
			// Remove selection for non source decks
			foreach (var deck in Decks)
			{
				if (deck != sourceDeck)
				{
					deck.Selection.Clear();
				}
			}

			await InvokeAsync(StateHasChanged);
		}

		private async Task OnDragAsync(PDCardDeck<TCard> deck)
		{
			// First deck in this user trail
			if (_userTrail.Count == 0)
			{
				_userTrail.Add(Decks.IndexOf(deck));
				return;
			}

			if (deck == Decks[_userTrail.Last()])
			{
				// User is dragging the same deck, do nothing
				return;
			}

			_userTrail.Add(Decks.IndexOf(deck));

			// Clamp the user trail to the last two decks
			if (_userTrail.Count > 2)
			{
				// User is dragging a deck that is not the first in the trail, clear the trail
				_userTrail.RemoveAt(0);
			}

			// Move the Cards
			await MoveCardsAsync();

			await InvokeAsync(StateHasChanged);
		}

		private async Task MoveCardsAsync()
		{
			if (_userTrail.Count < 2)
			{
				// Not enough decks in the trail to move cards
				return;
			}

			var sourceDeck = Decks[_userTrail[0]];
			var destinationDeck = Decks[_userTrail[1]];

			if (!sourceDeck.DragState.IsDragging)
			{
				// User is not dragging cards, do not allow migration
				return;
			}

			await destinationDeck.AddCardsAsync(sourceDeck.Selection);
			await sourceDeck.RemoveSelectedCardsAsync();

			await OnCardMigration.InvokeAsync();
			_userTrail.RemoveAt(0);
		}
		#endregion
	}
}