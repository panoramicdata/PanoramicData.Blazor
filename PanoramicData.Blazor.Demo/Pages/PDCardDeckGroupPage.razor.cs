using PanoramicData.Blazor.Services;
using System.Text;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCardDeckGroupPage
{
	public CardDeckDataProviderService<Todo> TodoList1 { get; set; } = new();

	public CardDeckDataProviderService<Todo> TodoList2 { get; set; } = new();

	public CardDeckDataProviderService<Todo> TodoList3 { get; set; } = new();

	public CardDeckDataProviderService<Todo> TodoList4 { get; set; } = new();

	public CardDeckDataProviderService<Todo> TodoList5 { get; set; } = new();

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	protected override void OnInitialized()
	{
		TodoList1.List.AddRange([new() {
			Title = "A Todo", Description = "a description" },
		new() { Title = "Put out Pan Fire", Description = "a description" },
		new() { Title = "Mow the Lawn", Description = "a description" }]);

		TodoList2.List.AddRange([new() {
			Title = "A Todo", Description = "a description" },
		new() { Title = "Put out Pan Fire", Description = "a description" },
		new() { Title = "Mow the Lawn", Description = "a description" }]);

		TodoList3.List.AddRange([new() {
			Title = "A Todo", Description = "a description" },
		new() { Title = "Put out Pan Fire", Description = "a description" },
		new() { Title = "Mow the Lawn", Description = "a description" }]);

		TodoList4.List.AddRange([new() {
			Title = "A Todo", Description = "a description" },
		new() { Title = "Put out Pan Fire", Description = "a description" },
		new() { Title = "Mow the Lawn", Description = "a description" }]);

		TodoList5.List.AddRange([new() {
			Title = "A Todo", Description = "a description" },
		new() { Title = "Put out Pan Fire", Description = "a description" },
		new() { Title = "Mow the Lawn", Description = "a description" }]);
	}

	private static Func<Task<DataResponse<Todo>>> GetDataFunction(CardDeckDataProviderService<Todo> dataProvider, TodoProgress progress)
		=> ()
			=> dataProvider.GetDataAsync(new DataRequest<Todo>
			{
				ResponseFilter = items => items.Where(item => item.Progress == progress),
			}, CancellationToken.None);

	private static Func<Task<DataResponse<Todo>>> GetDelayedDataFunction(CardDeckDataProviderService<Todo> dataProvider, TodoProgress progress)
		=> ()
			=> dataProvider.GetDataAsync(new DataRequest<Todo>
			{
				ResponseFilter = items => items.Where(item => item.Progress == progress),
			}, 5, CancellationToken.None);


	private async Task MoveCardToRespectiveStatusAsync(
		IDataProviderService<Todo> dataProvider,
		PDCardDeck<Todo> sourceDeck,
		PDCardDeck<Todo> destinationDeck,
		List<Todo> cards)
	{
		// Update the data provider cards depending on the destination deck
		var destinationId = destinationDeck.Id;

		var cardProgress = destinationId switch
		{
			"beingdefined" => TodoProgress.BeingDefined,
			"inprogress" => TodoProgress.InProgress,
			"readyfortest" => TodoProgress.ReadyForTest,
			"done" => TodoProgress.Done,
			_ => TodoProgress.BeingDefined
		};

		// Update each moving card
		foreach (var card in cards)
		{
			await dataProvider.UpdateAsync(card, new Dictionary<string, object?>
			{
				{ nameof(Todo.Progress), cardProgress },
				{ nameof(Todo.DeckPosition), card.DeckPosition }
			}, default);
		}

		if (sourceDeck == destinationDeck)
		{
			EventManager!.Add(new Event($"Reordered cards for {destinationDeck.Parent!.Id}", new EventArgument($"{destinationDeck.Id} - Order", ConvertToString(destinationDeck.Cards))));
		}
		else
		{
			EventManager!.Add(new Event($"Migrated cards for {destinationDeck.Parent!.Id}", new EventArgument($"{destinationDeck.Id} - New Contents", ConvertToString(destinationDeck.Cards))));
		}
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