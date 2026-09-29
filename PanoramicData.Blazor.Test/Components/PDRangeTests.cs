using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDRange"/>: the dual-handle range slider, its validation and its pointer dragging.
/// </summary>
/// <remarks>
/// A width of 112 pixels leaves a 100 pixel track (the handle width and a pixel either side are taken off),
/// so over the default 0 to 100 range one pixel of drag is exactly one unit of range.
/// </remarks>
public class PDRangeTests : BunitContext
{
	private readonly List<(double Start, double End)> _changes = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDRangeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<PDRange> RenderRange(NumericRange range, Action<ComponentParameterCollectionBuilder<PDRange>>? more = null)
		=> Render<PDRange>(parameters =>
		{
			parameters
				.Add(p => p.Range, range)
				.Add(p => p.Width, 112)
				.Add(p => p.RangeChanged, r => _changes.Add((r.Start, r.End)));
			more?.Invoke(parameters);
		});

	private static async Task DragAsync(IRenderedComponent<PDRange> range, string handle, double from, double to)
	{
		var element = range.Find($"rect.handle.{handle}");
		await element.PointerDownAsync(new PointerEventArgs { OffsetX = from, PointerId = 1 });
		await range.Find($"rect.handle.{handle}").PointerMoveAsync(new PointerEventArgs { OffsetX = to });
		await range.Find($"rect.handle.{handle}").PointerUpAsync(new PointerEventArgs());
	}

	/// <summary>
	/// Verifies that a valid range is drawn as an SVG with both tracks and both handles at the range's positions.
	/// </summary>
	[Fact]
	public void ValidRange_IsDrawnWithTracksAndHandles()
	{
		var range = RenderRange(new NumericRange(20, 80));

		var svg = range.Find("svg.plot-area");
		svg.GetAttribute("width").Should().Be("112");
		svg.GetAttribute("height").Should().Be("30");
		range.FindAll("rect.track").Should().HaveCount(2);
		range.Find("rect.handle.start").GetAttribute("x").Should().Be("21");
		range.Find("rect.handle.end").GetAttribute("x").Should().Be("81");
		range.Find("rect.handle.start").GetAttribute("title").Should().Be("20");
		range.Find("div.pd-range").ClassList.Should().NotContain("disabled");
	}

	/// <summary>
	/// Verifies that with a non-zero Min the handles are placed relative to Min, so Min is the left end of the
	/// track and Max the right end (#179).
	/// </summary>
	[Fact]
	public void NonZeroMin_HandlesArePlacedRelativeToMin()
	{
		var range = RenderRange(new NumericRange(50, 100), p => p
			.Add(x => x.Min, 50)
			.Add(x => x.Max, 100));

		range.Find("rect.handle.start").GetAttribute("x").Should().Be("1");
		range.Find("rect.handle.end").GetAttribute("x").Should().Be("101");

		var middle = RenderRange(new NumericRange(60, 90), p => p
			.Add(x => x.Min, 50)
			.Add(x => x.Max, 100));

		middle.Find("rect.handle.start").GetAttribute("x").Should().Be("21");
		middle.Find("rect.handle.end").GetAttribute("x").Should().Be("81");
	}

	/// <summary>
	/// Verifies that with a non-zero Min the major ticks start at the left end of the track (#179).
	/// </summary>
	[Fact]
	public void NonZeroMin_TicksArePlacedRelativeToMin()
	{
		var range = RenderRange(new NumericRange(50, 100), p => p
			.Add(x => x.Min, 50)
			.Add(x => x.Max, 100)
			.Add(x => x.TickMajor, 25));

		range.FindAll("line.tick.major").Select(t => t.GetAttribute("x1")).Should().Equal("6", "56", "106");
	}

	/// <summary>
	/// Verifies that inverting marks both tracks, and disabling marks the component.
	/// </summary>
	[Fact]
	public void InvertAndDisabled_AreReflectedInTheClasses()
	{
		var range = RenderRange(new NumericRange(20, 80), p => p
			.Add(x => x.Invert, true)
			.Add(x => x.IsEnabled, false));

		range.FindAll("rect.track.invert").Should().HaveCount(2);
		range.Find("div.pd-range").ClassList.Should().Contain("disabled");
	}

