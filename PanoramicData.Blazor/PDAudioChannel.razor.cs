namespace PanoramicData.Blazor;

/// <summary>
/// Composite audio channel UI containing gain, EQ, DSP, pan, PFL, mute, and fader controls.
/// </summary>
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
	/// <summary>
	/// Gets or sets gain control value.
	/// </summary>
	[Parameter] public double GainValue { get; set; } = 0.5;
	/// <summary>
	/// Gets or sets callback fired when gain value changes.
	/// </summary>
	[Parameter] public EventCallback<double> GainValueChanged { get; set; }
	/// <summary>
	/// Gets or sets color used by gain control.
	/// </summary>
	[Parameter] public string GainColor { get; set; } = "red";

	// Compressor
	/// <summary>
	/// Gets or sets compressor control value.
	/// </summary>
	[Parameter] public double CompValue { get; set; } = 0.5;
	/// <summary>
	/// Gets or sets callback fired when compressor value changes.
	/// </summary>
	[Parameter] public EventCallback<double> CompValueChanged { get; set; }
	/// <summary>
	/// Gets or sets color used by compressor control.
	/// </summary>
	[Parameter] public string CompColor { get; set; } = "blue";

	// EQ
	/// <summary>
	/// Gets or sets high-band EQ value.
	/// </summary>
	[Parameter] public double EqHighValue { get; set; } = 0.5;
	/// <summary>
	/// Gets or sets callback fired when high-band EQ value changes.
	/// </summary>
	[Parameter] public EventCallback<double> EqHighValueChanged { get; set; }
	/// <summary>
	/// Gets or sets mid-band EQ value.
	/// </summary>
	[Parameter] public double EqMidValue { get; set; } = 0.5;
	/// <summary>
	/// Gets or sets callback fired when mid-band EQ value changes.
	/// </summary>
	[Parameter] public EventCallback<double> EqMidValueChanged { get; set; }
	/// <summary>
	/// Gets or sets low-band EQ value.
	/// </summary>
	[Parameter] public double EqLowValue { get; set; } = 0.5;
	/// <summary>
	/// Gets or sets callback fired when low EQ value changes.
	/// </summary>
	[Parameter] public EventCallback<double> EqLowValueChanged { get; set; }
	/// <summary>
	/// Gets or sets color used by EQ controls.
	/// </summary>
	[Parameter] public string EqColor { get; set; } = "#aa8";

	// DSP
	/// <summary>
	/// Gets or sets DSP control value.
	/// </summary>
	[Parameter] public double DspValue { get; set; } = 0.5;
	/// <summary>
	/// Gets or sets callback fired when DSP value changes.
	/// </summary>
	[Parameter] public EventCallback<double> DspValueChanged { get; set; }
	/// <summary>
	/// Gets or sets color used by the DSP control.
	/// </summary>
	[Parameter] public string DspColor { get; set; } = "green";

	// Pan
	/// <summary>
	/// Gets or sets pan control value.
	/// </summary>
	[Parameter] public double PanValue { get; set; } = 0.5;
	/// <summary>
	/// Gets or sets callback fired when pan value changes.
	/// </summary>
	[Parameter] public EventCallback<double> PanValueChanged { get; set; }
	/// <summary>
	/// Gets or sets color used by pan control.
	/// </summary>
	[Parameter] public string PanColor { get; set; } = "purple";

	// PFL Button
	/// <summary>
	/// Gets or sets pre-fade-listen button state value.
	/// </summary>
	[Parameter] public double PflValue { get; set; }
	/// <summary>
	/// Gets or sets callback fired when PFL value changes.
	/// </summary>
	[Parameter] public EventCallback<double> PflValueChanged { get; set; }
	/// <summary>
	/// Gets or sets active color for PFL button.
	/// </summary>
	[Parameter] public string PflActiveColor { get; set; } = "#0f0";
	/// <summary>
	/// Gets or sets inactive color for PFL button.
	/// </summary>
	[Parameter] public string PflInactiveColor { get; set; } = "#040";

	// Mute Button
	/// <summary>
	/// Gets or sets mute button state value.
	/// </summary>
	[Parameter] public double MuteValue { get; set; }
	/// <summary>
	/// Gets or sets callback fired when mute value changes.
	/// </summary>
	[Parameter] public EventCallback<double> MuteValueChanged { get; set; }
	/// <summary>
	/// Gets or sets active color for mute button.
	/// </summary>
	[Parameter] public string MuteActiveColor { get; set; } = "#f00";
	/// <summary>
	/// Gets or sets inactive color for mute button.
	/// </summary>
	[Parameter] public string MuteInactiveColor { get; set; } = "#400";

	// Fader
	/// <summary>
	/// Gets or sets channel fader value.
	/// </summary>
	[Parameter] public double FaderValue { get; set; }
	/// <summary>
	/// Gets or sets callback fired when fader value changes.
	/// </summary>
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
