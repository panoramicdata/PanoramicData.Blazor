using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using System.Globalization;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDTimelineToolbar"/> reflects and drives the <see cref="PDTimeline"/> it is bound to:
/// zoom buttons follow the timeline's zoom limits, the scale, range and selection are displayed in the
/// timeline's date format, and the opt-in Follow Now toggle starts and stops live following.
/// </summary>
public class PDTimelineToolbarTests : BunitContext
{
	private static readonly DateTime _min = new(2026, 1, 1);
	private static readonly DateTime _max = new(2026, 1, 31);

	/// <summary>A generous wait for renders that can queue behind the timeline's own follow-now timer.</summary>
	private static readonly TimeSpan _wait = TimeSpan.FromSeconds(30);

	/// <summary>Sets up the rendering context, giving the timeline a measurable plot area.</summary>
	public PDTimelineToolbarTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		var common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		common.Setup<double>("getWidth", _ => true).SetResult(800);
		common.Setup<double>("getHeight", _ => true).SetResult(200);
		common.Setup<double>("getX", _ => true).SetResult(0);
	}

	private IRenderedComponent<PDTimeline> RenderTimeline(DateTime? max)
		=> Render<PDTimeline>(p => p
			.Add(x => x.MinDateTime, _min)
			.Add(x => x.MaxDateTime, max)
			.Add(x => x.Scale, TimelineScale.Days));

	private IRenderedComponent<PDTimelineToolbar> RenderToolbar(PDTimeline? timeline, bool showFollowNow = false)
		=> Render<PDTimelineToolbar>(p => p
			.Add(x => x.Timeline, timeline)
			.Add(x => x.ShowFollowNow, showFollowNow));

	private static string Format(DateTime value) => value.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);

	/// <summary>Without a timeline the zoom buttons are disabled and no scale, selection or range is shown.</summary>
	[Fact]
	public void WithoutTimeline_ZoomDisabled_AndNothingDisplayed()
	{
		var cut = RenderToolbar(null);

		cut.Find("button[title='Zoom In']").HasAttribute("disabled").Should().BeTrue();
		cut.Find("button[title='Zoom Out']").HasAttribute("disabled").Should().BeTrue();
		cut.Find(".scale-label").TextContent.Should().BeEmpty();
		cut.FindAll(".selection-bar").Should().BeEmpty();
		cut.FindAll(".range-bar").Should().BeEmpty();
		cut.FindAll("button[title='Follow Now']").Should().BeEmpty();
	}

	/// <summary>With a timeline, the scale name and the range in the timeline's date format are shown, and both zoom directions are available.</summary>
	[Fact]
	public void WithTimeline_ShowsScaleAndRange_AndEnablesZoom()
	{
		var timeline = RenderTimeline(_max).Instance;

		var cut = RenderToolbar(timeline);

		cut.Find(".scale-label").TextContent.Should().Be("Days");
		cut.Find("button[title='Zoom In']").HasAttribute("disabled").Should().BeFalse();
		cut.Find("button[title='Zoom Out']").HasAttribute("disabled").Should().BeFalse();
		cut.Find(".range-bar .range-min-label").TextContent.Should().Be(Format(_min));
		cut.Find(".range-bar .range-max-label").TextContent.Should().Be(Format(timeline.RoundedMaxDateTime));
	}

	/// <summary>The range is displayed in the date format configured on the timeline's options.</summary>
	[Fact]
	public void Range_UsesTheTimelineDateFormat()
	{
		var options = new TimelineOptions();
		options.General.DateFormat = "yyyy-MM-dd";
		var timeline = Render<PDTimeline>(p => p
			.Add(x => x.MinDateTime, _min)
			.Add(x => x.MaxDateTime, _max)
			.Add(x => x.Scale, TimelineScale.Days)
			.Add(x => x.Options, options)).Instance;

		var cut = RenderToolbar(timeline);

		cut.Find(".range-bar .range-min-label").TextContent.Should().Be("2026-01-01 00:00:00");
	}

	/// <summary>The zoom buttons step the timeline one configured scale finer or coarser.</summary>
	[Fact]
	public async Task ZoomButtons_StepTheTimelineScale()
	{
		var timeline = RenderTimeline(_max).Instance;
		var cut = RenderToolbar(timeline);

		await cut.Find("button[title='Zoom In']").ClickAsync(new MouseEventArgs());
		timeline.Scale.Name.Should().Be("12 Hours");

		cut.Render();
		cut.Find(".scale-label").TextContent.Should().Be("12 Hours");

		await cut.Find("button[title='Zoom Out']").ClickAsync(new MouseEventArgs());
		await cut.Find("button[title='Zoom Out']").ClickAsync(new MouseEventArgs());
		timeline.Scale.Name.Should().Be("Weeks");
	}

	/// <summary>A selection on the timeline is displayed, and the zoom-to-selection button fits the timeline to it.</summary>
	[Fact]
	public async Task Selection_IsDisplayed_AndZoomToSelectionFitsIt()
	{
		var timeline = RenderTimeline(_max);
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(new DateTime(2026, 1, 10), new DateTime(2026, 1, 12)));
		var selection = timeline.Instance.GetSelection()!;
		var expectedScale = timeline.Instance.GetScaleToFit(selection.StartTime, selection.EndTime)!;
		expectedScale.Name.Should().NotBe("Days", "the zoom must actually change the scale for this test to prove anything");

		var cut = RenderToolbar(timeline.Instance);

		cut.Find(".selection-bar .range-min-label").TextContent.Should().Be(Format(selection.StartTime));
		cut.Find(".selection-bar .range-max-label").TextContent.Should().Be(Format(selection.EndTime));
		await cut.Find("button[title='Zoom to selection']").ClickAsync(new MouseEventArgs());
		timeline.Instance.Scale.Name.Should().Be(expectedScale.Name);
	}

	/// <summary>Each Show flag hides its own part of the toolbar.</summary>
	[Fact]
	public async Task ShowFlags_False_HideTheirSections()
	{
		var timeline = RenderTimeline(_max);
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(new DateTime(2026, 1, 10), new DateTime(2026, 1, 12)));

		var cut = Render<PDTimelineToolbar>(p => p
			.Add(x => x.Timeline, timeline.Instance)
			.Add(x => x.ShowRange, false)
			.Add(x => x.ShowScale, false)
			.Add(x => x.ShowSelection, false)
			.Add(x => x.ShowZoomButtons, false));

		cut.FindAll("button").Should().BeEmpty();
		cut.FindAll(".scale-label, .selection-bar, .range-bar").Should().BeEmpty();
	}

	/// <summary>The Follow Now toggle starts live following, then pauses it, updating its style and tooltip.</summary>
	[Fact]
	public async Task FollowNowToggle_StartsThenPausesLiveFollowing()
	{
		var timeline = RenderTimeline(null).Instance;
		var cut = RenderToolbar(timeline, showFollowNow: true);
		timeline.IsFollowingNow.Should().BeFalse();

		await cut.Find("button[title='Follow Now']").ClickAsync(new MouseEventArgs());
		timeline.IsFollowingNow.Should().BeTrue();
		cut.Render();
		cut.Find("button[title='Pause live following']").ClassList.Should().Contain("btn-primary");

		await cut.Find("button[title='Pause live following']").ClickAsync(new MouseEventArgs());
		timeline.IsFollowingNow.Should().BeFalse();
		cut.Render();
		cut.Find("button[title='Follow Now']").ClassList.Should().Contain("btn-outline-primary");
	}

	/// <summary>A timeline with a fixed maximum date cannot be followed, so the Follow Now toggle is disabled.</summary>
	[Fact]
	public void FollowNowToggle_WithFixedMaximum_IsDisabled()
	{
		var timeline = RenderTimeline(_max).Instance;

		var cut = RenderToolbar(timeline, showFollowNow: true);

		cut.Find("button[title='Follow Now']").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Disable, Enable and SetEnabled switch the toolbar's own enabled state, which gates the Follow Now toggle.</summary>
	[Fact]
	public async Task EnableDisable_GateTheFollowNowToggle()
	{
		var timeline = RenderTimeline(null).Instance;
		var cut = RenderToolbar(timeline, showFollowNow: true);
		IsFollowNowDisabled(cut).Should().BeFalse();

		await cut.InvokeAsync(cut.Instance.Disable);
		cut.Instance.IsEnabled.Should().BeFalse();
		IsFollowNowDisabled(cut).Should().BeTrue();

		await cut.InvokeAsync(cut.Instance.Enable);
		IsFollowNowDisabled(cut).Should().BeFalse();

		await cut.InvokeAsync(() => cut.Instance.SetEnabled(false));
		IsFollowNowDisabled(cut).Should().BeTrue();
	}

	private static bool IsFollowNowDisabled(IRenderedComponent<PDTimelineToolbar> cut)
		=> cut.Find("button[title='Follow Now']").HasAttribute("disabled");
}
