using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Fetching, clearing and disposal tests for <see cref="PDTimeline"/>.
/// </summary>
public partial class PDTimelineTests
{
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
	/// Verifies that disposing the timeline after its circuit has gone does not fail.
	/// </summary>
	[Fact]
	public async Task Dispose_AfterDisconnect_DoesNotThrow()
	{
		_module.SetupVoid("dispose", _ => true).SetException(new Microsoft.JSInterop.JSDisconnectedException("gone"));
		RenderTimeline();

		var dispose = async () => await DisposeComponentsAsync();

		await dispose.Should().NotThrowAsync();
		_module.VerifyInvoke("dispose");
	}

	/// <summary>
	/// Verifies that clearing without saying whether to keep the selection clears the selection too.
	/// </summary>
	[Fact]
	public async Task Clear_ByDefault_ClearsSelection()
	{
		var timeline = RenderTimeline();
		await timeline.InvokeAsync(() => timeline.Instance.SetSelection(Day(2), Day(4)));

		await timeline.InvokeAsync(timeline.Instance.Clear);

		timeline.Instance.GetSelection().Should().BeNull();
		_selections[^1].Should().BeNull();
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
}
