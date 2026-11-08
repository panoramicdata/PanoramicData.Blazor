using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;
using System.Globalization;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for the <see cref="PDKnobMode.Angle"/> mode of <see cref="PDKnob"/>.
/// </summary>
public partial class PDKnobTests
{
	private const double PointerDistance = 20;

	/// <summary>Gets a pointer offset from the dial's centre in a screen direction (degrees clockwise from right).</summary>
	private static double[] PointerAt(double direction)
	{
		var radians = direction * Math.PI / 180;
		return [PointerDistance * Math.Cos(radians), PointerDistance * Math.Sin(radians)];
	}

	private IRenderedComponent<PDKnob> RenderAngleKnob(double angle, List<double> values)
		=> RenderAngleKnob(angle, values, _ => { });

	private IRenderedComponent<PDKnob> RenderAngleKnob(double angle, List<double> values, Action<ComponentParameterCollectionBuilder<PDKnob>> configure)
		=> Render<PDKnob>(parameters =>
		{
			parameters
				.Add(p => p.Mode, PDKnobMode.Angle)
				.Add(p => p.Angle, angle)
				.Add(p => p.AngleChanged, (double v) => values.Add(v));
			configure(parameters);
		});

	/// <summary>Presses the pointer at a screen direction, with the JS module reporting that offset.</summary>
	private async Task PressAtAsync(IRenderedComponent<PDKnob> knob, double direction)
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.Setup<double[]?>("registerAngleEvents", _ => true).SetResult(PointerAt(direction));
		await knob.Find("svg").PointerDownAsync(new PointerEventArgs { ClientX = 1, ClientY = 1 });
	}

	private static Task MoveToAsync(IRenderedComponent<PDKnob> knob, double direction)
		=> MoveToAsync(knob, direction, false);

	private static async Task MoveToAsync(IRenderedComponent<PDKnob> knob, double direction, bool free)
	{
		var point = PointerAt(direction);
		await knob.InvokeAsync(() => knob.Instance.OnAnglePointerMove(point[0], point[1], free));
	}

	private static (double X, double Y) IndicatorEnd(IRenderedComponent<PDKnob> knob)
	{
		var line = knob.Find("line[stroke='#333']");
		return (double.Parse(line.GetAttribute("x2")!, CultureInfo.InvariantCulture),
			double.Parse(line.GetAttribute("y2")!, CultureInfo.InvariantCulture));
	}

	/// <summary>Turning clockwise past 360 continues smoothly, reported wrapped or as a running total.</summary>
	[Theory]
	[InlineData(true, new double[] { 355, 5, 20 })]
	[InlineData(false, new double[] { 355, 365, 380 })]
	public async Task Angle_TurningClockwisePast360_DoesNotJump(bool wrap, double[] expected)
	{
		var values = new List<double>();
		var knob = RenderAngleKnob(340, values, p => p.Add(x => x.AngleWrap, wrap));

		await PressAtAsync(knob, 340);
		await MoveToAsync(knob, 355);
		await MoveToAsync(knob, 5);
		await MoveToAsync(knob, 20);

		values.Should().HaveCount(3);
		values.Zip(expected).Should().AllSatisfy(pair => pair.First.Should().BeApproximately(pair.Second, 1e-6));
	}

	/// <summary>Turning anticlockwise below 0 continues smoothly, reported wrapped or as a running total.</summary>
	[Theory]
	[InlineData(true, 350)]
	[InlineData(false, -10)]
	public async Task Angle_TurningBelowZero_WrapsOrRunsNegative(bool wrap, double expected)
	{
		var values = new List<double>();
		var knob = RenderAngleKnob(10, values, p => p.Add(x => x.AngleWrap, wrap));

		await PressAtAsync(knob, 10);
		await MoveToAsync(knob, 0);
		await MoveToAsync(knob, -10);

		values[^1].Should().BeApproximately(expected, 1e-6);
	}

	/// <summary>With the anticlockwise sense, turning the pointer clockwise lowers the angle.</summary>
	[Fact]
	public async Task Angle_Anticlockwise_ReversesTheSense()
	{
		var values = new List<double>();
		var knob = RenderAngleKnob(0, values, p => p.Add(x => x.AngleClockwise, false));

		await PressAtAsync(knob, 0);
		await MoveToAsync(knob, 30);

		values.Should().ContainSingle().Which.Should().BeApproximately(330, 1e-6);
	}

	/// <summary>A drag snaps to the step, and Shift moves freely.</summary>
	[Theory]
	[InlineData(false, 15)]
	[InlineData(true, 8)]
	public async Task Angle_Drag_SnapsToTheStep_UnlessFree(bool free, double expected)
	{
		var values = new List<double>();
		var knob = RenderAngleKnob(0, values, p => p.Add(x => x.AngleStep, 15));

		await PressAtAsync(knob, 0);
		await MoveToAsync(knob, 8, free);

		values.Should().ContainSingle().Which.Should().BeApproximately(expected, 1e-6);
	}

	/// <summary>Releasing the pointer ends the drag.</summary>
	[Fact]
	public async Task Angle_AfterPointerUp_MovesNothing()
	{
		var values = new List<double>();
		var knob = RenderAngleKnob(0, values);

		await PressAtAsync(knob, 0);
		knob.Instance.OnAnglePointerUp();
		await MoveToAsync(knob, 45);

		values.Should().BeEmpty();
	}

	/// <summary>Arrow keys move by the step (1 degree without one), Page Up and Page Down by the large step, wrapping at 0.</summary>
	[Theory]
	[InlineData(0, 0, "ArrowRight", 1)]
	[InlineData(0, 0, "ArrowLeft", 359)]
	[InlineData(0, 15, "ArrowUp", 15)]
	[InlineData(0, 15, "ArrowDown", 345)]
	[InlineData(350, 0, "PageUp", 5)]
	[InlineData(10, 0, "PageDown", 355)]
	public async Task Angle_Keyboard_ChangesTheValue(double start, double step, string key, double expected)
	{
		var values = new List<double>();
		var knob = RenderAngleKnob(start, values, p => p.Add(x => x.AngleStep, step));

		await knob.Find("svg").KeyDownAsync(new KeyboardEventArgs { Key = key });

		values.Should().ContainSingle().Which.Should().BeApproximately(expected, 1e-6);
	}

	/// <summary>Other keys, and a disabled knob, change nothing.</summary>
	[Fact]
	public async Task Angle_Keyboard_IgnoresOtherKeysAndDisabled()
	{
		var values = new List<double>();
		var knob = RenderAngleKnob(0, values);
		await knob.Find("svg").KeyDownAsync(new KeyboardEventArgs { Key = "a" });

		var disabled = RenderAngleKnob(0, values, p => p.Add(x => x.IsEnabled, false));
		await disabled.Find("svg").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

		values.Should().BeEmpty();
		disabled.Find("svg").GetAttribute("tabindex").Should().Be("-1");
	}

	/// <summary>Keyboard keys do nothing in the other modes.</summary>
	[Fact]
	public async Task Volume_Keyboard_DoesNothing()
	{
		var values = new List<double>();
		var knob = Render<PDKnob>(parameters => parameters.Add(p => p.ValueChanged, (double v) => values.Add(v)));

		await knob.Find("svg").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });

		values.Should().BeEmpty();
	}

	/// <summary>The angle dial is a focusable slider with an accessible name and current value.</summary>
	[Fact]
	public void Angle_HasSliderAccessibility()
	{
		var knob = RenderAngleKnob(42.5, [], p => p.Add(x => x.AriaLabel, "Gradient angle"));

		var svg = knob.Find("svg");
		svg.GetAttribute("role").Should().Be("slider");
		svg.GetAttribute("tabindex").Should().Be("0");
		svg.GetAttribute("aria-label").Should().Be("Gradient angle");
		svg.GetAttribute("aria-valuenow").Should().Be("42.5");
		svg.GetAttribute("aria-valuemin").Should().Be("0");
		svg.GetAttribute("aria-valuemax").Should().Be("360");
	}

	/// <summary>The accessible name falls back to the label, and an unbounded dial has no minimum or maximum.</summary>
	[Fact]
	public void Angle_Unbounded_UsesTheLabelAndHasNoRange()
	{
		var knob = RenderAngleKnob(720, [], p => p.Add(x => x.Label, "Turns").Add(x => x.AngleWrap, false));

		var svg = knob.Find("svg");
		svg.GetAttribute("aria-label").Should().Be("Turns");
		svg.GetAttribute("aria-valuenow").Should().Be("720");
		svg.HasAttribute("aria-valuemin").Should().BeFalse();
	}

	/// <summary>The other modes carry none of the angle mode's attributes.</summary>
	[Fact]
	public void Volume_HasNoSliderAttributes()
	{
		var svg = Render<PDKnob>().Find("svg");

		svg.HasAttribute("role").Should().BeFalse();
		svg.HasAttribute("tabindex").Should().BeFalse();
		svg.HasAttribute("aria-valuenow").Should().BeFalse();
	}

	/// <summary>
	/// The indicator points the way the angle describes: 0 points right and the angle increases clockwise by
	/// default, and both are configurable.
	/// </summary>
	[Theory]
	[InlineData(0, 0, true, 45, 30)]
	[InlineData(90, 0, true, 30, 45)]
	[InlineData(45, 0, false, 40.60660171779821, 19.39339828220179)]
	[InlineData(0, 180, true, 15, 30)]
	[InlineData(450, 0, true, 30, 45)]
	public void Angle_IndicatorPointsInTheDirection(double angle, double zero, bool clockwise, double x, double y)
	{
		var knob = RenderAngleKnob(angle, [], p => p
			.Add(px => px.AngleZeroDirection, zero)
			.Add(px => px.AngleClockwise, clockwise));

		var (x2, y2) = IndicatorEnd(knob);
		x2.Should().BeApproximately(x, 1e-6);
		y2.Should().BeApproximately(y, 1e-6);
	}

	/// <summary>The angle dial draws no value arc and a tick every 45 degrees.</summary>
	[Fact]
	public void Angle_DrawsTicksAndNoArc()
	{
		var knob = RenderAngleKnob(0, []);

		knob.FindAll("path").Should().BeEmpty();
		knob.FindAll(TickSelector).Should().HaveCount(8);
		MarkLabels(knob).Should().BeEmpty();
	}

	/// <summary>A double-click resets the angle to its default.</summary>
	[Fact]
	public async Task Angle_DoubleClick_ResetsToTheDefault()
	{
		var values = new List<double>();
		var knob = RenderAngleKnob(200, values, p => p.Add(x => x.DefaultAngle, 90));

		await knob.Find("svg").DoubleClickAsync(new MouseEventArgs());

		values.Should().Equal(90);
	}

	/// <summary>Normalising an angle maps it into 0 up to 360.</summary>
	[Theory]
	[InlineData(0, 0)]
	[InlineData(360, 0)]
	[InlineData(370, 10)]
	[InlineData(-10, 350)]
	[InlineData(-720, 0)]
	[InlineData(359.99, 359.99)]
	public void NormaliseAngle_MapsInto0To360(double angle, double expected)
		=> PDKnob.NormaliseAngle(angle).Should().BeApproximately(expected, 1e-9);

	/// <summary>
	/// Two-way binding with a number box: typing a value moves the knob, and turning the knob updates the box.
	/// </summary>
	[Fact]
	public async Task Angle_TwoWayBinding_WithANumberBox()
	{
		var host = Render<AngleBindingHost>();

		await host.Find("input").ChangeAsync(new ChangeEventArgs { Value = "90" });
		var knob = host.FindComponent<PDKnob>();
		var (x2, y2) = IndicatorEnd(knob);
		x2.Should().BeApproximately(30, 1e-6);
		y2.Should().BeApproximately(45, 1e-6);

		await PressAtAsync(knob, 90);
		await MoveToAsync(knob, 135);

		host.Find("input").GetAttribute("value").Should().Be("135");
	}

	/// <summary>A number box and an angle knob bound to the same value.</summary>
	private sealed class AngleBindingHost : ComponentBase
	{
		private double _angle;

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "input");
			builder.AddAttribute(1, "type", "number");
			builder.AddAttribute(2, "value", BindConverter.FormatValue(_angle, CultureInfo.InvariantCulture));
			builder.AddAttribute(3, "onchange", EventCallback.Factory.CreateBinder(this, value => _angle = value, _angle, CultureInfo.InvariantCulture));
			builder.CloseElement();
			builder.OpenComponent<PDKnob>(4);
			builder.AddAttribute(5, nameof(PDKnob.Mode), PDKnobMode.Angle);
			builder.AddAttribute(6, nameof(PDKnob.Angle), _angle);
			builder.AddAttribute(7, nameof(PDKnob.AngleChanged), EventCallback.Factory.Create<double>(this, value => _angle = value));
			builder.CloseComponent();
		}
	}
}
