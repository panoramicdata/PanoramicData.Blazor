using PanoramicData.Blazor.Services;
using System.Text;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDCardDeckGroupPage
	{
		#region Example 1
		private readonly ListDataProviderService<Todo> _todoList1 = new();
		private readonly ListDataProviderService<Todo> _todoList2 = new();
		private readonly ListDataProviderService<Todo> _todoList3 = new();

		private List<IDataProviderService<Todo>> _todoLists = [];

		private PDCardDeckGroup<Todo> _cardDeckGroup1 = null!;
		#endregion

		#region Example 2
		private readonly ListDataProviderService<Todo> _todoList4 = new();
		private readonly ListDataProviderService<Todo> _todoList5 = new();
		private readonly ListDataProviderService<Todo> _todoList6 = new();

		private List<IDataProviderService<Todo>> _todoLists2 = [];

		private PDCardDeckGroup<Todo> _cardDeckGroup2 = null!;
		#endregion

		#region
		private readonly ListDataProviderService<Todo> _todoList7 = new();
		private readonly ListDataProviderService<Todo> _todoList8 = new();
		private readonly ListDataProviderService<Todo> _todoList9 = new();

		private List<IDataProviderService<Todo>> _todoLists3 = [];

		private PDCardDeckGroup<Todo> _cardDeckGroup3 = null!;

		#endregion

		[CascadingParameter] protected EventManager? EventManager { get; set; }

		protected override void OnInitialized()
		{
			SetupExample1();

			SetupExample2();

			SetupExample3();
		}

		private void OnSelect(MouseEventArgs args, PDCardDeckGroup<Todo> cardDeckGroup)
		{
			var selectedDecks = cardDeckGroup.Decks
				.Where(deck => deck.Selection.Count > 0)
				.Select(deck => deck.Id)
				.ToList();

			EventManager?.Add(new Event("Decks Selected", new EventArgument("Decks", string.Join(", ", selectedDecks))));
		}

		private async Task OnRearrange(DragEventArgs args, List<IDataProviderService<Todo>> dataProviders)
		{
			var output = new StringBuilder("");

			foreach ((var dataProvider, var index) in dataProviders.Select((index, dataProvider) => (index, dataProvider)))
			{
				var data = await dataProvider.GetDataAsync(new(), default);

				var cardsString = ConvertToString(data.Items.OrderBy(c => c.DeckPosition));

				output.AppendLine($"Deck [{index}]: {cardsString}");
			}

			EventManager?.Add(new Event("Order State Updated", new EventArgument("Rearranged Cards:", output)));
		}

		private async Task OnCardMigration(List<IDataProviderService<Todo>> dataProviders)
		{
			var output = new StringBuilder("");

			foreach ((var dataProvider, var index) in dataProviders.Select((index, dataProvider) => (index, dataProvider)))
			{
				var data = await dataProvider.GetDataAsync(new(), default);

				var cardsString = ConvertToString(data.Items.OrderBy(c => c.DeckPosition));

				output.AppendLine($"Deck [{index}]: {cardsString}");
			}

			EventManager?.Add(new Event("Migrated Card(s)", new EventArgument("List", output)));
		}


		private void SetupExample1()
		{
			_todoList1.List.AddRange(
							new Todo { Title = "Fix Sink", Description = "a description" },
							new Todo { Title = "Water the Garden", Description = "a description", Priority = Priority.Minor },
							new Todo { Title = "Walk the dog", Description = "a description", Priority = Priority.Critical },
							new Todo { Title = "Put Pan Fire Out", Description = "a description", Priority = Priority.Blocker });

			_todoList2.List.Add(new Todo { Title = "Open the Window", Description = "a description", Priority = Priority.Major });

			_todoList3.List.Add(new Todo { Title = "Write Example Code", Description = "a description", Priority = Priority.Trivial });

			_todoLists = [_todoList1, _todoList2, _todoList3];
		}

		private void SetupExample2()
		{
			_todoList4.List.AddRange(
				new Todo
				{
					Title = "Fix Sink",
					Description = "a description"
				},
				new Todo
				{
					Title = "Water the Garden",
					Description = "a description",
					Priority = Priority.Minor
				},
				new Todo
				{
					Title = "Walk the dog",
					Description = "a description",
					Priority = Priority.Critical
				},
				new Todo
				{
					Title = "Put Pan Fire Out",
					Description = "a description",
					Priority = Priority.Blocker
				});

			_todoList5.List.Add(new Todo
			{
				Title = "Open the Window",
				Description = "a description",
				Priority = Priority.Major
			});

			_todoList6.List.Add(new Todo
			{
				Title = "Write Example Code",
				Description = "a description",
				Priority = Priority.Trivial
			});

			_todoLists2 = [_todoList4, _todoList5, _todoList6];
		}

		private void SetupExample3()
		{
			_todoList7.List.AddRange(
				new Todo
				{
					Title = "Fix Sink",
					Description = "a description"
				},
				new Todo
				{
					Title = "Water the Garden",
					Description = "a description",
					Priority = Priority.Minor
				},
				new Todo
				{
					Title = "Walk the dog",
					Description = "a description",
					Priority = Priority.Critical
				},
				new Todo
				{
					Title = "Put Pan Fire Out",
					Description = "a description",
					Priority = Priority.Blocker
				});

			_todoList8.List.Add(new Todo
			{
				Title = "Open the Window",
				Description = "a description",
				Priority = Priority.Major
			});

			_todoList9.List.Add(new Todo
			{
				Title = "Write Example Code",
				Description = "a description",
				Priority = Priority.Trivial
			});

			_todoLists3 = [_todoList7, _todoList8, _todoList9];
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
	}
}