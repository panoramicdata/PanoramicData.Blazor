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

	// PFL Button - changed to double to match PDAudioButton
	[Parameter] public double PflValue { get; set; }
	[Parameter] public EventCallback<double> PflValueChanged { get; set; }
	[Parameter] public string PflActiveColor { get; set; } = "#0f0";
	[Parameter] public string PflInactiveColor { get; set; } = "#040";

	// Mute Button - changed to double to match PDAudioButton
	[Parameter] public double MuteValue { get; set; }
	[Parameter] public EventCallback<double> MuteValueChanged { get; set; }
	[Parameter] public string MuteActiveColor { get; set; } = "#f00";
	[Parameter] public string MuteInactiveColor { get; set; } = "#400";

	// Fader
	[Parameter] public double FaderValue { get; set; }
	[Parameter] public EventCallback<double> FaderValueChanged { get; set; }

	// Event handlers
	private Task OnGainChanged(double value) => GainValueChanged.InvokeAsync(value);
	private Task OnCompChanged(double value) => CompValueChanged.InvokeAsync(value);
	private Task OnEqHighChanged(double value) => EqHighValueChanged.InvokeAsync(value);
	private Task OnEqMidChanged(double value) => EqMidValueChanged.InvokeAsync(value);
	private Task OnEqLowChanged(double value) => EqLowValueChanged.InvokeAsync(value);
	private Task OnDspChanged(double value) => DspValueChanged.InvokeAsync(value);
	private Task OnPanChanged(double value) => PanValueChanged.InvokeAsync(value);
	private Task OnPflChanged(double value) => PflValueChanged.InvokeAsync(value);
	private Task OnMuteChanged(double value) => MuteValueChanged.InvokeAsync(value);
	private Task OnFaderChanged(double value) => FaderValueChanged.InvokeAsync(value);
}
