namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCardDeckPage
{
	private List<Todo> _todoList1 = new();
	private List<Todo> _todoList2 = new();
	private List<Todo> _todoList3 = new();

	protected override void OnInitialized()
	{
		// generate some todos
		_todoList1.AddRange(new[] {
			new Todo("Do weekly shop"),
			new Todo("Record Antiques Roadshow"),
			new Todo("Get a haircut"),
			new Todo("Walk the dog")
		});

		_todoList2.AddRange(new[] {
			new Todo("Book holiday"),
			new Todo("Do weekly shop"),
			new Todo("Record Antiques Roadshow"),
			new Todo("Put up shelf"),
			new Todo("Get a haircut"),
			new Todo("Walk the dog")
		});

		_todoList3.AddRange(new[] {
			new Todo("Get a haircut"),
			new Todo("Record Antiques Roadshow"),
			new Todo("Walk the dog")
		});
	}
}
