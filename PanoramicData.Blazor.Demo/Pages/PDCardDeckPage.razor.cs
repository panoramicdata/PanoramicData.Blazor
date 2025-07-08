using PanoramicData.Blazor.Services;
using System.Text;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCardDeckPage
{
	private readonly ListDataProviderService<Todo> _todoList1 = new();
	private readonly ListDataProviderService<Todo> _todoList2 = new();
	private readonly ListDataProviderService<Todo> _todoList3 = new();
	private readonly ListDataProviderService<Todo> _todoList4 = new();


	private PDCardDeck<Todo> _cardDeck1 = new();
	private PDCardDeck<Todo> _cardDeck2 = new();
	private PDCardDeck<Todo> _cardDeck3 = new();
	private PDCardDeck<Todo> _cardDeck4 = new();

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	protected override void OnInitialized()
	{
		// generate some todos
		_todoList1.List.AddRange([
			new Todo{Title = "Do weekly shop", Description = "a description" },
			new Todo{Title = "Record Antiques Roadshow", Description = "a description"},
			new Todo{Title = "Get a haircut", Description = "a description"},
			new Todo{Title = "Walk the dog",Description =  "a description"} ]
			);

		_todoList2.List.AddRange([
			new Todo{Title = "Book holiday", Description = "a description"},
			new Todo{Title = "Do weekly shop", Description = "a description"},
			new Todo{Title = "Record Antiques Roadshow", Description = "a description"},
			new Todo{Title = "Put up shelf", Description = "a description"},
			new Todo{Title = "Get a haircut", Description = "a description"},
			new Todo{Title = "Walk the dog",Description =  "a description"} ]
			);

		_todoList3.List.AddRange([
			new Todo{Title = "Get a haircut", Description = "a description"},
			new Todo{Title = "Record Antiques Roadshow", Description = "a description", Priority = Priority.Minor},
			new Todo{Title = "Walk the dog",Description =  "a description", Priority = Priority.Critical},
			new Todo{Title = "Extinguish Shed Fire",Description =  "a description", Priority = Priority.Blocker},
		]
			);

		_todoList4.List.AddRange([
			new Todo{Title = "Get a haircut", Description = "a description"},
			new Todo{Title = "Record Antiques Roadshow", Description = "a description", Priority = Priority.Minor},
			new Todo{Title = "Walk the dog",Description =  "a description", Priority = Priority.Critical},
			new Todo{Title = "Extinguish Shed Fire",Description =  "a description", Priority = Priority.Blocker},
		]
			);
	}

	protected void OnSelect(MouseEventArgs args, PDCardDeck<Todo> cardDeck)
	{
		EventManager?.Add(new Event("Selection Update", new EventArgument("List", ConvertToString(cardDeck.Selection))));
	}

	protected void OnRearrange(DragEventArgs args, ListDataProviderService<Todo> dataProvider)
	{
		EventManager?.Add(new Event("Rearranged cards", new EventArgument("List", ConvertToString(dataProvider.List.OrderBy(c => c.DeckPosition)))));
	}

	private static string ConvertToString(IEnumerable<Todo?> cards)
	{
		var stringBuilder = new StringBuilder("[");

		foreach (var card in cards)
		{
			if (card != null)
			{
				stringBuilder.AppendLine($"{card.Title}, ");
			}
			else
			{
				stringBuilder.AppendLine("null, ");
			}
		}

		stringBuilder.Append(']');

		return stringBuilder.ToString();
	}

	/// <summary>
	/// Fetch information from the data provider and returns a DataResponse.
	/// </summary>
	/// <param name="dataProvider"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	private static Task<DataResponse<Todo>> FetchData(ListDataProviderService<Todo> dataProvider, CancellationToken cancellationToken)
		=> dataProvider.GetDataAsync(new DataRequest<Todo>(), cancellationToken);
}
