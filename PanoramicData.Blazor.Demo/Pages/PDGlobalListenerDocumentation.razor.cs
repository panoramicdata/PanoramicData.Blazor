namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDGlobalListenerDocumentation
{
	private const string _example1Code = """
		<PDGlobalListener 
		    KeyDown="OnKeyDown" />

		@code {
		    private void OnKeyDown(
		        KeyboardEventArgs e)
		    {
		        Console.WriteLine(e.Key);
		    }
		}
		""";
}
