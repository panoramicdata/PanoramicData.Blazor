namespace PanoramicData.Blazor
{
	public partial class PDCardDeckGroup<TCard> where TCard : ICard
	{
		private static int _sequence;

		private PDCardDeck<TCard>? _dragSource;
		private PDCardDeck<TCard>? _dropTarget;

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
		/// Function that checks the validity of card moving operations between decks
		/// </summary>
		[EditorRequired]
		[Parameter]
		public Func<PDCardDeck<TCard>, PDCardDeck<TCard>, bool>? MoveValidationFunction { get; set; }

		/// <summary>
		/// Operation that is performed when a card (or collection of cards) are to be moved from one deck to another
		/// </summary>
		[EditorRequired]
		[Parameter]
		public Action<PDCardDeck<TCard>, PDCardDeck<TCard>>? TransformOperation { get; set; }

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

		public void RegisterSource(PDCardDeck<TCard> deck)
		{
			_dropTarget = null!;
			_dragSource = deck;
		}

		public void RegisterTarget(PDCardDeck<TCard> deck)
		{
			// Cannot drag from the same deck
			if (deck != _dragSource)
			{
				_dropTarget = deck;
			}
		}

		internal async Task InitiateTransformAsync()
		{
			// Ensure that the transform methods are set
			if (MoveValidationFunction is null || TransformOperation is null)
			{
				return;
			}

			// Cannot perform a transform without a source and target
			if (_dragSource is null || _dropTarget is null)
			{

				return;
			}

			// Move is not valid
			if (!MoveValidationFunction(_dragSource, _dropTarget))
			{

				return;
			}
			var movingCards = _dragSource.Selection.ToList();

			// Perform the transform
			TransformOperation(_dragSource, _dropTarget);

			// reset the drag and drop targets
			_dragSource.Selection.Clear();
			_dropTarget.Selection.Clear();

			await _dropTarget.RefreshAsync();
			await _dragSource.RefreshAsync();

			_dragSource = null;
			_dropTarget = null;

			await InvokeAsync(StateHasChanged);
		}
	}
}