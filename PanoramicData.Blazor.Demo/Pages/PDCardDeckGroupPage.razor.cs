using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDCardDeckGroupPage
	{
		public CardDeckDataProviderService<Todo> _todoList1 { get; set; } = new();

		public CardDeckDataProviderService<Todo> _todoList2 { get; set; } = new();

		public CardDeckDataProviderService<Todo> _todoList3 { get; set; } = new();

		protected override void OnInitialized()
		{
			_todoList1.List.AddRange([new() { Title = "A Todo", Description = "a description" },
			new() { Title = "Put out Pan Fire", Description = "a description" },
			new() { Title = "Mow the Lawn", Description = "a description" }]);
		}

		private Func<Task<DataResponse<Todo>>> GetDataFunction(CardDeckDataProviderService<Todo> dataProvider, TodoProgress progress)
			=> ()
			=> dataProvider.GetDataAsync(new DataRequest<Todo>
			{
				ResponseFilter = items => items.Where(item => item.Progress == progress),
			}, CancellationToken.None);


		private Func<PDCardDeck<Todo>, PDCardDeck<Todo>, bool> ValidationFunction
				=> (source, target) =>
			{
				// Allow all moves
				return true;
			};

		private static Action<PDCardDeck<Todo>, PDCardDeck<Todo>>
			GetTransformOperation()
		{
			return (source, destination) =>
				{
					source.Selection.ForEach(item =>
					{
						// Update the progress of the todo item
						item.Progress = destination.Cards[0].Progress;
					});
				};
		}
	}
}