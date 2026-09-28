using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDAudioChannel"/> lays out its knobs, buttons and fader with the configured colours,
/// and relays each control's change to the matching channel callback.
/// </summary>
public class PDAudioChannelTests : BunitContext
{
	private readonly Dictionary<string, double> _raised = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDAudioChannelTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<PDAudioChannel> RenderChannel(bool isEnabled = true)
		=> Render<PDAudioChannel>(parameters => parameters
			.Add(p => p.Label, "Ch 1")
			.Add(p => p.IsEnabled, isEnabled)
			.Add(p => p.GainColor, "gold")
			.Add(p => p.PanValue, 0.3)
			.Add(p => p.GainValueChanged, (double v) => _raised["Gain"] = v)
			.Add(p => p.CompValueChanged, (double v) => _raised["Comp"] = v)
			.Add(p => p.EqHighValueChanged, (double v) => _raised["EQ High"] = v)
			.Add(p => p.EqMidValueChanged, (double v) => _raised["EQ Mid"] = v)
			.Add(p => p.EqLowValueChanged, (double v) => _raised["EQ Low"] = v)
			.Add(p => p.DspValueChanged, (double v) => _raised["DSP"] = v)
			.Add(p => p.PanValueChanged, (double v) => _raised["Pan"] = v)
			.Add(p => p.PflValueChanged, (double v) => _raised["PFL"] = v)
			.Add(p => p.MuteValueChanged, (double v) => _raised["Mute"] = v)
			.Add(p => p.FaderValueChanged, (double v) => _raised["Fader"] = v));

	/// <summary>
	/// Verifies the channel's controls: seven labelled knobs with the pan knob in balance mode, the PFL and mute
	/// buttons, and a fader carrying the channel label.
	/// </summary>
	[Fact]
	public void Channel_LaysOutItsControls()
	{
		var channel = RenderChannel();

		var knobs = channel.FindComponents<PDKnob>().Select(k => k.Instance).ToList();
		knobs.Select(k => k.Label).Should().Equal("Gain", "Comp", "EQ High", "EQ Mid", "EQ Low", "DSP", "Pan");
		knobs[0].CapColor.Should().Be("gold");
		knobs[0].MaxDisplay.Should().Be(11);
		knobs.Skip(1).Should().OnlyContain(k => k.MaxDisplay == 10);
		knobs[6].Mode.Should().Be(PDKnobMode.Balance);
		knobs[6].Value.Should().Be(0.3);
		knobs[6].CapColor.Should().Be("purple");

		var buttons = channel.FindComponents<PDAudioButton>().Select(b => b.Instance).ToList();
		buttons.Select(b => b.Label).Should().Equal("PFL", "Mute");
		buttons[0].ActiveColor.Should().Be("#0f0");
		buttons[1].ActiveColor.Should().Be("#f00");

		channel.FindComponent<PDFader>().Instance.Label.Should().Be("Ch 1");
	}

	/// <summary>
	/// Verifies that the channel's enabled state reaches every control.
	/// </summary>
	[Fact]
	public void Disabled_ReachesEveryControl()
	{
		var channel = RenderChannel(isEnabled: false);

		channel.FindComponents<PDKnob>().Should().OnlyContain(k => !k.Instance.IsEnabled);
		channel.FindComponents<PDAudioButton>().Should().OnlyContain(b => !b.Instance.IsEnabled);
		channel.FindComponent<PDFader>().Instance.IsEnabled.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that a change on each knob is stored on the channel and raised through its own callback.
	/// </summary>
	[Fact]
	public async Task KnobChanges_AreRelayed()
	{
		var channel = RenderChannel();
		var knobs = channel.FindComponents<PDKnob>();

		for (var i = 0; i < knobs.Count; i++)
		{
			var value = (i + 1) / 10.0;
			var knob = knobs[i];
			await channel.InvokeAsync(() => knob.Instance.ValueChanged.InvokeAsync(value));
		}

		_raised.Should().BeEquivalentTo(new Dictionary<string, double>
		{
			["Gain"] = 0.1, ["Comp"] = 0.2, ["EQ High"] = 0.3, ["EQ Mid"] = 0.4,
			["EQ Low"] = 0.5, ["DSP"] = 0.6, ["Pan"] = 0.7
		});

		var instance = channel.Instance;
		new[] { instance.GainValue, instance.CompValue, instance.EqHighValue, instance.EqMidValue,
			instance.EqLowValue, instance.DspValue, instance.PanValue }
			.Should().Equal(0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7);
	}

	/// <summary>
	/// Verifies that pressing the PFL and mute buttons and moving the fader are stored and raised.
	/// </summary>
	[Fact]
	public async Task ButtonAndFaderChanges_AreRelayed()
	{
		var channel = RenderChannel();

		var buttons = channel.FindAll(".pd-audio-button .button-container");
		buttons[0].Click();
		channel.FindAll(".pd-audio-button .button-container")[1].Click();
		var fader = channel.FindComponent<PDFader>();
		await channel.InvokeAsync(() => fader.Instance.ValueChanged.InvokeAsync(0.8));

		_raised.Should().BeEquivalentTo(new Dictionary<string, double> { ["PFL"] = 1, ["Mute"] = 1, ["Fader"] = 0.8 });
		channel.Instance.PflValue.Should().Be(1);
		channel.Instance.MuteValue.Should().Be(1);
		channel.Instance.FaderValue.Should().Be(0.8);
		channel.FindAll(".pd-audio-button .button-container.pressed").Should().HaveCount(2);
	}
}
