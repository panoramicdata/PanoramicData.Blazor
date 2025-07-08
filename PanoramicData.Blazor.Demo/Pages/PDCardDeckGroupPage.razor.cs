using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDCardDeckGroupPage
	{
		public PDCardDeck<Todo> _cardDeck1 { get; set; } = null!;
		public PDCardDeck<Todo> _cardDeck2 { get; set; } = null!;
		public PDCardDeck<Todo> _cardDeck3 { get; set; } = null!;


		public ListDataProviderService<Todo> _todoList1 { get; set; } = new();

		public ListDataProviderService<Todo> _todoList2 { get; set; } = new();

		public ListDataProviderService<Todo> _todoList3 { get; set; } = new();

		protected override void OnInitialized()
		{
			_todoList1.List.AddRange([new() { Title = "A Todo", Description = "a description" },
			new() { Title = "Put out Pan Fire", Description = "a description" },
			new() { Title = "Mow the Lawn", Description = "a description" }]);
		}
	}
}