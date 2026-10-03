namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDPagerDocumentation
{
	protected int CurrentPage { get; set; } = 1;

	private const string _example1Code = """
		<PDPager TotalItems="100"
		         PageSize="10"
		         @bind-CurrentPage="_currentPage" />

		@code {
		    private int _currentPage = 1;
		}
		""";
}
