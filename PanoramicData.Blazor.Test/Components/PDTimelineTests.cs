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
public partial class PDTimelineTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDTimeline.razor.js";
	private static readonly DateTime _min = new(2026, 1, 1);
	private static readonly DateTime _max = new(2026, 3, 1);

	/// <summary>
	/// How long a wait may take. Waits cover asynchronous continuations only, such as the timer tick and the
	/// first-render interop, never real time. They are generous so a loaded thread pool cannot fail them, and they
	/// return as soon as they pass.
	/// </summary>
	private static readonly TimeSpan _wait = TimeSpan.FromSeconds(30);

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

	/// <summary>Sets the scale the next <see cref="RenderTimeline"/> renders with.</summary>
	private void UseScale(TimelineScale scale) => _renderScale = scale;

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
		timeline.WaitForState(() => _initialized, _wait);
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

		timeline.WaitForState(() => _initialized, _wait);

		// initialize(id, options, ref): the object reference is the third argument, and there is no fourth
		var initialize = _module.VerifyInvoke("initialize");
		initialize.Arguments.Should().HaveCount(3);
		initialize.Arguments[0].Should().Be("tl-1");
		initialize.Arguments[2].Should().BeOfType<Microsoft.JSInterop.DotNetObjectReference<PDTimeline>>();
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
