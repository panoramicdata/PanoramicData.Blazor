namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDModalDocumentation
{
	private PDModal? _modal;

	private async Task ShowModal()
	{
		if (_modal != null)
		{
			await _modal.ShowAsync().ConfigureAwait(true);
		}
	}

	private const string _example1Code = """
		<button class="btn btn-primary" 
		        @onclick="ShowModal">
		    Open Modal
		</button>

		<PDModal @ref="_modal" Title="Example Modal">
		    <p>This is the modal content.</p>
		</PDModal>

		@code {
		    private PDModal? _modal;

		    private async Task ShowModal()
		    {
		        await _modal!.ShowAsync();
		    }
		}
		""";
}
