using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests the search debounce against a clock the test controls, so that the order of a debounce elapsing
/// and a newer keystroke arriving is fixed rather than raced (issue #218).
/// </summary>
public partial class PDChatConversationSidebarTests
{
	private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

	/// <summary>
	/// A keystroke whose debounce has already elapsed, but whose search has not yet started when a newer
	/// keystroke supersedes it, neither empties the list nor calls the store.
	/// </summary>
	/// <remarks>
	/// The first debounce is completed from another thread while the test holds the renderer's dispatcher,
	/// so its continuation is queued behind the second keystroke: exactly the order in which the timer wins
	/// the race but the newer keystroke is handled first. Nothing here waits on real time.
	/// </remarks>
	[Fact]
	public async Task A_superseded_search_whose_debounce_elapsed_neither_clears_nor_queries()
	{
		Add("Alpha", TimeSpan.Zero);
		var component = RenderSidebar();
		var clock = new ManualTimeProvider();
		component.Instance.Clock = clock;
		var emptied = false;
		component.OnMarkupUpdated += (_, _) => emptied |= component.FindAll(".pdchat-conversation-row").Count == 0;
		Task first = Task.CompletedTask;
		Task second = Task.CompletedTask;

		await component.InvokeAsync(() =>
		{
			first = component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "Al" });
			AdvanceFromAnotherThread(clock);
			second = component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "Alp" });
		});
		// The first keystroke's handler has now finished, and the newer debounce has not been released.
		await first;
		emptied.Should().BeFalse("a superseded search must not clear the list the newer search is about to fill");
		_service.Queries.Should().ContainSingle("a superseded search must not call the store");

		clock.Advance(Debounce);
		await second;

		component.WaitForAssertion(() => component.FindAll(".pdchat-conversation-row").Should().ContainSingle(), DebounceTimeout);
		_service.Queries.Select(q => q.SearchText).Should().Equal(string.Empty, "Alp");
	}

	/// <summary>
	/// Fires due timers on another thread and waits for it, so that a continuation captured on the renderer's
	/// dispatcher is posted to it rather than run inline on the thread that fired the timer.
	/// </summary>
	private static void AdvanceFromAnotherThread(ManualTimeProvider clock)
	{
		var thread = new Thread(() => clock.Advance(Debounce));
		thread.Start();
		thread.Join();
	}

	/// <summary>A clock whose timers fire only when the test advances it.</summary>
	private sealed class ManualTimeProvider : TimeProvider
	{
		private readonly Lock _lock = new();
		private readonly List<ManualTimer> _timers = [];
		private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

		public override DateTimeOffset GetUtcNow()
		{
			lock (_lock)
			{
				return _now;
			}
		}

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			// Task.Delay only ever asks for a one-shot timer; anything periodic would be a test mistake.
			if (period != Timeout.InfiniteTimeSpan)
			{
				throw new NotSupportedException($"This clock only drives one-shot timers, not a period of {period}.");
			}

			lock (_lock)
			{
				var timer = new ManualTimer(this, callback, state);
				timer.Schedule(dueTime);
				_timers.Add(timer);
				return timer;
			}
		}

		/// <summary>Moves time on and fires, outside the lock, every timer that has fallen due.</summary>
		public void Advance(TimeSpan by)
		{
			List<ManualTimer> due;
			lock (_lock)
			{
				_now += by;
				due = [.. _timers.Where(t => t.DueAt <= _now)];
				foreach (var timer in due)
				{
					timer.Schedule(Timeout.InfiniteTimeSpan);
				}
			}

			foreach (var timer in due)
			{
				timer.Fire();
			}
		}

		public void Remove(ManualTimer timer)
		{
			lock (_lock)
			{
				_timers.Remove(timer);
			}
		}

		/// <summary>The clock's current time; callers hold the lock.</summary>
		public DateTimeOffset Now => _now;
	}

	/// <summary>A one-shot timer driven by <see cref="ManualTimeProvider"/>.</summary>
	private sealed class ManualTimer(ManualTimeProvider clock, TimerCallback callback, object? state) : ITimer
	{
		/// <summary>When the timer next fires; <see cref="DateTimeOffset.MaxValue"/> while disarmed.</summary>
		public DateTimeOffset DueAt { get; private set; } = DateTimeOffset.MaxValue;

		public void Schedule(TimeSpan dueTime)
			=> DueAt = dueTime == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : clock.Now + dueTime;

		public void Fire() => callback(state);

		public bool Change(TimeSpan dueTime, TimeSpan period)
		{
			// Task.Delay only ever creates one-shot timers, so a period is recorded nowhere.
			Schedule(dueTime);
			return period == Timeout.InfiniteTimeSpan;
		}

		public void Dispose() => clock.Remove(this);

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
