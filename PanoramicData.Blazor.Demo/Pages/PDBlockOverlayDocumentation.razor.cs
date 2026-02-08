namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDBlockOverlayDocumentation
{
	private bool _isLoading = true;

	private void ToggleLoading() => _isLoading = !_isLoading;

	private const string _example1Code = """
		<div style="position: relative; height: 150px;">
		    <p>Content underneath</p>
		    <PDBlockOverlay IsVisible="@_isLoading" 
		                    Message="Loading data..." />
		</div>

		<button @onclick="() => _isLoading = !_isLoading">
		    Toggle Overlay
		</button>

		@code {
		    private bool _isLoading = true;
		}
		""";
}
