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
			new Todo{Title = "Walk the dog",Description =  "a description", Priority = Priority.Critical} ]
			);

		_todoList4.List.AddRange([
			new Todo{Title = "Get a haircut", Description = "a description"},
			new Todo{Title = "Record Antiques Roadshow", Description = "a description", Priority = Priority.Minor},
			new Todo{Title = "Walk the dog",Description =  "a description", Priority = Priority.Critical},
			new Todo{Title = "Extinguish Shed Fire",Description =  "a description", Priority = Priority.Blocker},
		]
			);
	}

	protected void OnSelect1(MouseEventArgs args)
	{
		var cards = _cardDeck1.Selection;
		EventManager?.Add(new Event("Selection Update", new EventArgument("List", ConvertToString(cards))));
	}

	protected void OnSelect2(MouseEventArgs args)
	{
		var cards = _cardDeck2.Selection;
		EventManager?.Add(new Event("Selection Update", new EventArgument("List", ConvertToString(cards))));
	}

	protected void OnSelect3(MouseEventArgs args)
	{
		var cards = _cardDeck3.Selection;
		EventManager?.Add(new Event("Selection Update", new EventArgument("List", ConvertToString(cards))));
	}

	protected void OnSelect4(MouseEventArgs args)
	{
		var cards = _cardDeck4.Selection;
		EventManager?.Add(new Event("Selection Update", new EventArgument("List", ConvertToString(cards))));
	}


	protected void OnRearrange1(DragEventArgs args)
	{
		var cards = _cardDeck1.Cards;

		EventManager?.Add(new Event("Rearranged cards", new EventArgument("List", ConvertToString(cards))));
	}

	protected void OnRearrange2(DragEventArgs args)
	{
		var cards = _cardDeck2.Cards;

		EventManager?.Add(new Event("Rearranged cards", new EventArgument("List", ConvertToString(cards))));
	}

	protected void OnRearrange3(DragEventArgs args)
	{
		var cards = _cardDeck3.Cards;

		EventManager?.Add(new Event("Rearranged cards", new EventArgument("List", ConvertToString(cards))));
	}

	protected void OnRearrange4(DragEventArgs args)
	{
		var cards = _cardDeck4.Cards;

		EventManager?.Add(new Event("Rearranged cards", new EventArgument("List", ConvertToString(cards))));
	}
	private string ConvertToString(IEnumerable<Todo?> cards)
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


}
