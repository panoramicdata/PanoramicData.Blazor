namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFileExplorerDocumentation
{
	private const string _example1Code = """
		<PDFileExplorer 
		    DataProvider="_dataProvider"
		    AllowUpload="true"
		    AllowDownload="true"
		    AllowDelete="true" />

		@code {
		    // DataProvider implements 
		    // IFileExplorerDataProvider
		}
		""";
}
