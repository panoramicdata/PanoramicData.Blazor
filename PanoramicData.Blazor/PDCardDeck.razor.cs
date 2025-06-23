using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

public partial class PDCardDeck<TCard> where TCard : ICard
{
	private static int _sequence;

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

	private Dictionary<string, object?> GetDeckAttributes()
	{
		var dict = new Dictionary<string, object?>
		{
			{ "class", $"pdcarddeck {CssClass}{(IsEnabled ? "" : " disabled")} {SizeCssClass}" },
			{ "id", Id }
		};
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
			card.CurrentPosition = index; // Set the index for the card

			Cards.Add(card);
		}

	}

	internal void AddToSelection(MouseEventArgs args, TCard card)
	{
		// No point in adding to selection if multiple selection is not enabled
		if (!MultipleSelection)
		{
			return;
		}

		if (args.ShiftKey)
		{
			Selection = PDCardDeckSelectionHelper
				.HandleAddRange(Selection, Cards, card);
		}

		else if (args.CtrlKey)
		{
			Selection = PDCardDeckSelectionHelper
				.HandleIndividualAddRemove(card, Selection);
		}
		else
		{
			// If no modifier keys are pressed, clear the selection and add the clicked card
			Selection.Clear();
			Selection.Add(card);
		}
		StateHasChanged();
	}

	private string SizeCssClass => Size switch
	{
		ButtonSizes.Small => "sm",
		ButtonSizes.Large => "lg",
		_ => "md"
	};
}
