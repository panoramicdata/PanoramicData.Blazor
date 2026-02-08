namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDConfirmDocumentation
{
	private PDConfirm? _confirm;
	private string _result = "(none)";

	private async Task ShowConfirm()
	{
		if (_confirm != null)
		{
			await _confirm.ShowAsync().ConfigureAwait(true);
			_result = "Dialog shown";
		}
	}

	private const string _example1Code = """
		<button class="btn btn-danger" 
		        @onclick="ShowConfirm">
		    Delete Item
		</button>

		<PDConfirm @ref="_confirm"
		           Title="Confirm Delete"
		           Message="Are you sure?" />

		@code {
		    private PDConfirm? _confirm;

		    private async Task ShowConfirm()
		    {
		        await _confirm!.ShowAsync();
		        // Handle confirmation via events
		    }
		}
		""";
}
