using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDConfirmPage
{
	[AllowNull]
	protected PDConfirm ConfirmModal1 { get; set; } = null!;
	private PDConfirm.Outcomes? _result1;

	[AllowNull]
	protected PDConfirm ConfirmModal2 { get; set; } = null!;
	private PDConfirm.Outcomes? _result2;
	private CancellationTokenSource _cancellationToken2 = new();

	[AllowNull]
	protected PDConfirm ConfirmModal3 { get; set; } = null!;
	private PDConfirm.Outcomes? _result3;

	[AllowNull]
	protected PDConfirm ConfirmModal4 { get; set; } = null!;
	private PDConfirm.Outcomes? _result4;

	private async Task OnAction1ClickAsync()
	{
		_result1 = await ConfirmModal1!
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
		_result2 = await ConfirmModal2!
			.ShowAndWaitResultAsync(_cancellationToken2.Token)
			.ConfigureAwait(true);
		if (_result2 == PDConfirm.Outcomes.Yes)
		{
			// DO ACTION 2!!!
		}
	}

	private async Task OnAction3ClickAsync()
	{
		_result3 = await ConfirmModal3!
			.ShowAndWaitResultAsync("Do you want to do action 3?", "Action 3")
			.ConfigureAwait(true);
		if (_result3 == PDConfirm.Outcomes.Yes)
		{
			// DO ACTION 3!!!
		}
	}

	private async Task OnAction4ClickAsync()
	{
		_result3 = await ConfirmModal3!
			.ShowAndWaitResultAsync("Do you want to do action 4?", "Action 4")
			.ConfigureAwait(true);
		if (_result3 == PDConfirm.Outcomes.Yes)
		{
			// DO ACTION 4!!!
		}
	}

	private async Task OnAction5ClickAsync()
	{
		_result4 = await ConfirmModal4!
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