	/// <summary>
	/// Verifies that major ticks are drawn at every interval, without labels unless asked for.
	/// </summary>
	[Fact]
	public void TickMajor_DrawsATickAtEveryInterval()
	{
		var range = RenderRange(new NumericRange(20, 80), p => p.Add(x => x.TickMajor, 25));

		range.FindAll("line.tick.major").Should().HaveCount(5);
		range.FindAll("text.label").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that tick labels use the default number format, and the handles make room for them.
	/// </summary>
	[Fact]
	public void ShowLabels_LabelsEachTickAndShortensTheHandles()
	{
		var range = RenderRange(new NumericRange(20, 80), p => p
			.Add(x => x.TickMajor, 50)
			.Add(x => x.ShowLabels, true)
			.Add(x => x.Height, 30));

		range.FindAll("text.label").Select(t => t.TextContent.Trim()).Should().Equal("0", "50", "100");
		range.Find("rect.handle.start").GetAttribute("height").Should().StartWith("19.98");
	}

	/// <summary>
	/// Verifies that a label function formats the tick labels.
	/// </summary>
	[Fact]
	public void TickMajorLabelFn_FormatsTheLabels()
	{
		var range = RenderRange(new NumericRange(20, 80), p => p
			.Add(x => x.TickMajor, 50)
			.Add(x => x.ShowLabels, true)
			.Add(x => x.TickMajorLabelFn, v => $"{v}%"));

		range.FindAll("text.label").Select(t => t.TextContent.Trim()).Should().Equal("0%", "50%", "100%");
	}

	/// <summary>
	/// Verifies that settings the validator rejects are reported instead of drawing the slider.
	/// </summary>
	[Theory]
	[InlineData(2.0, 0, 100, "TrackHeight")]
	[InlineData(0.5, 50, 10, "Min")]
	public void InvalidSettings_AreReportedInsteadOfDrawn(double trackHeight, double min, double max, string field)
	{
		var range = RenderRange(new NumericRange(20, 80), p => p
			.Add(x => x.TrackHeight, trackHeight)
			.Add(x => x.Min, min)
			.Add(x => x.Max, max));

		range.FindAll("svg").Should().BeEmpty();
		range.FindAll(".pd-validation-summary td.field").Select(td => td.TextContent).Should().Contain(field);
	}

	/// <summary>
	/// Verifies that the range supplied is clamped and snapped to the limits before it is drawn.
	/// </summary>
	[Theory]
	[InlineData(12, 87, 10, 0, 10, 90)]
	[InlineData(-5, 50, 0, 0, 0, 50)]
	[InlineData(60, 40, 0, 0, 40, 40)]
	[InlineData(10, 150, 0, 0, 10, 100)]
	[InlineData(50, 55, 0, 20, 35, 55)]
	[InlineData(0, 5, 0, 20, 0, 20)]
	public void SuppliedRange_IsClampedAndSnapped(double start, double end, double step, double minGap, double expectedStart, double expectedEnd)
	{
		var supplied = new NumericRange(start, end);

		RenderRange(supplied, p => p
			.Add(x => x.Step, step)
			.Add(x => x.MinGap, minGap));

		supplied.Start.Should().Be(expectedStart);
		supplied.End.Should().Be(expectedEnd);
	}

	/// <summary>
	/// Verifies that dragging the start handle moves the start by the dragged distance and reports it.
	/// </summary>
	[Theory]
	[InlineData(10, 0, 0, 30)]
	[InlineData(-40, 0, 0, 0)]
	[InlineData(100, 0, 0, 80)]
	[InlineData(100, 0, 10, 70)]
	[InlineData(13, 5, 0, 35)]
	public async Task DraggingTheStartHandle_MovesTheStart(double pixels, double step, double minGap, double expectedStart)
	{
		var range = RenderRange(new NumericRange(20, 80), p => p
			.Add(x => x.Step, step)
			.Add(x => x.MinGap, minGap));

		await DragAsync(range, "start", 50, 50 + pixels);

		_changes.Should().Equal((expectedStart, 80d));
	}

	/// <summary>
	/// Verifies that dragging the end handle moves the end by the dragged distance and reports it.
	/// </summary>
	[Theory]
	[InlineData(10, 0, 0, 90)]
	[InlineData(-100, 0, 0, 20)]
	[InlineData(-100, 0, 10, 30)]
	[InlineData(40, 0, 0, 100)]
	[InlineData(-13, 5, 0, 65)]
	public async Task DraggingTheEndHandle_MovesTheEnd(double pixels, double step, double minGap, double expectedEnd)
	{
		var range = RenderRange(new NumericRange(20, 80), p => p
			.Add(x => x.Step, step)
			.Add(x => x.MinGap, minGap));

		await DragAsync(range, "end", 50, 50 + pixels);

		_changes.Should().Equal((20d, expectedEnd));
	}

	/// <summary>
	/// Verifies that moving a handle without first pressing on it, or after releasing it, changes nothing.
	/// </summary>
	[Fact]
	public async Task MovingWithoutAPress_ChangesNothing()
	{
		var range = RenderRange(new NumericRange(20, 80));

		await range.Find("rect.handle.start").PointerMoveAsync(new PointerEventArgs { OffsetX = 90 });
		await range.Find("rect.handle.end").PointerMoveAsync(new PointerEventArgs { OffsetX = 90 });
		await range.Find("rect.handle.end").PointerUpAsync(new PointerEventArgs());
		await DragAsync(range, "start", 50, 60);
		await range.Find("rect.handle.start").PointerMoveAsync(new PointerEventArgs { OffsetX = 90 });

		_changes.Should().Equal((30d, 80d));
	}

	/// <summary>
	/// Verifies that a disabled slider cannot be dragged.
	/// </summary>
	[Fact]
	public async Task DisabledSlider_CannotBeDragged()
	{
		var range = RenderRange(new NumericRange(20, 80), p => p.Add(x => x.IsEnabled, false));

		await DragAsync(range, "start", 50, 60);
		await DragAsync(range, "end", 50, 60);

		_changes.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that pressing a handle captures the pointer, so a drag continues outside the handle.
	/// </summary>
	[Fact]
	public async Task PressingAHandle_CapturesThePointer()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.SetupVoid("setPointerCapture", _ => true).SetVoidResult();
		var range = RenderRange(new NumericRange(20, 80));

		await DragAsync(range, "start", 50, 60);
		await DragAsync(range, "end", 50, 60);

		module.Invocations["setPointerCapture"].Select(i => i.Arguments[0]).Should().Equal(1L, 1L);
	}
}
