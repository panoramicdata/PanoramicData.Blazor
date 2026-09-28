using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDTimeline"/> lays out its viewport from the measured canvas, fetches the visible
/// range from its data provider, and turns pointer, wheel and method calls into selection, zoom and pan.
/// </summary>
/// <remarks>
/// The canvas is measured through the common JavaScript module, so every test sets that module up to report a
/// plot 400 pixels wide, 100 high and 10 from the left. With the default 20-pixel bars that is a 20-column
/// viewport, and column <c>c</c> is under client X <c>20c + 20</c>. Unless a test says otherwise the timeline
/// runs by day from 1 January to 1 March 2026, which is 60 columns.
/// </remarks>
public class PDTimelineTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDTimeline.razor.js";
	private static readonly DateTime _min = new(2026, 1, 1);
	private static readonly DateTime _max = new(2026, 3, 1);

	private readonly BunitJSModuleInterop _module;
	private readonly BunitJSModuleInterop _common;
	private readonly List<(DateTime Start, DateTime End, string Scale)> _queries = [];
	private readonly List<TimeRange?> _selections = [];
	private readonly List<string> _scaleChanges = [];
	private readonly List<DataPoint> _data = [];
	private int _selectionChangeEnds;
	private int _refreshes;
	private bool _initialized;

	/// <summary>Sets up the rendering context and the measured canvas.</summary>
	public PDTimelineTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		_common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		SetCanvas(400);
		_common.Setup<double>("getHeight", _ => true).SetResult(100);
		_common.Setup<double>("getX", _ => true).SetResult(10);
	}

	private void SetCanvas(double width) => _common.Setup<double>("getWidth", _ => true).SetResult(width);

	private static double ColumnX(int column) => (20 * column) + 20;

	private ValueTask<DataPoint[]> Provide(DateTime start, DateTime end, TimelineScale scale, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		_queries.Add((start, end, scale.Name));
		return ValueTask.FromResult(_data.Where(p => p.StartTime >= start && p.StartTime < end).ToArray());
	}

	private DateTime _renderMin = _min;
	private DateTime? _renderMax = _max;
	private TimelineScale _renderScale = TimelineScale.Days;

	private IRenderedComponent<PDTimeline> RenderTimeline(Action<ComponentParameterCollectionBuilder<PDTimeline>>? configure = null)
	{
		var timeline = Render<PDTimeline>(parameters =>
		{
			parameters
				.Add(p => p.Scale, _renderScale)
				.Add(p => p.MinDateTime, _renderMin)
				.Add(p => p.MaxDateTime, _renderMax)
				.Add(p => p.DataProvider, Provide)
				.Add(p => p.Initialized, () => _initialized = true)
				.Add(p => p.Refreshed, () => _refreshes++)
				.Add(p => p.SelectionChanged, (TimeRange? r) => _selections.Add(r))
				.Add(p => p.SelectionChangeEnd, () => _selectionChangeEnds++)
				.Add(p => p.ScaleChanged, (TimelineScale s) => _scaleChanges.Add(s.Name));
			configure?.Invoke(parameters);
		});
		timeline.WaitForState(() => _initialized);
		_scaleChanges.Clear();
		_queries.Clear();
		return timeline;
	}

	private static AngleSharp.Dom.IElement Plot(IRenderedComponent<PDTimeline> timeline) => timeline.Find("svg.tl-plot-area");

	private static void ClickColumn(IRenderedComponent<PDTimeline> timeline, int column, bool shift = false)
	{
		Plot(timeline).PointerDown(new PointerEventArgs { ClientX = ColumnX(column), PointerId = 1, ShiftKey = shift });
		Plot(timeline).PointerUp(new PointerEventArgs { ClientX = ColumnX(column), PointerId = 1 });
	}

	private static void DragColumns(IRenderedComponent<PDTimeline> timeline, int from, int to, bool shift = false)
	{
		Plot(timeline).PointerDown(new PointerEventArgs { ClientX = ColumnX(from), PointerId = 1, ShiftKey = shift });
		Plot(timeline).PointerMove(new PointerEventArgs { ClientX = ColumnX(to), PointerId = 1 });
		Plot(timeline).PointerUp(new PointerEventArgs { ClientX = ColumnX(to), PointerId = 1 });
	}

	private static TimeRange Range(DateTime start, DateTime end) => new() { StartTime = start, EndTime = end };

	private static DateTime Day(int dayOfJanuary) => new DateTime(2026, 1, 1).AddDays(dayOfJanuary - 1);

	#region Layout and data

	/// <summary>
	/// Verifies that after first render the timeline initialises its module, measures the canvas, fetches just
	/// the visible columns and draws one bar per column.
	/// </summary>
	[Fact]
	public void FirstRender_InitialisesAndFetchesViewport()
	{
		_data.Add(new DataPoint { StartTime = Day(2), Count = 4, SeriesValues = [3] });
		var timeline = Render<PDTimeline>(parameters => parameters
			.Add(p => p.Id, "tl-1")
			.Add(p => p.Scale, TimelineScale.Days)
			.Add(p => p.MinDateTime, _min)
			.Add(p => p.MaxDateTime, _max)
			.Add(p => p.DataProvider, Provide)
			.Add(p => p.Refreshed, () => _refreshes++)
			.Add(p => p.Initialized, () => _initialized = true));

		timeline.WaitForState(() => _initialized);

		_module.VerifyInvoke("initialize").Arguments[0].Should().Be("tl-1");
		_queries[^1].Should().Be((Day(1), Day(22), "Days"));
		_refreshes.Should().BePositive();
		var bars = timeline.FindComponents<PDStackedBar>();
		bars.Should().HaveCount(20);
		bars[1].Instance.DataPoint.Count.Should().Be(4);
		bars[1].Instance.X.Should().Be(20);
		bars[1].Instance.MaxValue.Should().Be(3);
		bars[0].Instance.DataPoint.StartTime.Should().Be(Day(1));
		timeline.Find("div.pd-timeline").Id.Should().Be("tl-1");
	}

	/// <summary>
	/// Verifies that a configured Y-axis maximum overrides the maximum found in the data, and that the Y-value
	/// transform is used when finding it.
	/// </summary>
	[Fact]
	public void MaxValue_ComesFromOptionsOrTransformedData()
	{
		_data.Add(new DataPoint { StartTime = Day(1), SeriesValues = [2, 3] });

		var transformed = RenderTimeline(p => p.Add(x => x.YValueTransform, v => v * 10));
		transformed.FindComponents<PDStackedBar>()[0].Instance.MaxValue.Should().Be(50);

		_initialized = false;
		var fixedMax = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { YAxis = new TimelineYAxisOptions { MaxValue = 99 } }));
		fixedMax.FindComponents<PDStackedBar>()[0].Instance.MaxValue.Should().Be(99);
	}

	/// <summary>
	/// Verifies that columns before <see cref="PDTimeline.DisableBefore"/> or ending after
	/// <see cref="PDTimeline.DisableAfter"/> are drawn disabled.
	/// </summary>
	[Fact]
	public void DisabledRanges_DisableTheirBars()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(3))
			.Add(x => x.DisableAfter, Day(18)));

		var enabled = timeline.FindComponents<PDStackedBar>().Select(b => b.Instance.IsEnabled).ToList();
		enabled.Take(2).Should().AllBeEquivalentTo(false);
		enabled.Skip(2).Take(15).Should().AllBeEquivalentTo(true);
		enabled.Skip(17).Should().AllBeEquivalentTo(false);
	}

	/// <summary>
	/// Verifies the X axis: a major tick labelled with the month on the first of the month, and minor labels with
	/// the day of the month for the other columns.
	/// </summary>
	[Fact]
	public void XAxis_LabelsMajorAndMinorTicks()
	{
		var timeline = RenderTimeline();

		var axis = timeline.Find("svg.tl-x-axis");
		axis.QuerySelectorAll("text.minor-tick").Select(t => t.TextContent.Trim()).Take(3).Should().Equal("01", "02", "03");
		axis.QuerySelectorAll("text:not(.minor-tick)").Select(t => t.TextContent.Trim()).Should().Equal("2026-01");
		axis.QuerySelectorAll("line[y2='18']").Should().ContainSingle();
		axis.QuerySelectorAll("rect title").Should().HaveCount(20);
	}

	/// <summary>
	/// Verifies that a timeline with no minimum date fetches nothing and draws no axis or pan handle.
	/// </summary>
	[Fact]
	public void NoMinimumDate_DrawsNothing()
	{
		_renderMin = DateTime.MinValue;
		var timeline = RenderTimeline();

		_queries.Should().BeEmpty();
		timeline.Find("svg.tl-x-axis").Children.Should().BeEmpty();
		timeline.Find("svg.tl-pan").Children.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the pan handle is sized to the share of the timeline in view.
	/// </summary>
	[Fact]
	public void PanHandle_IsSizedToViewportShare()
	{
		var timeline = RenderTimeline();

		var handle = timeline.FindAll("svg.tl-pan rect")[1];
		double.Parse(handle.GetAttribute("width")!, System.Globalization.CultureInfo.InvariantCulture)
			.Should().BeApproximately(20.0 / 61 * 400, 0.001);
		handle.GetAttribute("x").Should().Be("0");
	}

	/// <summary>
	/// Verifies that a disabled timeline is styled disabled and draws no pan handle, and the enable methods
	/// restore it.
	/// </summary>
	[Fact]
	public async Task Disabled_IsStyledAndEnableRestores()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.IsEnabled, false));

		timeline.Find("div.pd-timeline").ClassList.Should().Contain("disabled");
		timeline.Find("svg.tl-x-axis line").GetAttribute("stroke").Should().Be("DarkGray");
		timeline.Find("svg.tl-pan").Children.Should().BeEmpty();

		await timeline.InvokeAsync(timeline.Instance.Enable);
		timeline.Find("div.pd-timeline").ClassList.Should().NotContain("disabled");
		timeline.FindAll("svg.tl-pan rect").Should().HaveCount(2);

		await timeline.InvokeAsync(timeline.Instance.Disable);
		timeline.Instance.IsEnabled.Should().BeFalse();
		await timeline.InvokeAsync(() => timeline.Instance.SetEnabled(true));
		timeline.Instance.IsEnabled.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the new-data indicators are drawn when announced and raise their callbacks when pressed.
	/// </summary>
	[Fact]
	public void Indicators_RaiseUpdateCallbacks()
	{
		var minUpdates = 0;
		var maxUpdates = 0;
		var timeline = RenderTimeline(p => p
			.Add(x => x.NewMinDateTimeAvailable, true)
			.Add(x => x.NewMaxDateTimeAvailable, true)
			.Add(x => x.UpdateMinDate, () => minUpdates++)
			.Add(x => x.UpdateMaxDate, () => maxUpdates++));

		timeline.Find("svg.tl-ind-l").PointerDown();
		timeline.Find("svg.tl-ind-r").PointerDown();

		minUpdates.Should().Be(1);
		maxUpdates.Should().Be(1);
		timeline.Find("svg.tl-ind-r rect").GetAttribute("x").Should().Be("380");
	}

	/// <summary>
	/// Verifies that a spinner is shown while a fetch is outstanding, and removed when it completes.
	/// </summary>
	[Fact]
	public async Task Spinner_ShownWhileLoading()
	{
		var pending = new TaskCompletionSource<DataPoint[]>();
		var timeline = RenderTimeline();
		timeline.Render(p => p.Add(x => x.DataProvider, (_, _, _, ct) =>
		{
			ct.ThrowIfCancellationRequested();
			return new ValueTask<DataPoint[]>(pending.Task);
		}));

		var refresh = timeline.InvokeAsync(() => timeline.Instance.RefreshAsync(true));
		timeline.Render();
		timeline.FindAll("path.rotate").Should().ContainSingle();

		pending.SetResult([]);
		await refresh;
		timeline.Render();
		timeline.FindAll("path.rotate").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that fetching everything asks for the whole range, and that an unchanged range is not fetched
	/// again unless forced.
	/// </summary>
	[Fact]
	public async Task FetchAll_AndUnchangedRangeGuard()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { General = new TimelineGeneralOptions { FetchAll = true } }));

		await timeline.InvokeAsync(() => timeline.Instance.RefreshAsync(true));
		_queries.Should().Equal((Day(1), new DateTime(2026, 3, 2), "Days"));

		await timeline.InvokeAsync(() => timeline.Instance.RefreshAsync());
		_queries.Should().HaveCount(1);
	}

	/// <summary>
	/// Verifies that clearing the data keeps the selection when asked, and resetting clears both.
	/// </summary>
	[Fact]
	public async Task ClearAndReset_HandleSelection()
	{
		var timeline = RenderTimeline();
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(2), Day(4)));

		await timeline.InvokeAsync(() => timeline.Instance.Clear(false));
		timeline.Instance.GetSelection().Should().NotBeNull();

		await timeline.InvokeAsync(timeline.Instance.Reset);
		timeline.Instance.GetSelection().Should().BeNull();
		_selections[^1].Should().BeNull();

		await timeline.InvokeAsync(timeline.Instance.ClearSelection);
		_selections.Should().HaveCount(2);
	}

	/// <summary>
	/// Verifies that disposing the timeline releases its JavaScript counterpart.
	/// </summary>
	[Fact]
	public async Task Dispose_ReleasesJavaScript()
	{
		var timeline = RenderTimeline();
		var id = timeline.Instance.Id;

		await DisposeComponentsAsync();

		_module.VerifyInvoke("dispose").Arguments.Should().Equal(id);
	}

	/// <summary>
	/// Verifies that a non-positive follow-now refresh interval is rejected.
	/// </summary>
	[Fact]
	public void NonPositiveFollowNowInterval_IsRejected()
	{
		var act = () => Render<PDTimeline>(parameters => parameters
			.Add(p => p.FollowNowRefreshInterval, TimeSpan.Zero));

		act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("FollowNowRefreshInterval");
	}

	#endregion

	#region Pointer selection

	/// <summary>
	/// Verifies that a click selects its single column, draws the selection with both handles, and ends the
	/// change; a second click on the same column changes nothing.
	/// </summary>
	[Fact]
	public void Click_SelectsSingleColumn()
	{
		var timeline = RenderTimeline();

		ClickColumn(timeline, 3);

		_selections.Should().ContainSingle().Which.Should().BeEquivalentTo(Range(Day(4), Day(5)));
		_selectionChangeEnds.Should().Be(1);
		timeline.Instance.GetSelection().Should().BeEquivalentTo(Range(Day(4), Day(5)));
		var selection = timeline.Find("svg.tl-plot-area > rect");
		selection.GetAttribute("x").Should().Be("60");
		selection.GetAttribute("width").Should().Be("20");
		timeline.FindAll("rect.tl-selection-handle").Should().HaveCount(2);

		ClickColumn(timeline, 3);
		_selections.Should().ContainSingle();
		_selectionChangeEnds.Should().Be(1);
	}

	/// <summary>
	/// Verifies that a drag selects the columns it covers in either direction, and that a movement under the drag
	/// threshold is still a click.
	/// </summary>
	[Fact]
	public void Drag_SelectsColumnsInEitherDirection()
	{
		var timeline = RenderTimeline();

		DragColumns(timeline, 2, 6);
		_selections[^1].Should().BeEquivalentTo(Range(Day(3), Day(8)));

		DragColumns(timeline, 9, 5);
		_selections[^1].Should().BeEquivalentTo(Range(Day(6), Day(11)));

		Plot(timeline).PointerDown(new PointerEventArgs { ClientX = ColumnX(12), PointerId = 1 });
		Plot(timeline).PointerMove(new PointerEventArgs { ClientX = ColumnX(12) + 3, PointerId = 1 });
		Plot(timeline).PointerUp(new PointerEventArgs { ClientX = ColumnX(12) + 3, PointerId = 1 });
		_selections[^1].Should().BeEquivalentTo(Range(Day(13), Day(14)));
		_selectionChangeEnds.Should().Be(3);
	}

	/// <summary>
	/// Verifies that a shift-drag starting inside the selection moves the whole selection, keeping its width.
	/// </summary>
	[Fact]
	public void ShiftDrag_MovesSelection()
	{
		var timeline = RenderTimeline();
		DragColumns(timeline, 2, 4);

		DragColumns(timeline, 3, 5, shift: true);

		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(8)));
		timeline.Instance.IsPointInSelection(ColumnX(4)).Should().BeTrue();
		timeline.Instance.IsPointInSelection(ColumnX(7)).Should().BeFalse();
	}

	/// <summary>
	/// Verifies that a shift-drag cannot move the selection before the first column or after the last.
	/// </summary>
	[Fact]
	public void ShiftDrag_IsClampedToTheTimeline()
	{
		var timeline = RenderTimeline();
		DragColumns(timeline, 2, 4);

		DragColumns(timeline, 3, 0, shift: true);
		DragColumns(timeline, 1, -5, shift: true);
		_selections[^1].Should().BeEquivalentTo(Range(Day(1), Day(4)));

		DragColumns(timeline, 1, 70, shift: true);
		_selections[^1].Should().BeEquivalentTo(Range(Day(58), new DateTime(2026, 3, 2)));
	}

	/// <summary>
	/// Verifies that a shift-drag cannot move the selection into the disabled ranges.
	/// </summary>
	[Fact]
	public void ShiftDrag_StaysOutOfDisabledRanges()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(3))
			.Add(x => x.DisableAfter, Day(20)));
		DragColumns(timeline, 4, 6);

		DragColumns(timeline, 5, 1, shift: true);
		_selections[^1].Should().BeEquivalentTo(Range(Day(3), Day(6)));

		DragColumns(timeline, 3, 19, shift: true);
		_selections[^1].Should().BeEquivalentTo(Range(Day(17), Day(20)));
	}

	/// <summary>
	/// Verifies that a shift-click outside the selection starts a new selection instead of moving it.
	/// </summary>
	[Fact]
	public void ShiftClick_OutsideSelection_StartsNewSelection()
	{
		var timeline = RenderTimeline();
		DragColumns(timeline, 2, 4);

		ClickColumn(timeline, 10, shift: true);

		_selections[^1].Should().BeEquivalentTo(Range(Day(11), Day(12)));
	}

	/// <summary>
	/// Verifies that the end handle extends or shrinks the selection but never past its start, and the start
	/// handle does the same from the other side.
	/// </summary>
	[Fact]
	public void Handles_ResizeSelection()
	{
		var timeline = RenderTimeline();
		DragColumns(timeline, 2, 4);
		var ends = _selectionChangeEnds;

		var endHandle = timeline.FindAll("rect.tl-selection-handle")[1];
		endHandle.PointerDown(new PointerEventArgs { ClientX = ColumnX(4), PointerId = 2 });
		timeline.FindAll("rect.tl-selection-handle")[1].PointerMove(new PointerEventArgs { ClientX = ColumnX(1) });
		timeline.FindAll("rect.tl-selection-handle")[1].PointerMove(new PointerEventArgs { ClientX = ColumnX(8) });
		timeline.FindAll("rect.tl-selection-handle")[1].PointerUp(new PointerEventArgs { ClientX = ColumnX(8) });
		_selections[^1].Should().BeEquivalentTo(Range(Day(3), Day(10)));

		var startHandle = timeline.FindAll("rect.tl-selection-handle")[0];
		startHandle.PointerDown(new PointerEventArgs { ClientX = ColumnX(2), PointerId = 2 });
		timeline.FindAll("rect.tl-selection-handle")[0].PointerMove(new PointerEventArgs { ClientX = ColumnX(9) });
		timeline.FindAll("rect.tl-selection-handle")[0].PointerMove(new PointerEventArgs { ClientX = ColumnX(0) });
		timeline.FindAll("rect.tl-selection-handle")[0].PointerUp(new PointerEventArgs { ClientX = ColumnX(0) });
		_selections[^1].Should().BeEquivalentTo(Range(Day(1), Day(10)));

		_selectionChangeEnds.Should().Be(ends + 2);
	}

	/// <summary>
	/// Verifies that a disabled selection option, or a disabled timeline, ignores clicks on the chart.
	/// </summary>
	[Fact]
	public void SelectionDisabled_IgnoresClicks()
	{
		var off = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { Selection = new TimelineSelectionOptions { Enabled = false } }));
		ClickColumn(off, 3);

		_initialized = false;
		var disabled = RenderTimeline(p => p.Add(x => x.IsEnabled, false));
		ClickColumn(disabled, 3);

		_selections.Should().BeEmpty();
		_selectionChangeEnds.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a click on a disabled column, before <see cref="PDTimeline.DisableBefore"/> or from
	/// <see cref="PDTimeline.DisableAfter"/>, selects nothing.
	/// </summary>
	[Fact]
	public void ClickOnDisabledColumn_SelectsNothing()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(3))
			.Add(x => x.DisableAfter, Day(10)));

		ClickColumn(timeline, 1);
		ClickColumn(timeline, 9);

		_selections.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that when only the start can change a click selects from there to the end of the timeline, with
	/// only the start handle drawn.
	/// </summary>
	[Fact]
	public void StartOnlySelection_RunsToTheEnd()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { Selection = new TimelineSelectionOptions { CanChangeEnd = false } }));

		ClickColumn(timeline, 3);

		_selections[^1].Should().BeEquivalentTo(Range(Day(4), new DateTime(2026, 3, 2)));
		timeline.FindAll("rect.tl-selection-handle").Should().ContainSingle()
			.Which.GetAttribute("x").Should().Be("60");
	}

	/// <summary>
	/// Verifies that when only the end can change a click selects from the start of the timeline, with only the
	/// end handle drawn.
	/// </summary>
	[Fact]
	public void EndOnlySelection_RunsFromTheStart()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { Selection = new TimelineSelectionOptions { CanChangeStart = false } }));

		ClickColumn(timeline, 3);

		_selections[^1].Should().BeEquivalentTo(Range(Day(1), Day(5)));
		timeline.FindAll("rect.tl-selection-handle").Should().ContainSingle()
			.Which.GetAttribute("x").Should().Be("76");
	}

	/// <summary>
	/// Verifies that a right-aligned timeline shorter than the viewport is padded on the left, and a start-only
	/// selection then runs to the right-hand edge.
	/// </summary>
	[Fact]
	public void RightAligned_ShortTimeline_PadsLeftAndSelectsToEdge()
	{
		_renderMax = Day(10);
		var options = new TimelineOptions
		{
			General = new TimelineGeneralOptions { RightAlign = true },
			Selection = new TimelineSelectionOptions { CanChangeEnd = false }
		};
		var timeline = RenderTimeline(p => p.Add(x => x.Options, options));

		timeline.Instance.RoundedMinDateTime.Should().Be(new DateTime(2025, 12, 22));
		ClickColumn(timeline, 15);

		_selections[^1].Should().BeEquivalentTo(Range(Day(6), Day(11)));
	}

	/// <summary>
	/// Verifies that when only the start can change on a short timeline that is not right-aligned, the selection
	/// runs to the last real column.
	/// </summary>
	[Fact]
	public void StartOnlySelection_OnShortTimeline_RunsToLastColumn()
	{
		_renderMax = Day(10);
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { Selection = new TimelineSelectionOptions { CanChangeEnd = false } }));

		ClickColumn(timeline, 5);

		_selections[^1].Should().BeEquivalentTo(Range(Day(6), Day(11)));
	}

	/// <summary>
	/// Verifies that a drag into a disabled range is cut short at its boundary, from either direction and from
	/// the start handle.
	/// </summary>
	[Fact]
	public void Drag_IsClippedToEnabledRange()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(5))
			.Add(x => x.DisableAfter, Day(12)));

		DragColumns(timeline, 6, 15);
		_selections[^1].Should().BeEquivalentTo(Range(Day(7), Day(12)));

		DragColumns(timeline, 8, 1);
		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(10)));

		DragColumns(timeline, 6, 8);
		timeline.FindAll("rect.tl-selection-handle")[0].PointerDown(new PointerEventArgs { ClientX = ColumnX(6) });
		timeline.FindAll("rect.tl-selection-handle")[0].PointerMove(new PointerEventArgs { ClientX = ColumnX(2) });
		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(10)));
	}

	/// <summary>
	/// Verifies that when selecting disabled ranges is allowed a drag is not clipped.
	/// </summary>
	[Fact]
	public void Drag_WithDisabledSelectionAllowed_IsNotClipped()
	{
		var options = new TimelineOptions { General = new TimelineGeneralOptions { AllowDisableSelection = true } };
		var timeline = RenderTimeline(p => p
			.Add(x => x.Options, options)
			.Add(x => x.DisableAfter, Day(12)));

		DragColumns(timeline, 6, 15);

		_selections[^1].Should().BeEquivalentTo(Range(Day(7), Day(17)));
	}

	/// <summary>
	/// Verifies that a single-column selection that straddles <see cref="PDTimeline.DisableAfter"/> is cut at it.
	/// </summary>
	[Fact]
	public void Click_OnPartlyDisabledColumn_IsClipped()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.DisableAfter, Day(5).AddHours(12)));

		ClickColumn(timeline, 4);

		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(5).AddHours(12)));
	}

	#endregion

	#region Programmatic selection

	/// <summary>
	/// Verifies that setting a selection raises the change once, draws it, and clamps it to the timeline's end.
	/// </summary>
	[Fact]
	public async Task SetSelection_RaisesOnceAndClampsToEnd()
	{
		var timeline = RenderTimeline();

		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(3), Day(6)));
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(3), Day(6)));
		_selections.Should().ContainSingle().Which.Should().BeEquivalentTo(Range(Day(3), Day(6)));
		timeline.Render();
		timeline.Find("svg.tl-plot-area > rect").GetAttribute("x").Should().Be("40");
		timeline.Find("svg.tl-plot-area > rect").GetAttribute("width").Should().Be("60");

		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(3), new DateTime(2026, 4, 1)));
		_selections[^1].Should().BeEquivalentTo(Range(Day(3), new DateTime(2026, 3, 2)));
	}

	/// <summary>
	/// Verifies that a set selection is clipped to the enabled range unless selecting disabled ranges is allowed.
	/// </summary>
	[Fact]
	public async Task SetSelection_IsClippedToEnabledRange()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.DisableBefore, Day(5))
			.Add(x => x.DisableAfter, Day(20)));

		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(2), Day(30)));
		_selections[^1].Should().BeEquivalentTo(Range(Day(5), Day(20)));

		_initialized = false;
		var options = new TimelineOptions { General = new TimelineGeneralOptions { AllowDisableSelection = true } };
		var unclipped = RenderTimeline(p => p
			.Add(x => x.Options, options)
			.Add(x => x.DisableAfter, Day(20)));
		await unclipped.InvokeAsync(() => unclipped.Instance.SetSelection(Day(2), Day(30)));
		_selections[^1].Should().BeEquivalentTo(Range(Day(2), Day(30)));
	}

	/// <summary>
	/// Verifies that <see cref="PDTimeline.IsPointInSelection"/> is false with no selection.
	/// </summary>
	[Fact]
	public void IsPointInSelection_WithoutSelection_IsFalse()
	{
		var timeline = RenderTimeline();

		timeline.Instance.IsPointInSelection(ColumnX(3)).Should().BeFalse();
	}

	#endregion

	#region Zoom

	/// <summary>
	/// Verifies when zooming in and out is possible: not past either end of the scale list, not when disabled,
	/// and not for a scale that is not in the list.
	/// </summary>
	[Fact]
	public void CanZoom_ReflectsScalePositionAndState()
	{
		var timeline = RenderTimeline();
		timeline.Instance.CanZoomIn().Should().BeTrue();
		timeline.Instance.CanZoomOut().Should().BeTrue();

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Seconds));
		timeline.Instance.CanZoomIn().Should().BeFalse();

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Years));
		timeline.Instance.CanZoomOut().Should().BeFalse();

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Minutes5));
		timeline.Instance.CanZoomIn().Should().BeFalse();
		timeline.Instance.CanZoomOut().Should().BeFalse();

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Days).Add(x => x.IsEnabled, false));
		timeline.Instance.CanZoomIn().Should().BeFalse();
		timeline.Instance.CanZoomOut().Should().BeFalse();
	}

	/// <summary>
	/// Verifies that with zoom-out restricted, zooming out is only possible while the wider scale still fills
	/// the viewport, and a refused zoom leaves the scale alone.
	/// </summary>
	[Fact]
	public async Task RestrictZoomOut_RefusesZoomPastTheData()
	{
		var restricted = new TimelineOptions { General = new TimelineGeneralOptions { RestrictZoomOut = true } };
		var timeline = RenderTimeline(p => p.Add(x => x.Options, restricted));

		timeline.Instance.CanZoomOut().Should().BeFalse();
		await timeline.InvokeAsync(() => timeline.Instance.SetScale(TimelineScale.Weeks));
		timeline.Instance.Scale.Name.Should().Be("Days");
		_scaleChanges.Should().BeEmpty();

		_initialized = false;
		_renderMin = new DateTime(2020, 1, 1);
		var longTimeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { General = new TimelineGeneralOptions { RestrictZoomOut = true } }));
		longTimeline.Instance.CanZoomOut().Should().BeTrue();
	}

	/// <summary>
	/// Verifies that Ctrl with the mouse wheel zooms in and out one scale at a time, and the wheel alone does not.
	/// </summary>
	[Fact]
	public void CtrlWheel_Zooms()
	{
		var timeline = RenderTimeline();
		var root = timeline.Find("div.pd-timeline");

		root.Wheel(new WheelEventArgs { DeltaY = -1 });
		_scaleChanges.Should().BeEmpty();

		timeline.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = -1, CtrlKey = true });
		timeline.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = 1, CtrlKey = true });
		timeline.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = 1, CtrlKey = true });

		_scaleChanges.Should().Equal("12 Hours", "Days", "Weeks");
	}

	/// <summary>
	/// Verifies that the wheel cannot zoom past either end of the scale list.
	/// </summary>
	[Fact]
	public void CtrlWheel_StopsAtTheEnds()
	{
		_renderScale = TimelineScale.Seconds;
		_renderMin = new DateTime(2026, 1, 1, 12, 0, 0);
		_renderMax = new DateTime(2026, 1, 1, 12, 5, 0);
		var timeline = RenderTimeline();
		timeline.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = -1, CtrlKey = true });

		_initialized = false;
		_renderScale = TimelineScale.Years;
		_renderMin = new DateTime(2000, 1, 1);
		var years = RenderTimeline();
		years.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = 1, CtrlKey = true });

		_scaleChanges.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the zoom methods step through the scale list and stop at its ends.
	/// </summary>
	[Fact]
	public async Task ZoomInAndOut_StepThroughScales()
	{
		var timeline = RenderTimeline();

		await timeline.InvokeAsync(timeline.Instance.ZoomOutAsync);
		await timeline.InvokeAsync(timeline.Instance.ZoomInAsync);
		await timeline.InvokeAsync(timeline.Instance.ZoomInAsync);
		_scaleChanges.Should().Equal("Weeks", "Days", "12 Hours");

		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Years));
		_scaleChanges.Clear();
		await timeline.InvokeAsync(timeline.Instance.ZoomOutAsync);
		timeline.Render(p => p.Add(x => x.Scale, TimelineScale.Seconds));
		_scaleChanges.Clear();
		await timeline.InvokeAsync(timeline.Instance.ZoomInAsync);
		_scaleChanges.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the scale chosen to fit a range is the finest one that fits in the viewport, falling back
	/// to the first scale when none does.
	/// </summary>
	[Fact]
	public void GetScaleToFit_ChoosesFinestFittingScale()
	{
		var timeline = RenderTimeline();

		timeline.Instance.GetScaleToFit(Day(1), Day(2))!.Name.Should().Be("4 Hours");
		timeline.Instance.GetScaleToFit()!.Name.Should().Be("Weeks");
		timeline.Instance.GetScaleToFit(new DateTime(1900, 1, 1), Day(1))!.Name.Should().Be("Seconds");
	}

	/// <summary>
	/// Verifies that the zoom-to methods switch to the scale that fits their range.
	/// </summary>
	[Fact]
	public async Task ZoomTo_SwitchesToFittingScale()
	{
		var timeline = RenderTimeline();

		await timeline.InvokeAsync(() => timeline.Instance.ZoomToAsync(Day(1), Day(2)));
		await timeline.InvokeAsync(timeline.Instance.ZoomToEndAsync);
		await timeline.InvokeAsync(() => timeline.Instance.ZoomToSelectionAsync());
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(4), Day(5)));
		await timeline.InvokeAsync(() => timeline.Instance.ZoomToSelectionAsync(true));
		await timeline.InvokeAsync(timeline.Instance.ZoomToStartAsync);

		_scaleChanges.Should().Equal("4 Hours", "Weeks", "4 Hours", "Weeks");
	}

	/// <summary>
	/// Verifies that changing scale keeps the selection in place, snapped to whole periods of the new scale.
	/// </summary>
	[Fact]
	public async Task ScaleChange_SnapsSelectionToNewPeriods()
	{
		var timeline = RenderTimeline();
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(4), Day(6)));

		await timeline.InvokeAsync(() => timeline.Instance.SetScale(TimelineScale.Weeks));

		timeline.Instance.GetSelection().Should().BeEquivalentTo(Range(Day(4), Day(6)));
		var selection = timeline.Find("svg.tl-plot-area > rect");
		selection.GetAttribute("x").Should().Be("20");
		selection.GetAttribute("width").Should().Be("20");
	}

	#endregion

	#region Pan and resize

	/// <summary>
	/// Verifies that dragging the pan handle moves the viewport and fetches the newly visible range.
	/// </summary>
	[Fact]
	public void PanDrag_MovesViewportAndFetches()
	{
		var timeline = RenderTimeline();
		var pan = timeline.Find("svg.tl-pan");

		pan.PointerDown(new PointerEventArgs { ClientX = 50, PointerId = 3 });
		timeline.Find("svg.tl-pan").PointerMove(new PointerEventArgs { ClientX = 110, Buttons = 1 });
		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 110 });

		timeline.FindAll("svg.tl-pan rect")[1].GetAttribute("x").Should().Be("60");
		_queries[^1].Should().Be((Day(10), Day(31), "Days"));
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(10));
	}

	/// <summary>
	/// Verifies that the pan handle cannot be dragged past either end of the track.
	/// </summary>
	[Fact]
	public void PanDrag_IsClampedToTrack()
	{
		var timeline = RenderTimeline();

		timeline.Find("svg.tl-pan").PointerMove(new PointerEventArgs { ClientX = 50, Buttons = 1 });
		timeline.Find("svg.tl-pan").PointerMove(new PointerEventArgs { ClientX = 900, Buttons = 1 });
		var x = double.Parse(timeline.FindAll("svg.tl-pan rect")[1].GetAttribute("x")!, System.Globalization.CultureInfo.InvariantCulture);
		x.Should().BeApproximately(400 - (20.0 / 61 * 400), 0.001);

		timeline.Find("svg.tl-pan").PointerMove(new PointerEventArgs { ClientX = -900, Buttons = 1 });
		timeline.FindAll("svg.tl-pan rect")[1].GetAttribute("x").Should().Be("0");
	}

	/// <summary>
	/// Verifies that clicking the pan track beside the handle pages the viewport that way.
	/// </summary>
	[Fact]
	public void PanClick_PagesViewport()
	{
		var timeline = RenderTimeline();

		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 310 });
		_queries[^1].Start.Should().Be(Day(20));

		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 20 });
		_queries[^1].Start.Should().Be(Day(1));

		var count = _queries.Count;
		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 50 });
		_queries.Should().HaveCount(count);
	}

	/// <summary>
	/// Verifies that when all data is fetched up front, panning does not fetch again.
	/// </summary>
	[Fact]
	public void Pan_WithFetchAll_DoesNotFetch()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { General = new TimelineGeneralOptions { FetchAll = true } }));

		timeline.Find("svg.tl-pan").PointerUp(new PointerEventArgs { ClientX = 310 });

		_queries.Should().BeEmpty();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(20));
	}

	/// <summary>
	/// Verifies that panning to a date centres it or ends the viewport on it, and a date outside the timeline
	/// pans to the end.
	/// </summary>
	[Fact]
	public void PanTo_PositionsTheDate()
	{
		var timeline = RenderTimeline();

		timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(30)));
		timeline.Render();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(20));

		timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(30), TimelinePositions.End));
		timeline.Render();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(11));

		timeline.InvokeAsync(() => timeline.Instance.PanTo(new DateTime(2026, 6, 1), TimelinePositions.End));
		timeline.Render();
		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(41));
	}

	/// <summary>
	/// Verifies that a timeline shorter than the viewport always pans to its start.
	/// </summary>
	[Fact]
	public void PanTo_OnShortTimeline_StaysAtStart()
	{
		_renderMax = Day(10);
		var timeline = RenderTimeline();

		timeline.InvokeAsync(() => timeline.Instance.PanTo(Day(8), TimelinePositions.End));
		timeline.Render();

		timeline.FindComponents<PDStackedBar>()[0].Instance.DataPoint.StartTime.Should().Be(Day(1));
	}

	/// <summary>
	/// Verifies that a resize re-measures the canvas and re-lays-out the viewport, re-fetching by default.
	/// </summary>
	[Fact]
	public async Task Resize_RelaysOutAndRefetches()
	{
		var timeline = RenderTimeline(p => p.Add(x => x.Options, new TimelineOptions { General = new TimelineGeneralOptions { FetchAll = true } }));

		SetCanvas(200);
		await timeline.InvokeAsync(timeline.Instance.OnResize);

		timeline.FindComponents<PDStackedBar>().Should().HaveCount(10);
		_queries.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that with re-fetching on resize turned off an unchanged range is not fetched again.
	/// </summary>
	[Fact]
	public async Task Resize_WithoutRefetch_KeepsData()
	{
		var timeline = RenderTimeline(p => p
			.Add(x => x.RefetchDataOnResize, false)
			.Add(x => x.Options, new TimelineOptions { General = new TimelineGeneralOptions { FetchAll = true } }));

		SetCanvas(200);
		await timeline.InvokeAsync(timeline.Instance.OnResize);

		timeline.FindComponents<PDStackedBar>().Should().HaveCount(10);
		_queries.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that setting the dates moves the rounded bounds.
	/// </summary>
	[Fact]
	public void SetDates_MovesBounds()
	{
		var timeline = RenderTimeline();

		timeline.Instance.SetDates(Day(5), Day(9));

		timeline.Instance.MinDateTime.Should().Be(Day(5));
		timeline.Instance.RoundedMaxDateTime.Should().Be(Day(10));
	}

	#endregion

	#region Follow now

	private static readonly DateTime _now = new(2026, 6, 15, 12, 0, 0);

	private IRenderedComponent<PDTimeline> RenderFollowing(ManualClock clock, List<bool> followChanges)
	{
		_renderMin = _now.Date.AddDays(-30);
		_renderMax = null;
		return RenderTimeline(p => p
			.Add(x => x.FollowNow, true)
			.Add(x => x.Clock, clock)
			.Add(x => x.FollowNowChanged, (bool v) => followChanges.Add(v)));
	}

	/// <summary>
	/// Verifies that an open-ended timeline asked to follow now does so, while one with a fixed end cannot.
	/// </summary>
	[Fact]
	public void FollowNow_FollowsOnlyAnOpenEndedTimeline()
	{
		var following = RenderFollowing(new ManualClock(_now), []);
		following.Instance.IsFollowingNow.Should().BeTrue();
		following.Instance.RoundedMaxDateTime.Should().Be(_now.Date.AddDays(1));

		_initialized = false;
		_renderMax = _max;
		var fixedEnd = RenderTimeline(p => p.Add(x => x.FollowNow, true));
		fixedEnd.Instance.IsFollowingNow.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that each timer tick after the day rolls over fetches the newly current range.
	/// </summary>
	[Fact]
	public void FollowNow_TimerTick_AdvancesToNewBoundary()
	{
		var clock = new ManualClock(_now);
		var timeline = RenderFollowing(clock, []);

		clock.Advance(TimeSpan.FromDays(1));

		timeline.WaitForAssertion(() => _queries.Should().NotBeEmpty());
		timeline.Instance.RoundedMaxDateTime.Should().Be(_now.Date.AddDays(2));
		timeline.Instance.IsFollowingNow.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that a timer tick within the same period does nothing.
	/// </summary>
	[Fact]
	public async Task FollowNow_TickWithinPeriod_DoesNothing()
	{
		var clock = new ManualClock(_now);
		var timeline = RenderFollowing(clock, []);

		clock.Advance(TimeSpan.FromMinutes(1));
		await timeline.InvokeAsync(() => timeline.Instance.RefreshFollowNowAsync());

		_queries.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that user navigation suspends following and reports it, and resuming follows again and reports
	/// that.
	/// </summary>
	[Fact]
	public async Task FollowNow_SuspendAndResume_AreReported()
	{
		var changes = new List<bool>();
		var timeline = RenderFollowing(new ManualClock(_now), changes);

		await timeline.InvokeAsync(timeline.Instance.ZoomInAsync);
		timeline.Instance.IsFollowingNow.Should().BeFalse();
		timeline.Instance.FollowNow.Should().BeFalse();

		await timeline.InvokeAsync(timeline.Instance.ResumeFollowNowAsync);
		timeline.Instance.IsFollowingNow.Should().BeTrue();

		changes.Should().Equal(false, true);
	}

	/// <summary>
	/// Verifies that turning the parameter off stops following, and turning it back on resumes it.
	/// </summary>
	[Fact]
	public void FollowNow_ParameterTogglesFollowing()
	{
		var timeline = RenderFollowing(new ManualClock(_now), []);

		timeline.Render(p => p.Add(x => x.FollowNow, false));
		timeline.Instance.IsFollowingNow.Should().BeFalse();

		timeline.Render(p => p.Add(x => x.FollowNow, true));
		timeline.Instance.IsFollowingNow.Should().BeTrue();

		timeline.Render(p => p.Add(x => x.FollowNowRefreshInterval, TimeSpan.FromSeconds(5)));
		timeline.Instance.IsFollowingNow.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that a selection made while following rolls forward with the boundary, keeping its duration.
	/// </summary>
	[Fact]
	public async Task FollowNow_RollsSelectionForward()
	{
		var clock = new ManualClock(_now);
		var timeline = RenderFollowing(clock, []);
		var end = _now.Date.AddDays(1);
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(end.AddDays(-3), end));

		clock.Advance(TimeSpan.FromDays(1));

		timeline.WaitForAssertion(() => timeline.Instance.GetSelection()
			.Should().BeEquivalentTo(Range(end.AddDays(-2), end.AddDays(1))));
	}

	/// <summary>
	/// A clock whose time and timers only move when the test says so.
	/// </summary>
	private sealed class ManualClock(DateTime now) : TimeProvider
	{
		private readonly List<ManualTimer> _timers = [];
		private DateTimeOffset _now = new(now, TimeSpan.Zero);

		public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

		public override DateTimeOffset GetUtcNow() => _now;

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			var timer = new ManualTimer(callback, state);
			_timers.Add(timer);
			return timer;
		}

		public void Advance(TimeSpan by)
		{
			_now += by;
			foreach (var timer in _timers.ToList())
			{
				timer.Fire();
			}
		}
	}

	/// <summary>
	/// A timer that fires only when its clock is advanced.
	/// </summary>
	private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
	{
		private bool _disposed;

		public bool Change(TimeSpan dueTime, TimeSpan period) => !_disposed;

		public void Fire()
		{
			if (!_disposed)
			{
				callback(state);
			}
		}

		public void Dispose() => _disposed = true;

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}

	#endregion

	#region Utilities

	/// <summary>
	/// Verifies the arc path, including the large-arc flag for sweeps over 180 degrees.
	/// </summary>
	[Fact]
	public void DescribeArc_BuildsSvgArc()
	{
		PDTimeline.Utilities.DescribeArc(50, 50, 10, 0, 90).Should().Be("M 50.00 60.00 A 10 10 0 0 0 60.00 50.00");
		PDTimeline.Utilities.DescribeArc(50, 50, 10, 0, 270).Should().Be("M 50.00 40.00 A 10 10 0 1 0 60.00 50.00");
	}

	/// <summary>
	/// Verifies the polar to cartesian conversion measures from the positive X axis.
	/// </summary>
	[Fact]
	public void PolarToCartesian_MeasuresFromXAxis()
	{
		var (x, y) = PDTimeline.Utilities.PolarToCartesian(10, 20, 5, 90);

		x.Should().BeApproximately(10, 1e-9);
		y.Should().BeApproximately(25, 1e-9);
	}

	/// <summary>
	/// Verifies the left- and right-facing arrow paths.
	/// </summary>
	[Fact]
	public void ArrowPath_FacesEitherWay()
	{
		PDTimeline.Utilities.ArrowPath(0, 100, 20, 5, true).Should().Be("M 5 50l 10 -10l 0 20Z");
		PDTimeline.Utilities.ArrowPath(380, 100, 20, 5, false).Should().Be("M 395 50l -10 -10l 0 20Z");
	}

	/// <summary>
	/// Verifies the text-info defaults.
	/// </summary>
	[Fact]
	public void TextInfo_HasDefaults()
	{
		var info = new PDTimeline.TextInfo { Text = "a", Skip = 2 };

		info.OffsetX.Should().Be(3);
		info.OffsetY.Should().Be(14);
		info.Skip.Should().Be(2);
		info.Text.Should().Be("a");
	}

	#endregion
}
