using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDCardDeckGroupPage
	{
		#region Example 1
		private readonly ListDataProviderService<Todo> _todoList1 = new();
		private readonly ListDataProviderService<Todo> _todoList2 = new();
		private readonly ListDataProviderService<Todo> _todoList3 = new();

		private List<IDataProviderService<Todo>> _todoLists = [];
		#endregion

		#region Example 2
		private readonly ListDataProviderService<Todo> _todoList4 = new();
		private readonly ListDataProviderService<Todo> _todoList5 = new();
		private readonly ListDataProviderService<Todo> _todoList6 = new();

		private List<IDataProviderService<Todo>> _todoLists2 = [];
		#endregion


		protected override void OnInitialized()
		{
			SetupExample1();

			SetupExample2();
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
	}
}