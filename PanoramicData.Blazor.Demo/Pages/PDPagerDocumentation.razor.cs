namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDPagerDocumentation
{
	private int _currentPage = 1;

	private const string _example1Code = """
		<PDPager TotalItems="100"
		         PageSize="10"
		         @bind-CurrentPage="_currentPage" />

		@code {
		    private int _currentPage = 1;
		}
		""";
}
