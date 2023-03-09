using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDConfirmPage
{
	[AllowNull]
	private PDConfirm _confirmModal1 = null!;
	private string _result1 = string.Empty;

	[AllowNull]
	private PDConfirm _confirmModal2 = null!;
	private string _result2 = string.Empty;
	private CancellationTokenSource _cancellationToken2 = new CancellationTokenSource();

	[AllowNull]
	private PDConfirm _confirmModal3 = null!;
	private string _result3 = string.Empty;

	[AllowNull]
	private PDConfirm _confirmModal4 = null!;
	private string _result4 = string.Empty;

	private async Task OnAction1ClickAsync()
	{
		_result1 = await _confirmModal1!
			.ShowAndWaitResultAsync()
			.ConfigureAwait(true);
	}

	private async Task OnAction2ClickAsync()
	{
		_cancellationToken2 = new();
		_result2 = await _confirmModal2!
			.ShowAndWaitResultAsync(_cancellationToken2.Token)
			.ConfigureAwait(true);
	}

	private async Task OnAction3ClickAsync()
	{
		_result3 = await _confirmModal3!
			.ShowAndWaitResultAsync("Do you want to do action 3?", "Action 3")
			.ConfigureAwait(true);
	}

	private async Task OnAction4ClickAsync()
	{
		_result3 = await _confirmModal3!
			.ShowAndWaitResultAsync("Do you want to do action 4?", "Action 4")
			.ConfigureAwait(true);
	}

	private async Task OnAction5ClickAsync()
	{
		_result4 = await _confirmModal4!
			.ShowAndWaitResultAsync()
			.ConfigureAwait(true);
	}

	private void OnCancelTokenClick()
	{
		_cancellationToken2.Cancel();
	}
}
