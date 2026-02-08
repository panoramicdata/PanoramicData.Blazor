namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTimelineDocumentation
{
	private readonly DateTime _minDate = new(2020, 1, 1);
	private readonly DateTime _maxDate = new(2025, 12, 31);

	private const string _example1Code = """
		<PDTimeline MinDateTime="@_minDate"
		            MaxDateTime="@_maxDate"
		            Height="150px" />

		@code {
		    private DateTime _minDate = new(2020, 1, 1);
		    private DateTime _maxDate = new(2025, 12, 31);
		}
		""";
}
