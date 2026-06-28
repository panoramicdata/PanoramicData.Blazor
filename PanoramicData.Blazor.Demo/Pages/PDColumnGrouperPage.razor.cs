namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDColumnGrouperPage
{
	private readonly PersonDataProvider _personDataProvider = new();
	private readonly PageCriteria _pageCriteria = new(1, 20);
	private readonly SortCriteria _sortCriteria = new("Last Name", SortDirection.Descending);
	private PDTable<Person> _table = null!;
	private PDColumnGroupVariant _variant = PDColumnGroupVariant.Segmented;
	private bool _showCounts = true;

	// Second, independent table used by the custom-CSS example.
	private readonly PersonDataProvider _brandedDataProvider = new();
	private readonly PageCriteria _brandedPageCriteria = new(1, 5);
	private readonly SortCriteria _brandedSortCriteria = new("Last Name", SortDirection.Descending);
	private PDTable<Person> _brandedTable = null!;

	protected override void OnAfterRender(bool firstRender)
	{
		// The grouper receives the table via @ref, which is assigned after the first render. Trigger one
		// further render so the grouper picks up the table reference and builds its facets.
		if (firstRender)
		{
			StateHasChanged();
		}
	}
}
