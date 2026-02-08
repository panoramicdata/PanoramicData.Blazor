namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDComboBoxDocumentation
{
	private string _value = "";
	private readonly List<string> _items = ["Apple", "Banana", "Cherry", "Date", "Elderberry"];

	private const string _example1Code = """
		<PDComboBox TItem="string"
		            Items="_items"
		            @bind-Value="_value"
		            Placeholder="Type or select..." />

		@code {
		    private string _value = "";
		    private List<string> _items = new()
		    { 
		        "Apple", "Banana", "Cherry" 
		    };
		}
		""";
}
