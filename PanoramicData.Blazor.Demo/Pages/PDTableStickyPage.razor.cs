namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTableStickyPage
{
	private readonly PageCriteria _pageCriteria1 = new(1, 25);

	private readonly PageCriteria _pageCriteria2 = new(1, 25);

	private readonly PageCriteria _pageCriteria3 = new(1, 25);

	private readonly PageCriteria _pageCriteria4 = new(1, 5);

	private readonly PageCriteria _pageCriteria5 = new(1, 10);

	private readonly PersonDataProvider _personDataProvider = new();

	private PDTable<Person> Table1 { get; set; } = null!;

	private PDTable<Person> Table2 { get; set; } = null!;

	private PDTable<Person> Table3 { get; set; } = null!;

	private PDTable<Person> Table4 { get; set; } = null!;

	private PDTable<Person> Table5 { get; set; } = null!;
}
