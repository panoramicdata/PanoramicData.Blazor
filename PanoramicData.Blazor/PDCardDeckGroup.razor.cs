
namespace PanoramicData.Blazor
{
	public partial class PDCardDeckGroup<TCard> where TCard : ICard
	{

		private static int _sequence;

		/// <summary>
		/// The deck that cards will be transfered from
		/// </summary>
		private PDCardDeck<TCard>? _sourceDeck;

		/// <summary>
		/// The deck that cards will be transfered to
		/// </summary>
		private PDCardDeck<TCard>? _targetDeck;

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

		protected override void OnAfterRender(bool firstRender)
		{
			if (!firstRender)
			{

			}

			var context = new DeckGroupContext<TCard>(true)
			{
				OnDeckSelected = OnSelectDeckAsync,
				OnDeckDragStarted = OnDragWithinAsync,
				OnDeckDragEntered = OnDragEnterDeckAsync,
				OnDeckDragLeft = OnDragLeaveDeckAsync

			};

			foreach (var deck in _decks)
			{
				deck.GroupContext = context;
			}


		}

		private Dictionary<string, object?> GetAttributes()
		{
			var dict = new Dictionary<string, object?>
			{
				{ "id", Id },
				{ "class", "pd-carddeck-group-default" },
			};

			return dict;
		}


		#region Event Handlers

		private async Task OnSelectDeckAsync(PDCardDeck<TCard> deck)
		{
			return;
		}

		private async Task OnDragWithinAsync(PDCardDeck<TCard> deck)
		{
			return;
		}

		private async Task OnDragEnterDeckAsync(PDCardDeck<TCard> deck)
		{
			return;
		}

		private async Task OnDragLeaveDeckAsync(PDCardDeck<TCard> deck)
		{
			return;
		}

		#endregion
	}
}