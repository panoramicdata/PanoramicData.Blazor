using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDCardDeckGroupPage
	{

		private readonly ListDataProviderService<Todo> _todoList1 = new();
		private readonly ListDataProviderService<Todo> _todoList2 = new();
		private readonly ListDataProviderService<Todo> _todoList3 = new();

		private List<IDataProviderService<Todo>> _todoLists = [];

		protected override void OnInitialized()
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

	}
}