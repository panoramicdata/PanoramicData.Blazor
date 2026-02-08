namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTextBoxDocumentation
{
	private string _text = "";

	private const string _example1Code = """
		<PDTextBox @bind-Value="_text" 
		           Placeholder="Enter text..." />
		<p>Value: @_text</p>

		@code {
		    private string _text = "";
		}
		""";
}
