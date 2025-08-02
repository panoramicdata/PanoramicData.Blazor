namespace PanoramicData.Blazor;

public partial class PDAudioPad : PDAudioControl, IAsyncDisposable
{
	private CancellationTokenSource? _cts;

	[Parameter] public string ActiveColor { get; set; } = "#ff0";
	[Parameter] public string InactiveColor { get; set; } = "#444";

	private string DisplayColor => ColorExtensions.Interpolate(InactiveColor, ActiveColor, Value);

	[Parameter] public DecayMode DecayMode { get; set; } = DecayMode.Toggle;
	[Parameter] public DecayUpon DecayUpon { get; set; } = DecayUpon.Press;
	[Parameter] public TimeSpan DecayHalfLife { get; set; } = TimeSpan.FromMilliseconds(250);
	[Parameter] public double? ZeroBelow { get; set; } = 0.01;
	[Parameter] public double MinValue { get; set; } = 0.0;
	[Parameter] public int Width { get; set; } = 60;
	[Parameter] public int Height { get; set; } = 60;
	[Parameter] public Symbol? Symbol { get; set; }
	[Parameter] public string? SymbolColor { get; set; } = null;
	[Parameter] public string? LabelColor { get; set; } = null;

	private string SymbolColorInternal => SymbolColor ?? (Value > 0.5 ? "black" : "white");

	private string OverlayLabelColor => LabelColor ?? "black";

	protected override string JsFileName => string.Empty;

	    private async void Activate()
	{
		_cts?.Cancel();
		_cts = new CancellationTokenSource();

		if (DecayMode == DecayMode.Toggle)
		{
			Value = Value > 0.5 ? MinValue : 1;
			await ValueChanged.InvokeAsync(Value);
		}
		else
		{
			Value = 1;
			await ValueChanged.InvokeAsync(Value);
			_ = DecayAsync(_cts.Token);
		}
	}

	private async Task DecayAsync(CancellationToken cancellationToken)
	{
		var startTime = DateTime.UtcNow;
		var initialValue = Value;

		while (!cancellationToken.IsCancellationRequested && Value > MinValue && Value > (ZeroBelow ?? 0))
		{
			await Task.Delay(10, cancellationToken).ConfigureAwait(true);
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}

			var elapsed = DateTime.UtcNow - startTime;

			if (DecayMode == DecayMode.Exponential)
			{
				var decayFactor = Math.Pow(0.5, elapsed.TotalMilliseconds / DecayHalfLife.TotalMilliseconds);
				Value = initialValue * decayFactor;
			}
			else if (DecayMode == DecayMode.Linear)
			{
				var decayAmount = elapsed.TotalMilliseconds / (DecayHalfLife.TotalMilliseconds * 2);
				Value = initialValue - decayAmount;
			}

			if (Value < MinValue)
			{
				Value = MinValue;
			}

			await ValueChanged.InvokeAsync(Value);
			await InvokeAsync(StateHasChanged);
		}

		if (Value != MinValue)
		{
			Value = MinValue;
			await ValueChanged.InvokeAsync(Value);
			await InvokeAsync(StateHasChanged);
		}
	}

	protected void HandlePress()
	{
		if (DecayUpon == DecayUpon.Press)
		{
			Activate();
		}
	}

	protected void HandleRelease()
	{
		if (DecayUpon == DecayUpon.Release)
		{
			Activate();
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

	public new async ValueTask DisposeAsync()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		GC.SuppressFinalize(this);
		await Task.CompletedTask;
	}
}
