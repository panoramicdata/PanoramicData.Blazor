namespace PanoramicData.Blazor;

/// <summary>
/// PDAudioPad: the decay of the value back to the minimum in the Linear and Exponential decay modes.
/// </summary>
public partial class PDAudioPad
{
	internal async Task DecayAsync(CancellationToken cancellationToken)
	{
		var startTime = DateTime.UtcNow;
		var initialValue = Value;

		while (!cancellationToken.IsCancellationRequested && Value > Math.Max(MinValue, ZeroBelow ?? 0))
		{
			await Task.Delay(10, cancellationToken).ConfigureAwait(true);
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}

			Value = Math.Max(GetDecayedValue(initialValue, DateTime.UtcNow - startTime), MinValue);

			await ValueChanged.InvokeAsync(Value);
			await EmitValueChangedEvent(); // Throttled emission
			await InvokeAsync(StateHasChanged);
		}

		// A newer press superseded this decay: that press owns the value now, so do not settle it at the
		// minimum or report the pad inactive while the new decay is running (issue #219).
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}

		await SettleDecayAsync();
	}

	// Only the Exponential and Linear modes decay: ActivateAsync never starts a decay in Toggle mode.
	private double GetDecayedValue(double initialValue, TimeSpan elapsed)
		=> DecayMode == DecayMode.Exponential
			? initialValue * Math.Pow(0.5, elapsed.TotalMilliseconds / DecayHalfLife.TotalMilliseconds)
			: initialValue - (elapsed.TotalMilliseconds / (DecayHalfLife.TotalMilliseconds * 2));

	private async Task SettleDecayAsync()
	{
		if (Value != MinValue)
		{
			Value = MinValue;
			await ValueChanged.InvokeAsync(Value);
			await EmitValueChangedEvent(forceEmit: true); // Emit final event when decay completes
			await InvokeAsync(StateHasChanged);
		}
		else if (!_lastEmittedValue.Equals(Value))
		{
			// The loop already reached the minimum (Linear decay clamps to it), but throttling may have
			// suppressed that emission: report the final state so a listener always sees the pad go inactive
			await EmitValueChangedEvent(forceEmit: true);
			await InvokeAsync(StateHasChanged);
		}
	}
}
