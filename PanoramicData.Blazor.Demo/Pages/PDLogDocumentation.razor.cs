namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDLogDocumentation
{
	private const string _example1Code = """
		<PDLog @ref="_log" 
		       MaxEntries="100" 
		       AutoScroll="true" />

		@code {
		    private PDLog? _log;

		    private void AddLog()
		    {
		        _log?.AddEntry(
		            "Message", 
		            LogSeverity.Info);
		    }
		}
		""";
}
