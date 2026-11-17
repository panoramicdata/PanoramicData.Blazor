using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Zooming with a selection: the selected range stays centred.
/// </summary>
public partial class PDTimelineTests
{
	private static DateTime QueryCentre((DateTime Start, DateTime End, string Scale) query)
		=> query.Start + ((query.End - query.Start) / 2);

	/// <summary>
	/// Verifies that zooming in with a range selected centres the viewport on the selection rather than on
	/// wherever the viewport happened to be.
	/// </summary>
	[Fact]
	public async Task ZoomIn_WithSelection_CentresOnTheSelection()
	{
		var timeline = RenderTimeline();
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(10), Day(14)));
		_queries.Clear();

		await timeline.InvokeAsync(timeline.Instance.ZoomInAsync);

		var query = _queries[^1];
		query.Scale.Should().Be("12 Hours");
		QueryCentre(query).Should().BeCloseTo(Day(12), TimeSpan.FromHours(12));
	}

	/// <summary>
	/// Verifies that zooming out with a range selected also keeps the selection centred.
	/// </summary>
	[Fact]
	public async Task ZoomOut_WithSelection_CentresOnTheSelection()
	{
		_renderMin = new DateTime(2025, 1, 1);
		_renderMax = new DateTime(2027, 1, 1);
		var timeline = RenderTimeline();
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(20), Day(24)));
		_queries.Clear();

		await timeline.InvokeAsync(timeline.Instance.ZoomOutAsync);

		var query = _queries[^1];
		query.Scale.Should().Be("Weeks");
		QueryCentre(query).Should().BeCloseTo(Day(22), TimeSpan.FromDays(7));
	}

	/// <summary>
	/// Verifies that Ctrl with the wheel centres on the selection in the same way as the zoom buttons.
	/// </summary>
	[Fact]
	public async Task CtrlWheelZoomIn_WithSelection_CentresOnTheSelection()
	{
		var timeline = RenderTimeline();
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(10), Day(14)));
		_queries.Clear();

		timeline.Find("div.pd-timeline").Wheel(new WheelEventArgs { DeltaY = -1, CtrlKey = true });

		var query = _queries[^1];
		query.Scale.Should().Be("12 Hours");
		QueryCentre(query).Should().BeCloseTo(Day(12), TimeSpan.FromHours(12));
	}
}
