using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Follow-now tests for <see cref="PDTimeline"/>, driven by a manual clock.
/// </summary>
public partial class PDTimelineTests
{
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

		timeline.WaitForAssertion(() => _queries.Should().NotBeEmpty(), _wait);
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
			.Should().BeEquivalentTo(Range(end.AddDays(-2), end.AddDays(1))), _wait);
	}

	/// <summary>
	/// A clock whose time and timers only move when the test says so.
	/// </summary>
	private sealed class ManualClock(DateTime now) : TimeProvider
	{
		private readonly List<ManualTimer> _timers = [];
		private DateTimeOffset _current = new(now, TimeSpan.Zero);

		public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

		public override DateTimeOffset GetUtcNow() => _current;

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			var timer = new ManualTimer(callback, state, dueTime, period);
			_timers.Add(timer);
			return timer;
		}

		public void Advance(TimeSpan by)
		{
			_current += by;
			foreach (var timer in _timers.ToList())
			{
				timer.Fire();
			}
		}
	}

	/// <summary>
	/// A timer that fires only when its clock is advanced.
	/// </summary>
	private sealed class ManualTimer(TimerCallback callback, object? state, TimeSpan initialDueTime, TimeSpan initialPeriod) : ITimer
	{
		private bool _disposed;
		private bool _armed = initialDueTime != Timeout.InfiniteTimeSpan;
		private bool _periodic = initialPeriod != Timeout.InfiniteTimeSpan;

		public bool Change(TimeSpan dueTime, TimeSpan period)
		{
			if (_disposed)
			{
				return false;
			}

			_armed = dueTime != Timeout.InfiniteTimeSpan;
			_periodic = period != Timeout.InfiniteTimeSpan;
			return true;
		}

		/// <summary>Fires the timer if it is armed; a one-shot timer then disarms, as a real one would.</summary>
		public void Fire()
		{
			if (_disposed || !_armed)
			{
				return;
			}

			_armed = _periodic;
			callback(state);
		}

		public void Dispose() => _disposed = true;

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
