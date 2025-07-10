

namespace PanoramicData.Blazor
{
	public partial class PDCardDeckGroup<TCard> where TCard : ICard
	{
		private static int _sequence;

		private List<string> _destinations = [];

		private PDCardDeckLoadingIcon _loadingIcon = new();

		/// <summary>
		/// Holds references to the decks that are part of this group
		/// </summary>
		private List<PDCardDeck<TCard>> _decks = [];

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
		/// The Event that is invoked when the user migrates cards from one deck to another within this group.
		/// </summary>
		[Parameter]
		public EventCallback OnCardMigration { get; set; }

		/// <summary>
		/// The decks that are to be rendered within this group.
		/// </summary>
		[Parameter]
		public RenderFragment? ChildContent { get; set; }

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
				{ "class", "pd-carddeck-group-default" },
			};

			return dict;
		}

		#region UI Indicators for Migration
		public void RegisterDestination(PDCardDeck<TCard> deck)
		{
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

		internal async Task UpdateCardLocationsAsync()
		{
			// Must have two destinations to perform a migration.
			if (_destinations.Count < 2)
			{
				return;
			}

			// Perform the migration
			var source = _decks.FirstOrDefault(c => _destinations[0] == c.Id);
			var destination = _decks.FirstOrDefault(c => _destinations[1] == c.Id);

			if (source == null || destination == null)
			{
				return;
			}

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
		}

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

		internal void SetActiveDeck(PDCardDeck<TCard> activeDeck)
		{
			foreach (var deck in _decks)
			{
				if (deck != activeDeck)
				{
					deck.Selection.Clear();
				}
			}
		}

		internal void RegisterDeckAsChild(PDCardDeck<TCard> deck)
		{
			if (deck == null || _destinations.Contains(deck.Id))
			{
				return;
			}

			_decks.Add(deck);

			StateHasChanged();
		}
	}
}