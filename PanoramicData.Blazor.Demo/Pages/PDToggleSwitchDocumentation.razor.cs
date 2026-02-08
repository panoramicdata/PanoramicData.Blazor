namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDToggleSwitchDocumentation
{
	private bool _isEnabled = true;

	private const string _example1Code = """
		<PDToggleSwitch @bind-Value="_isEnabled" 
		                Label="Enable Feature" />

		@code {
		    private bool _isEnabled = true;
		}
		""";
}
