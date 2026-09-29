using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFader"/> draws its scale and grip from its parameters, snaps its value to the
/// configured range, and reports value changes from dragging and double-clicking.
/// </summary>
public class PDFaderTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDFader.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDFaderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that the default 0 to 10 range draws one marking per whole value, labelled on both sides.
	/// </summary>
	[Fact]
	public void The_default_range_draws_a_labelled_mark_per_value_on_both_sides()
	{
		var component = Render<PDFader>();

		var labels = component.FindAll("text").Select(t => t.TextContent.Trim()).ToList();
		labels.Should().HaveCount(22);
		labels.Distinct().Should().Equal("0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10");
		component.FindAll("text[text-anchor=start]").Should().HaveCount(11);
		component.FindAll("text[text-anchor=end]").Should().HaveCount(11);
	}

	/// <summary>
	/// Verifies that the label position parameter restricts labels to one side.
	/// </summary>
	[Theory]
	[InlineData(PDFaderLabelPosition.Left, "start")]
	[InlineData(PDFaderLabelPosition.Right, "end")]
	public void The_label_position_restricts_labels_to_one_side(PDFaderLabelPosition position, string anchor)
	{
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.FaderLabelPosition, position));

		var labels = component.FindAll("text");
		labels.Should().HaveCount(11);
		labels.Should().OnlyContain(t => t.GetAttribute("text-anchor") == anchor);
	}

	/// <summary>
	/// Verifies that a wide range uses a coarser marking step, so the scale stays readable.
	/// </summary>
	[Fact]
	public void A_wide_range_uses_a_coarser_marking_step()
	{
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.MaxValue, 100)
			.Add(p => p.FaderLabelPosition, PDFaderLabelPosition.Left));

		component.FindAll("text").Select(t => t.TextContent.Trim())
			.Should().Equal("0", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100");
	}

	/// <summary>
	/// Verifies that the marks are spaced over the travel of the grip, with the maximum at the top.
	/// </summary>
	[Fact]
	public void Marks_run_from_the_bottom_to_the_top_of_the_grip_travel()
	{
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.Height, 150)
			.Add(p => p.FaderLabelPosition, PDFaderLabelPosition.Left));

		var labels = component.FindAll("text");
		labels[0].GetAttribute("y").Should().Be("140");
		labels[^1].GetAttribute("y").Should().Be("10");
	}

	/// <summary>
	/// Verifies that the grip is drawn in the fader colour at the position of the current value.
	/// </summary>
	[Fact]
	public void The_grip_is_drawn_at_the_value_in_the_fader_colour()
	{
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.Value, 0.5)
			.Add(p => p.Height, 150)
			.Add(p => p.FaderColor, "#123456"));

		var grip = component.Find("rect[fill='#123456']");
		grip.GetAttribute("y").Should().Be("65");
		grip.GetAttribute("height").Should().Be("20");
	}

	/// <summary>
	/// Verifies that the disabled class and the label are rendered from their parameters.
	/// </summary>
	[Fact]
	public void Renders_the_disabled_class_and_the_label()
	{
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.IsEnabled, false)
			.Add(p => p.Label, "Volume"));

		component.Find(".pd-fader").ClassList.Should().Contain("disabled");
		component.Find(".pd-fader > div").TextContent.Should().Be("Volume");
	}

	/// <summary>
	/// Verifies that an enabled fader with no label has neither the disabled class nor a label.
	/// </summary>
	[Fact]
	public void An_enabled_fader_without_a_label_has_neither()
	{
		var component = Render<PDFader>();

		component.Find(".pd-fader").ClassList.Should().NotContain("disabled");
		component.FindAll(".pd-fader > div").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that double-clicking resets to the default value, snapped to the whole-value steps.
	/// </summary>
	[Fact]
	public async Task Double_clicking_resets_to_the_snapped_default_value()
	{
		var values = new List<double>();
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.Value, 0.2)
			.Add(p => p.DefaultValue, 0.73)
			.Add(p => p.ValueChanged, v => values.Add(v)));

		await component.InvokeAsync(() => component.Find("svg").DoubleClickAsync(new MouseEventArgs()));

		values.Should().ContainSingle().Which.Should().BeApproximately(0.7, 0.0001);
	}

	/// <summary>
	/// Verifies that dragging loads the module, registers for pointer events and reports snapped values.
	/// </summary>
	[Fact]
	public async Task Dragging_registers_events_and_reports_snapped_values()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var values = new List<double>();
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.Value, 0.5)
			.Add(p => p.ValueChanged, v => values.Add(v)));

		await component.InvokeAsync(() => component.Find("svg").PointerDownAsync(new PointerEventArgs { ClientY = 100 }));

		module.VerifyInvoke("registerAudioControlEvents");

		// Moving up by 30px at 150px per full travel adds 0.2.
		await component.InvokeAsync(() => component.Instance.OnPointerMove(70));
		values.Should().ContainSingle().Which.Should().BeApproximately(0.7, 0.0001);

		await component.InvokeAsync(() => component.Instance.OnPointerUp(70));
		await component.InvokeAsync(() => component.Instance.OnPointerMove(0));
		values.Should().ContainSingle("a move after the pointer is released is not part of the drag");
	}

	/// <summary>
	/// Verifies that a drag beyond the end of the travel is clamped to the maximum value.
	/// </summary>
	[Fact]
	public async Task Dragging_past_the_end_clamps_to_the_maximum()
	{
		JSInterop.SetupModule(ModulePath);
		var values = new List<double>();
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.Value, 0.5)
			.Add(p => p.ValueChanged, v => values.Add(v)));

		await component.InvokeAsync(() => component.Find("svg").PointerDownAsync(new PointerEventArgs { ClientY = 500 }));
		await component.InvokeAsync(() => component.Instance.OnPointerMove(0));

		values.Should().ContainSingle().Which.Should().Be(1);
	}

	/// <summary>
	/// Verifies that a disabled fader does not start a drag.
	/// </summary>
	[Fact]
	public async Task A_disabled_fader_does_not_start_a_drag()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.IsEnabled, false));

		await component.InvokeAsync(() => component.Find("svg").PointerDownAsync(new PointerEventArgs { ClientY = 100 }));

		module.Invocations["registerAudioControlEvents"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that changing the number of snap points snaps the current value to the new steps.
	/// </summary>
	[Fact]
	public void Changing_the_snap_points_snaps_the_value()
	{
		var values = new List<double>();
		var component = Render<PDFader>(parameters => parameters
			.Add(p => p.Value, 0.3)
			.Add(p => p.ValueChanged, v => values.Add(v)));

		component.Render(parameters => parameters.Add(p => p.SnapPoints, 3));

		values.Should().ContainSingle().Which.Should().BeApproximately(0.5, 0.0001);
	}

	/// <summary>
	/// Verifies the grip geometry and colours that the fader exposes to derived controls.
	/// </summary>
	[Fact]
	public void The_grip_geometry_is_exposed_to_derived_controls()
	{
		var component = Render<GeometryFader>(parameters => parameters
			.Add(p => p.Width, 40)
			.Add(p => p.Height, 150)
			.Add(p => p.Value, 1));

		var fader = component.Instance;
		fader.Geometry.Should().Be((5d, 35d, 0d, 10d));
		fader.Colours.Should().Be(("#aaa", "#666"));
	}

	/// <summary>
	/// A fader that exposes its protected geometry.
	/// </summary>
	private sealed class GeometryFader : PDFader
	{
		/// <summary>Gets the grip left and right edges, top, and centre line.</summary>
		public (double X, double X2, double Y, double CentreY) Geometry => (GripX, GripX2, GripY, CenterLineY);

		/// <summary>Gets the grip highlight and shadow colours.</summary>
		public (string Highlight, string Shadow) Colours => (HighlightColor, ShadowColor);
	}
}
