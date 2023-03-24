namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCardDeckPage
{
	private List<Todo> _todoList1 = new();
	private List<Todo> _todoList2 = new();

	protected override void OnInitialized()
	{
		// generate some todos
		_todoList1.AddRange(new[] {
			new Todo("Do weekly shop"),
			new Todo("Record Antiques Roadshow"),
			new Todo("Put up shelf"),
			new Todo("Get a haircut"),
			new Todo("Walk the dog")
		});

		// generate some todos
		_todoList2.AddRange(new[] {
			new Todo("Do weekly shop"),
			new Todo("Record Antiques Roadshow"),
			new Todo("Put up shelf"),
			new Todo("Get a haircut"),
			new Todo("Walk the dog")
		});
	}
}
