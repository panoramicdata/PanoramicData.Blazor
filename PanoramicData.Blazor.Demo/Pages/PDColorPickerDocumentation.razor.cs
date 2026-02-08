namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDColorPickerDocumentation
{
	private string _color = "#3498db";

	private const string _example1Code = """
		<PDColorPicker @bind-Value="_color" 
		               Text="Pick Color" />

		<div style="background-color: @_color; 
		            width: 40px; height: 40px;">
		</div>

		@code {
		    private string _color = "#3498db";
		}
		""";
}
