namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDateTimeDocumentation
{
	protected DateTime Date { get; set; } = DateTime.Today;

	private const string _example1Code = """
		<PDDateTime @bind-Value="_date" 
		            Format="yyyy-MM-dd" />

		@code {
		    private DateTime _date = DateTime.Today;
		}
		""";
}
