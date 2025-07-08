using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDCardDeckGroupPage
	{

		public ListDataProviderService<Todo> _todoList1 { get; set; } = new();

		public ListDataProviderService<Todo> _todoList2 { get; set; } = new();

		public ListDataProviderService<Todo> _todoList3 { get; set; } = new();
	}
}