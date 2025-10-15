namespace PanoramicData.Blazor;

public partial class PDAudioChannel
{
	/// <summary>
	/// Gets or sets the label for the channel (displayed on the fader).
	/// </summary>
	[Parameter]
	public string Label { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the channel is enabled.
	/// </summary>
	[Parameter]
	public bool IsEnabled { get; set; } = true;

	// Gain
	[Parameter] public double GainValue { get; set; } = 0.5;
	[Parameter] public EventCallback<double> GainValueChanged { get; set; }
	[Parameter] public string GainColor { get; set; } = "red";

	// Compressor
	[Parameter] public double CompValue { get; set; } = 0.5;
	[Parameter] public EventCallback<double> CompValueChanged { get; set; }
	[Parameter] public string CompColor { get; set; } = "blue";

	// EQ
	[Parameter] public double EqHighValue { get; set; } = 0.5;
	[Parameter] public EventCallback<double> EqHighValueChanged { get; set; }
	[Parameter] public double EqMidValue { get; set; } = 0.5;
	[Parameter] public EventCallback<double> EqMidValueChanged { get; set; }
	[Parameter] public double EqLowValue { get; set; } = 0.5;
	[Parameter] public EventCallback<double> EqLowValueChanged { get; set; }
	[Parameter] public string EqColor { get; set; } = "#aa8";

	// DSP
	[Parameter] public double DspValue { get; set; } = 0.5;
	[Parameter] public EventCallback<double> DspValueChanged { get; set; }
	[Parameter] public string DspColor { get; set; } = "green";

	// Pan
	[Parameter] public double PanValue { get; set; } = 0.5;
	[Parameter] public EventCallback<double> PanValueChanged { get; set; }
	[Parameter] public string PanColor { get; set; } = "purple";

	// PFL Button
	[Parameter] public double PflValue { get; set; }
	[Parameter] public EventCallback<double> PflValueChanged { get; set; }
	[Parameter] public string PflActiveColor { get; set; } = "#0f0";
	[Parameter] public string PflInactiveColor { get; set; } = "#040";

	// Mute Button
	[Parameter] public double MuteValue { get; set; }
	[Parameter] public EventCallback<double> MuteValueChanged { get; set; }
	[Parameter] public string MuteActiveColor { get; set; } = "#f00";
	[Parameter] public string MuteInactiveColor { get; set; } = "#400";

	// Fader
	[Parameter] public double FaderValue { get; set; }
	[Parameter] public EventCallback<double> FaderValueChanged { get; set; }

	// Event handlers
	private async Task OnGainChanged(double value)
	{
		GainValue = value;
		await GainValueChanged.InvokeAsync(value);
	}

	private async Task OnCompChanged(double value)
	{
		CompValue = value;
		await CompValueChanged.InvokeAsync(value);
	}

	private async Task OnEqHighChanged(double value)
	{
		EqHighValue = value;
		await EqHighValueChanged.InvokeAsync(value);
	}

	private async Task OnEqMidChanged(double value)
	{
		EqMidValue = value;
		await EqMidValueChanged.InvokeAsync(value);
	}

	private async Task OnEqLowChanged(double value)
	{
		EqLowValue = value;
		await EqLowValueChanged.InvokeAsync(value);
	}

	private async Task OnDspChanged(double value)
	{
		DspValue = value;
		await DspValueChanged.InvokeAsync(value);
	}

	private async Task OnPanChanged(double value)
	{
		PanValue = value;
		await PanValueChanged.InvokeAsync(value);
	}

	private async Task OnPflChanged(double value)
	{
		PflValue = value;
		await PflValueChanged.InvokeAsync(value);
	}

	private async Task OnMuteChanged(double value)
	{
		MuteValue = value;
		await MuteValueChanged.InvokeAsync(value);
	}

	private async Task OnFaderChanged(double value)
	{
		FaderValue = value;
		await FaderValueChanged.InvokeAsync(value);
	}
}
