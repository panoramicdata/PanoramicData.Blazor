namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDRangeDocumentation
{
	private double _value = 50;

	private const string _example1Code = """
		<PDRange @bind-Value="_value" 
		         Min="0" 
		         Max="100" />
		<p>Value: @_value</p>

		@code {
		    private double _value = 50;
		}
		""";
}
