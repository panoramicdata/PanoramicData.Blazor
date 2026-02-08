namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDListDocumentation
{
	private const string _example1Code = """
		<PDList TItem="string"
		        Items="_items"
		        @bind-SelectedItem="_selected" />

		@code {
		    private List<string> _items = new()
		    { 
		        "Item 1", "Item 2", "Item 3" 
		    };
		    private string? _selected;
		}
		""";
}
