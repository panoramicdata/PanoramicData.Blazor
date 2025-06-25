namespace PanoramicData.Blazor
{
	public partial class PDCardDeckGroup<TCard> where TCard : ICard
	{

		private static int _sequence;

		private DeckGroupContext<TCard> _context = null!;

		private List<int> _userTrail = [];

		// Reference Capture
		private List<PDCardDeck<TCard>> _decks = [];

		public PDCardDeck<TCard> Ref { set => _decks.Add(value); }

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

		/// <summary>
		/// Holds styling for each individual Deck
		/// </summary>
		[Parameter]
		public string DeckCss { get; set; } = "pd-carddeck-default";

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
			foreach (var deck in _decks)
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
				_userTrail.Add(_decks.IndexOf(deck));
				return;
			}

			if (deck == _decks[_userTrail.Last()])
			{
				// User is dragging the same deck, do nothing
				return;
			}

			_userTrail.Add(_decks.IndexOf(deck));

			// Clamp the user trail to the last two decks
			if (_userTrail.Count > 2)
			{
				// User is dragging a deck that is not the first in the trail, clear the trail
				_userTrail.RemoveAt(0);
			}

			// Move the Cards
			MoveCards();

			await InvokeAsync(StateHasChanged);
		}

		private void MoveCards()
		{
			if (_userTrail.Count < 2)
			{
				// Not enough decks in the trail to move cards
				return;
			}

			var sourceDeck = _decks[_userTrail[0]];
			var destinationDeck = _decks[_userTrail[1]];

			destinationDeck.AddCards(sourceDeck.Selection);
			sourceDeck.RemoveSelectedCards();

			_userTrail.RemoveAt(0);
		}
		#endregion
	}
}