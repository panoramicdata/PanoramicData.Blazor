using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDCardDeckLoadingIcon"/> appears after its short start-up delay, counts elapsed
/// seconds, and goes away when disposed.
/// </summary>
public class PDCardDeckLoadingIconTests : BunitContext
{
	// Generous because the component runs on real time and the whole suite runs in parallel: a wait that is
	// normally about a second can be delayed several-fold under load. The waits return as soon as they pass.
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

	/// <summary>Sets up the rendering context.</summary>
	public PDCardDeckLoadingIconTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that the icon renders nothing until its start-up delay has passed, then shows the loading cards
	/// with an elapsed time of zero seconds.
	/// </summary>
	[Fact]
	public void BecomesActive_AfterStartupDelay()
	{
		var component = Render<PDCardDeckLoadingIcon>();

		component.Instance.IsActive.Should().BeFalse();
		component.Markup.Trim().Should().BeEmpty();

		component.WaitForAssertion(() => component.FindAll(".pd-carddeck-loading").Should().ContainSingle(), _timeout);
		component.Instance.IsActive.Should().BeTrue();
		component.FindAll(".loading-card").Should().HaveCount(3);
		ElapsedSeconds(component).Should().BeGreaterThanOrEqualTo(0);
	}

	/// <summary>
	/// Verifies that the elapsed-time counter advances while the icon is active.
	/// </summary>
	/// <remarks>
	/// This runs on the default system clock, to prove the real-time path works; exact values are checked
	/// against a supplied clock in <see cref="ElapsedTime_IsMeasuredOnTheSuppliedClock"/>. The test therefore
	/// asserts only that the counter moves past its first reading: under load a tick can be late enough for
	/// the display to skip a second.
	/// </remarks>
	[Fact]
	public void ElapsedTime_Advances()
	{
		var component = Render<PDCardDeckLoadingIcon>();
		component.WaitForState(() => component.Instance.IsActive, _timeout);
		var first = ElapsedSeconds(component);

		component.WaitForAssertion(() => ElapsedSeconds(component).Should().BeGreaterThan(first), _timeout);
	}

	private static int ElapsedSeconds(IRenderedComponent<PDCardDeckLoadingIcon> component)
	{
		var match = System.Text.RegularExpressions.Regex.Match(
			component.Find(".loading-message").TextContent,
			@"current elapsed time (\d+) seconds",
			System.Text.RegularExpressions.RegexOptions.None,
			TimeSpan.FromSeconds(1));
		match.Success.Should().BeTrue();
		return int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Verifies that disposing the icon deactivates it, so the next render shows nothing, and that a second
	/// dispose is harmless.
	/// </summary>
	[Fact]
	public void Dispose_Deactivates()
	{
		var component = Render<PDCardDeckLoadingIcon>();
		component.WaitForState(() => component.Instance.IsActive, _timeout);

		component.Instance.Dispose();
		component.Render();

		component.Instance.IsActive.Should().BeFalse();
		component.FindAll(".pd-carddeck-loading").Should().BeEmpty();

		component.Instance.Dispose();
		component.Instance.IsActive.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that an icon disposed during its start-up delay, as happens whenever data arrives quickly, never
	/// becomes active and leaves no elapsed-time timer running (#195).
	/// </summary>
	[Fact]
	public async Task DisposedDuringStartupDelay_NeverStartsItsTimer()
	{
		var clock = new StepClock();
		var component = Render<PDCardDeckLoadingIcon>(parameters => parameters.Add(p => p.Clock, clock));
		var icon = component.Instance;
		clock.LiveTimers.Should().Be(1, "the start-up delay is waiting on the clock");

		await DisposeComponentsAsync();
		clock.FireAll();
		// Let anything the start-up delay resumed run to completion on the renderer's dispatcher.
		await component.InvokeAsync(() => clock.FireAll());

		icon.IsActive.Should().BeFalse();
		clock.LiveTimers.Should().Be(0);
	}

	/// <summary>
	/// Verifies that the elapsed time shown is measured on the supplied clock.
	/// </summary>
	[Fact]
	public async Task ElapsedTime_IsMeasuredOnTheSuppliedClock()
	{
		var clock = new StepClock();
		var component = Render<PDCardDeckLoadingIcon>(parameters => parameters.Add(p => p.Clock, clock));

		await component.InvokeAsync(() => clock.FireAll());
		component.WaitForState(() => component.Instance.IsActive && clock.LiveTimers == 1, _timeout);

		clock.Now += TimeSpan.FromSeconds(7);
		await component.InvokeAsync(() => clock.FireAll());

		component.WaitForAssertion(() => ElapsedSeconds(component).Should().Be(7), _timeout);
	}

	/// <summary>
	/// A clock whose timers fire only when the test says so.
	/// </summary>
	private sealed class StepClock : TimeProvider
	{
		private readonly List<StepTimer> _timers = [];

		public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

		public int LiveTimers => _timers.Count(t => !t.IsDisposed);

		public override DateTimeOffset GetUtcNow() => Now;

		/// <summary>Whether a timer with these times would never fire.</summary>
		public static bool IsDisarmed(TimeSpan dueTime, TimeSpan period)
			=> dueTime == Timeout.InfiniteTimeSpan && period == Timeout.InfiniteTimeSpan;

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			var timer = new StepTimer(callback, state, IsDisarmed(dueTime, period));
			_timers.Add(timer);
			return timer;
		}

		/// <summary>Fires every timer that is still live, once.</summary>
		public void FireAll()
		{
			foreach (var timer in _timers.Where(t => !t.IsDisposed).ToList())
			{
				timer.Fire();
			}
		}
	}

	/// <summary>
	/// A one-shot timer that fires when its clock tells it to, then counts as spent.
	/// </summary>
	private sealed class StepTimer(TimerCallback callback, object? state, bool disarmed) : ITimer
	{
		public bool IsDisposed { get; private set; } = disarmed;

		/// <summary>Re-arms the timer, or disarms it when both times are infinite.</summary>
		bool ITimer.Change(TimeSpan dueTime, TimeSpan period)
		{
			if (IsDisposed)
			{
				return false;
			}

			IsDisposed = StepClock.IsDisarmed(dueTime, period);
			return true;
		}

		public void Fire()
		{
			IsDisposed = true;
			callback(state);
		}

		public void Dispose() => IsDisposed = true;

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
