namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCardDeckPage
{
	private List<Todo> _todoList = new();

	protected override void OnInitialized()
	{
		// generate some todos
		_todoList.AddRange(new[] {
			new Todo("Do weekly shop"),
			new Todo("Record Antiques Roadshow"),
			new Todo("Put up shelf"),
			new Todo("Get a haircut"),
			new Todo("Walk the dog")
		});
	}
}
