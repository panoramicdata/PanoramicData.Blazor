using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDConfirmPage
{
	[AllowNull]
	private PDConfirm _confirmModal1 = null!;
	private PDConfirm.Outcomes? _result1;

	[AllowNull]
	private PDConfirm _confirmModal2 = null!;
	private PDConfirm.Outcomes? _result2;
	private CancellationTokenSource _cancellationToken2 = new();

	[AllowNull]
	private PDConfirm _confirmModal3 = null!;
	private PDConfirm.Outcomes? _result3;

	[AllowNull]
	private PDConfirm _confirmModal4 = null!;
	private PDConfirm.Outcomes? _result4;

	private async Task OnAction1ClickAsync()
	{
		_result1 = await _confirmModal1!
			.ShowAndWaitResultAsync()
			.ConfigureAwait(true);
		if (_result1 == PDConfirm.Outcomes.Yes)
		{
			// DO ACTION 1!!!
		}
	}

	private async Task OnAction2ClickAsync()
	{
		_cancellationToken2 = new();
		_result2 = await _confirmModal2!
			.ShowAndWaitResultAsync()
			.ConfigureAwait(true);
		if (_result2 == PDConfirm.Outcomes.Yes)
		{
			// DO ACTION 2!!!
		}
	}

	private async Task OnAction3ClickAsync()
	{
		_result3 = await _confirmModal3!
			.ShowAndWaitResultAsync("Do you want to do action 3?", "Action 3")
			.ConfigureAwait(true);
		if (_result3 == PDConfirm.Outcomes.Yes)
		{
			// DO ACTION 3!!!
		}
	}

	private async Task OnAction4ClickAsync()
	{
		_result3 = await _confirmModal3!
			.ShowAndWaitResultAsync("Do you want to do action 4?", "Action 4")
			.ConfigureAwait(true);
		if (_result3 == PDConfirm.Outcomes.Yes)
		{
			// DO ACTION 4!!!
		}
	}

	private async Task OnAction5ClickAsync()
	{
		_result4 = await _confirmModal4!
			.ShowAndWaitResultAsync()
			.ConfigureAwait(true);
		if (_result4 == PDConfirm.Outcomes.Yes)
		{
			// DO ACTION 5!!!
		}
	}

	private void OnCancelTokenClick() =>
		// simulation of a task cancellation
		_cancellationToken2.Cancel();
}
