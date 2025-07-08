namespace PanoramicData.Blazor
{
	public partial class PDCardDeckGroup<TCard> where TCard : ICard
	{
		private static int _sequence;

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

		/// <summary>
		/// The decks that are part of this group
		/// </summary>
		[EditorRequired]
		[Parameter]
		public List<PDCardDeck<TCard>> Decks { get; set; } = [];

		#endregion

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
	}
}