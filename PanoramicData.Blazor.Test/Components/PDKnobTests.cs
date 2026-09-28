using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDKnob"/> draws its dial, markings and ticks for each mode, positions its label,
/// and turns pointer and double-click input into value changes.
/// </summary>
public class PDKnobTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDKnob.razor.js";
	private const string TickSelector = "line[stroke='#999']";

	/// <summary>Sets up the rendering context.</summary>
	public PDKnobTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private List<string> MarkLabels(IRenderedComponent<PDKnob> knob)
		=> [.. knob.FindAll("svg text").Select(t => t.TextContent)];

	/// <summary>
	/// Verifies the default volume dial: one marking and one tick per whole step from zero to eleven, sized by
	/// <see cref="PDKnob.SizePx"/>.
	/// </summary>
	[Fact]
	public void Volume_Default_MarksEveryStepToEleven()
	{
		var knob = Render<PDKnob>();

		var svg = knob.Find("svg");
		svg.GetAttribute("width").Should().Be("60");
		svg.GetAttribute("height").Should().Be("60");
		MarkLabels(knob).Should().Equal(Enumerable.Range(0, 12).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
		knob.FindAll(TickSelector).Should().HaveCount(12);
		knob.Find("circle").GetAttribute("fill").Should().Be("#eee");
		knob.Instance.SnapPoints.Should().Be(12);
	}

	/// <summary>
	/// Verifies that a larger range is marked in readable steps, always ending with the maximum.
	/// </summary>
	/// <param name="maxDisplay">The maximum displayed value.</param>
	/// <param name="expected">The expected marking labels, comma separated.</param>
	[Theory]
	[InlineData(25, "0,5,10,15,20,25")]
	[InlineData(23, "0,5,10,15,20,23")]
	[InlineData(40, "0,5,10,15,20,25,30,35,40")]
	[InlineData(100, "0,10,20,30,40,50,60,70,80,90,100")]
	[InlineData(150, "0,20,40,60,80,100,120,140,150")]
	[InlineData(450, "0,50,100,150,200,250,300,350,400,450")]
	[InlineData(900, "0,100,200,300,400,500,600,700,800,900")]
	[InlineData(1500, "0,150,300,450,600,750,900,1050,1200,1350,1500")]
	public void Volume_LargeRange_UsesReadableSteps(int maxDisplay, string expected)
	{
		var knob = Render<PDKnob>(parameters => parameters.Add(p => p.MaxDisplay, maxDisplay));

		var labels = expected.Split(',');
		MarkLabels(knob).Should().Equal(labels);
		knob.FindAll(TickSelector).Should().HaveCount(labels.Length);
	}

	/// <summary>
	/// Verifies that ticks can be turned off without affecting the markings.
	/// </summary>
	[Fact]
	public void ShowTicksFalse_DrawsNoTicks()
	{
		var knob = Render<PDKnob>(parameters => parameters.Add(p => p.ShowTicks, false));

		knob.FindAll(TickSelector).Should().BeEmpty();
		MarkLabels(knob).Should().HaveCount(12);
	}

	/// <summary>
	/// Verifies the balance and gain dials: two end labels, no ticks, and no automatic snapping.
	/// </summary>
	/// <param name="mode">The knob mode.</param>
	/// <param name="min">The expected start label.</param>
	/// <param name="max">The expected end label.</param>
	[Theory]
	[InlineData(PDKnobMode.Balance, "L", "R")]
	[InlineData(PDKnobMode.Gain, "-∞", "+∞")]
	public void BalanceAndGain_ShowEndLabelsOnly(PDKnobMode mode, string min, string max)
	{
		var knob = Render<PDKnob>(parameters => parameters.Add(p => p.Mode, mode));

		MarkLabels(knob).Should().Equal(min, max);
		knob.FindAll(TickSelector).Should().BeEmpty();
		knob.Instance.SnapPoints.Should().BeNull();
	}

	/// <summary>
	/// Verifies that custom range labels replace the volume markings and suppress automatic snapping.
	/// </summary>
	[Fact]
	public void CustomLabels_ReplaceVolumeMarkings()
	{
		var knob = Render<PDKnob>(parameters => parameters
			.Add(p => p.MinLabel, "Low")
			.Add(p => p.MaxLabel, "High"));

		MarkLabels(knob).Should().Equal("Low", "High");
		knob.Instance.SnapPoints.Should().BeNull();
	}

	/// <summary>
	/// Verifies that the indicator and arc follow the value: straight up at the midpoint, and a large arc once the
	/// sweep passes 180 degrees.
	/// </summary>
	[Fact]
	public void Indicator_AndArc_FollowTheValue()
	{
		var knob = Render<PDKnob>(parameters => parameters
			.Add(p => p.Mode, PDKnobMode.Balance)
			.Add(p => p.ActiveColor, "orange")
			.Add(p => p.Value, 0.5));

		var indicator = knob.Find("line[stroke='#333']");
		indicator.GetAttribute("x2").Should().Be("30");
		indicator.GetAttribute("y2").Should().Be("15");
		var arc = knob.Find("path");
		arc.GetAttribute("stroke").Should().Be("orange");
		arc.GetAttribute("d").Should().Contain(" 0 0 0 ");

		knob.Render(parameters => parameters.Add(p => p.Value, 1.0));
		knob.Find("path").GetAttribute("d").Should().Contain(" 0 1 0 ");
	}

	/// <summary>
	/// Verifies that the label is placed above, below or over the dial, and omitted when empty.
	/// </summary>
	[Fact]
	public void Label_IsPositioned()
	{
		var above = Render<PDKnob>(parameters => parameters
			.Add(p => p.Label, "Gain")
			.Add(p => p.LabelCssClass, "lbl")
			.Add(p => p.LabelPosition, PDLabelPosition.Above));
		above.Find(".pd-knob").FirstElementChild!.ClassList.Should().Contain(["pd-knob-label", "lbl"]);

		var below = Render<PDKnob>(parameters => parameters.Add(p => p.Label, "Gain"));
		below.Find(".pd-knob").LastElementChild!.ClassName.Should().Contain("pd-knob-label");

		var overlay = Render<PDKnob>(parameters => parameters
			.Add(p => p.Label, "Gain")
			.Add(p => p.LabelPosition, PDLabelPosition.Overlay));
		overlay.Find(".pd-knob-label-overlay").TextContent.Should().Be("Gain");
		overlay.FindAll(".pd-knob-label").Should().BeEmpty();

		var none = Render<PDKnob>();
		none.FindAll(".pd-knob-label, .pd-knob-label-overlay").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a disabled knob is marked disabled and ignores pointer input.
	/// </summary>
	[Fact]
	public void Disabled_IgnoresPointerDown()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var knob = Render<PDKnob>(parameters => parameters.Add(p => p.IsEnabled, false));

		knob.Find(".pd-knob").ClassList.Should().Contain("disabled");
		knob.Find("svg").PointerDown(new PointerEventArgs { ClientY = 100 });

		module.VerifyNotInvoke("registerAudioControlEvents");
	}

	/// <summary>
	/// Verifies that dragging registers the pointer listeners, and that moving the pointer up raises the value
	/// in proportion until the pointer is released.
	/// </summary>
	[Fact]
	public async Task Drag_RaisesValueChanged()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var values = new List<double>();
		var knob = Render<PDKnob>(parameters => parameters
			.Add(p => p.Mode, PDKnobMode.Balance)
			.Add(p => p.Value, 0.2)
			.Add(p => p.ValueChanged, (double v) => values.Add(v)));

		knob.Find("svg").PointerDown(new PointerEventArgs { ClientY = 100 });
		module.VerifyInvoke("registerAudioControlEvents");

		await knob.InvokeAsync(() => knob.Instance.OnPointerMove(70));
		knob.Instance.OnPointerUp(70);
		await knob.InvokeAsync(() => knob.Instance.OnPointerMove(0));

		values.Should().ContainSingle().Which.Should().BeApproximately(0.4, 1e-9);
	}

	/// <summary>
	/// Verifies that a volume knob snaps a drag to its whole-number steps.
	/// </summary>
	[Fact]
	public async Task Drag_OnVolumeKnob_SnapsToSteps()
	{
		JSInterop.SetupModule(ModulePath);
		var values = new List<double>();
		var knob = Render<PDKnob>(parameters => parameters
			.Add(p => p.MaxDisplay, 10)
			.Add(p => p.Value, 0.0)
			.Add(p => p.ValueChanged, (double v) => values.Add(v)));

		knob.Find("svg").PointerDown(new PointerEventArgs { ClientY = 100 });
		await knob.InvokeAsync(() => knob.Instance.OnPointerMove(80));

		values.Should().ContainSingle().Which.Should().BeApproximately(0.1, 1e-9);
	}

	/// <summary>
	/// Verifies that a double-click resets the knob to its default value.
	/// </summary>
	[Fact]
	public void DoubleClick_ResetsToDefault()
	{
		var values = new List<double>();
		var knob = Render<PDKnob>(parameters => parameters
			.Add(p => p.Mode, PDKnobMode.Gain)
			.Add(p => p.Value, 0.9)
			.Add(p => p.DefaultValue, 0.25)
			.Add(p => p.ValueChanged, (double v) => values.Add(v)));

		knob.Find("svg").DoubleClick();

		knob.WaitForAssertion(() => values.Should().Equal(0.25), TimeSpan.FromSeconds(30));
	}

	/// <summary>
	/// Verifies that the pointer-to-angle conversion measures from straight up and clamps to the dial's range.
	/// </summary>
	/// <param name="offsetX">Pointer X offset within the dial.</param>
	/// <param name="offsetY">Pointer Y offset within the dial.</param>
	/// <param name="expected">The expected angle in degrees.</param>
	[Theory]
	[InlineData(30, 0, 0)]
	[InlineData(60, 30, 90)]
	[InlineData(0, 30, -90)]
	[InlineData(31, 60, 160)]
	[InlineData(29, 60, -160)]
	public void GetAngleFromPointer_MeasuresFromTopAndClamps(double offsetX, double offsetY, double expected)
	{
		var knob = Render<AngleProbeKnob>();

		knob.Instance.AngleAt(new PointerEventArgs { OffsetX = offsetX, OffsetY = offsetY })
			.Should().BeApproximately(expected, 1e-9);
	}

	/// <summary>Exposes the protected pointer-angle calculation of <see cref="PDKnob"/>.</summary>
	private sealed class AngleProbeKnob : PDKnob
	{
		public double AngleAt(PointerEventArgs e) => GetAngleFromPointer(e);
	}
}
