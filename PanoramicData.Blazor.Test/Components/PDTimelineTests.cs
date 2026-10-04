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
}
