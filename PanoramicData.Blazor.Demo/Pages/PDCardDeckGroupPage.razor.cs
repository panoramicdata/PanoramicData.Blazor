using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDCardDeckGroupPage
	{
		public CardDeckDataProviderService<Todo> _todoList1 { get; set; } = new();

		public CardDeckDataProviderService<Todo> _todoList2 { get; set; } = new();

		public CardDeckDataProviderService<Todo> _todoList3 { get; set; } = new();
		public CardDeckDataProviderService<Todo> _todoList4 { get; set; } = new();

		protected override void OnInitialized()
		{
			_todoList1.List.AddRange([new() {
				Title = "A Todo", Description = "a description" },
			new() { Title = "Put out Pan Fire", Description = "a description" },
			new() { Title = "Mow the Lawn", Description = "a description" }]);

			_todoList2.List.AddRange([new() {
				Title = "A Todo", Description = "a description" },
			new() { Title = "Put out Pan Fire", Description = "a description" },
			new() { Title = "Mow the Lawn", Description = "a description" }]);

			_todoList3.List.AddRange([new() {
				Title = "A Todo", Description = "a description" },
			new() { Title = "Put out Pan Fire", Description = "a description" },
			new() { Title = "Mow the Lawn", Description = "a description" }]);

			_todoList4.List.AddRange([new() {
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
	}
}