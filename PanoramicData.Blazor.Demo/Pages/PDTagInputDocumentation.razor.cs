namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTagInputDocumentation
{
	private List<string> _quickStartTags = ["Production"];

	private const string _example1Code = """
		<PDTagInput @bind-Values="_tags"
		            Suggestions="@(new[] { "Production", "Development", "Test" })" />

		@code {
		    private List<string> _tags = ["Production"];
		}
		""";
}
