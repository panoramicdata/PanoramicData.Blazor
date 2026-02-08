namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDropDownDocumentation
{
	private string? _selectedItem;
	private readonly List<string> _items = ["Option 1", "Option 2", "Option 3", "Option 4"];

	private const string _example1Code = """
		<PDDropDown TItem="string"
		            Items="_items"
		            @bind-SelectedItem="_selectedItem"
		            Placeholder="Select an option..." />

		@code {
		    private string? _selectedItem;
		    private List<string> _items = new()
		    {
		        "Option 1", 
		        "Option 2", 
		        "Option 3"
		    };
		}
		""";
}
