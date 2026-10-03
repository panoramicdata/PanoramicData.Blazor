namespace PanoramicData.Blazor;

/// <summary>
/// Audio pad control with toggle or decay-based activation behavior.
/// </summary>
public partial class PDAudioPad : PDAudioControl, IAsyncDisposable
{
	private CancellationTokenSource? _cts;
	private DateTime _lastEventEmitTime = DateTime.MinValue;
	private double _lastEmittedValue = double.NaN;

	/// <summary>
	/// Gets or sets active color used when pad value is high.
	/// </summary>
	[Parameter] public string ActiveColor { get; set; } = "#ff0";
	/// <summary>
	/// Gets or sets inactive color used when pad value is low.
	/// </summary>
	[Parameter] public string InactiveColor { get; set; } = "#444";

	private string DisplayColor => ColorExtensions.Interpolate(InactiveColor, ActiveColor, Value);

	/// <summary>
	/// Gets or sets decay behavior mode.
	/// </summary>
	[Parameter] public DecayMode DecayMode { get; set; } = DecayMode.Toggle;
	/// <summary>
	/// Gets or sets whether activation starts on press or release.
	/// </summary>
	[Parameter] public DecayUpon DecayUpon { get; set; } = DecayUpon.Press;
	/// <summary>
	/// Gets or sets half-life duration used by exponential and linear decay.
	/// </summary>
	[Parameter] public TimeSpan DecayHalfLife { get; set; } = TimeSpan.FromMilliseconds(250);
	/// <summary>
	/// Gets or sets threshold below which values are treated as zero during decay.
	/// </summary>
	[Parameter] public double? ZeroBelow { get; set; } = 0.01;
	/// <summary>
	/// Gets or sets minimum output value for this pad.
	/// </summary>
	[Parameter] public double MinValue { get; set; }
	/// <summary>
	/// Gets or sets the pad width in pixels.
	/// </summary>
	[Parameter] public int Width { get; set; } = 60;
	/// <summary>
	/// Gets or sets the pad height in pixels.
	/// </summary>
	[Parameter] public int Height { get; set; } = 60;
	/// <summary>
	/// Gets or sets an optional symbol rendered on the pad.
	/// </summary>
	[Parameter] public Symbol? Symbol { get; set; }
	/// <summary>
	/// Gets or sets optional symbol color override.
	/// </summary>
	[Parameter] public string? SymbolColor { get; set; }
	/// <summary>
	/// Gets or sets optional overlay label color override.
	/// </summary>
	[Parameter] public string? LabelColor { get; set; }

	/// <summary>
	/// Throttle interval in milliseconds for decay events (default 100ms).
	/// Only applies to Linear and Exponential decay modes to prevent event spam.
	/// </summary>
	[Parameter] public int EventThrottleMs { get; set; } = 100;

	/// <summary>
	/// Event callback fired when the pad value changes.
	/// For toggle mode: fires on each toggle.
	/// For decay modes: throttled to EventThrottleMs interval.
	/// </summary>
	[Parameter] public EventCallback<PDAudioPadEventArgs> OnPadValueChanged { get; set; }

	private string SymbolColorInternal => SymbolColor ?? (Value > 0.5 ? "black" : "white");

	private string OverlayLabelColor => LabelColor ?? "black";

	private async Task ActivateAsync()
	{
		_cts?.Cancel();
		_cts = new CancellationTokenSource();

		if (DecayMode == DecayMode.Toggle)
		{
			Value = Value > 0.5 ? MinValue : 1;
			await ValueChanged.InvokeAsync(Value);
			await EmitValueChangedEvent(forceEmit: true); // Always emit for toggle
		}
		else
		{
			Value = 1;
			await ValueChanged.InvokeAsync(Value);
			await EmitValueChangedEvent(forceEmit: true); // Emit activation event
			_ = DecayAsync(_cts.Token);
		}
	}

	private async Task EmitValueChangedEvent(bool forceEmit = false)
	{
		var now = DateTime.UtcNow;

		// Toggle mode and forced emissions always emit; the decay modes throttle the events
		if (!forceEmit && DecayMode != DecayMode.Toggle && (now - _lastEventEmitTime).TotalMilliseconds < EventThrottleMs)
		{
			return;
		}

		await OnPadValueChanged.InvokeAsync(new PDAudioPadEventArgs
		{
			Label = Label,
			Value = Value,
			DecayMode = DecayMode,
			IsActive = Value > 0.5
		});

		_lastEventEmitTime = now;
		_lastEmittedValue = Value;
	}

	/// <summary>
	/// Handles press interaction and triggers activation when configured for press behavior.
	/// </summary>
	/// <returns>A task that completes once the activation has been reported.</returns>
	protected async Task HandlePress()
	{
		if (DecayUpon == DecayUpon.Press)
		{
			await ActivateAsync().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Handles release interaction and triggers activation when configured for release behavior.
	/// </summary>
	/// <returns>A task that completes once the activation has been reported.</returns>
	protected async Task HandleRelease()
	{
		if (DecayUpon == DecayUpon.Release)
		{
			await ActivateAsync().ConfigureAwait(true);
		}
	}

	private string SymbolClass => Symbol switch
	{
		Blazor.Symbol.PreviousTrack => "fas fa-backward",
		Blazor.Symbol.Play => "fas fa-play",
		Blazor.Symbol.Pause => "fas fa-pause",
		Blazor.Symbol.NextTrack => "fas fa-forward",
		_ => string.Empty
	};

	/// <summary>
	/// Disposes resources used by this audio pad.
	/// </summary>
	public new async ValueTask DisposeAsync()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		GC.SuppressFinalize(this);
		await Task.CompletedTask;
	}
}

/// <summary>
/// Event arguments for PDAudioPad value changes.
/// </summary>
public class PDAudioPadEventArgs : EventArgs
{
	/// <summary>
	/// The label of the pad that changed.
	/// </summary>
	public string? Label { get; set; }

	/// <summary>
	/// The current value (0.0 to 1.0).
	/// </summary>
	public double Value { get; set; }

	/// <summary>
	/// The decay mode of the pad.
	/// </summary>
	public DecayMode DecayMode { get; set; }

	/// <summary>
	/// Whether the pad is currently active (value > 0.5).
	/// </summary>
	public bool IsActive { get; set; }
}
