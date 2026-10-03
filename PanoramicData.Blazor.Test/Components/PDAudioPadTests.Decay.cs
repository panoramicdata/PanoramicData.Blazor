using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Press, release and decay tests for <see cref="PDAudioPad"/>.
/// </summary>
public partial class PDAudioPadTests
{
	/// <summary>Configured for release, the pad ignores the press and acts on the release.</summary>
	[Fact]
	public async Task DecayUponRelease_ActsOnMouseUpOnly()
	{
		var component = RenderPad(p => p.Add(x => x.Value, 0.0).Add(x => x.DecayUpon, DecayUpon.Release));

		await component.Find("svg").MouseDownAsync(new MouseEventArgs());
		_values.Should().BeEmpty();

		await component.Find("svg").MouseUpAsync(new MouseEventArgs());
		_values.Should().Equal(1.0);
	}

	/// <summary>Configured for press, a release does nothing.</summary>
	[Fact]
	public async Task DecayUponPress_IgnoresMouseUp()
	{
		var component = RenderPad();

		await component.Find("svg").MouseUpAsync(new MouseEventArgs());

		_values.Should().BeEmpty();
	}

	/// <summary>A decaying pad jumps to full and decays back to its minimum.</summary>
	[Theory]
	[InlineData(DecayMode.Exponential)]
	[InlineData(DecayMode.Linear)]
	public async Task Decay_JumpsToFull_ThenDecaysToTheMinimum(DecayMode mode)
	{
		var component = RenderPad(p => p
			.Add(x => x.Value, 0.0)
			.Add(x => x.DecayMode, mode)
			.Add(x => x.DecayHalfLife, TimeSpan.FromMilliseconds(40))
			.Add(x => x.EventThrottleMs, 10000));

		await component.Find("svg").MouseDownAsync(new MouseEventArgs());

		component.WaitForAssertion(() => component.Instance.Value.Should().Be(0.0), TimeSpan.FromSeconds(5));
		_values[0].Should().Be(1.0);
		_values[^1].Should().Be(0.0);
		_values.Should().BeInDescendingOrder();
		_events[0].Value.Should().Be(1.0);
		_events[0].IsActive.Should().BeTrue();
		_events.Should().AllSatisfy(e => e.DecayMode.Should().Be(mode));
	}

	/// <summary>An exponential decay, throttled, reports the activation and the final inactive state.</summary>
	[Fact]
	public async Task ExponentialDecay_Throttled_ReportsActivationAndCompletion()
	{
		var component = RenderPad(p => p
			.Add(x => x.Value, 0.0)
			.Add(x => x.DecayMode, DecayMode.Exponential)
			.Add(x => x.DecayHalfLife, TimeSpan.FromMilliseconds(40))
			.Add(x => x.EventThrottleMs, 10000));

		await component.Find("svg").MouseDownAsync(new MouseEventArgs());

		component.WaitForAssertion(() => component.Instance.Value.Should().Be(0.0), TimeSpan.FromSeconds(5));
		_events.Select(e => e.Value).Should().Equal(1.0, 0.0);
		_events[^1].IsActive.Should().BeFalse();
	}

	/// <summary>
	/// A linear decay, throttled, reports the activation and the final inactive state, as the exponential decay
	/// does (#166).
	/// </summary>
	[Fact]
	public async Task LinearDecay_Throttled_ReportsActivationAndCompletion()
	{
		var component = RenderPad(p => p
			.Add(x => x.Value, 0.0)
			.Add(x => x.DecayMode, DecayMode.Linear)
			.Add(x => x.DecayHalfLife, TimeSpan.FromMilliseconds(40))
			.Add(x => x.EventThrottleMs, 10000));

		await component.Find("svg").MouseDownAsync(new MouseEventArgs());

		component.WaitForAssertion(() => _events.Select(e => e.Value).Should().Equal(1.0, 0.0), TimeSpan.FromSeconds(5));
		_events[^1].IsActive.Should().BeFalse();
		component.Instance.Value.Should().Be(0.0);
	}

	/// <summary>
	/// A linear decay with no throttle reports the final inactive state exactly once, not a duplicate forced
	/// event after the throttled one (#166).
	/// </summary>
	[Fact]
	public async Task LinearDecay_WithoutThrottle_ReportsTheFinalStateOnce()
	{
		var component = RenderPad(p => p
			.Add(x => x.Value, 0.0)
			.Add(x => x.DecayMode, DecayMode.Linear)
			.Add(x => x.DecayHalfLife, TimeSpan.FromMilliseconds(40))
			.Add(x => x.EventThrottleMs, 0));

		await component.Find("svg").MouseDownAsync(new MouseEventArgs());

		component.WaitForAssertion(() => _events[^1].Value.Should().Be(0.0), TimeSpan.FromSeconds(5));
		await component.InvokeAsync(() => Task.CompletedTask);
		_events.Count(e => e.Value == 0.0).Should().Be(1);
	}

	/// <summary>With no throttle, the decay reports intermediate values as well.</summary>
	[Fact]
	public async Task Decay_WithoutThrottle_ReportsIntermediateValues()
	{
		var component = RenderPad(p => p
			.Add(x => x.Value, 0.0)
			.Add(x => x.DecayMode, DecayMode.Exponential)
			.Add(x => x.DecayHalfLife, TimeSpan.FromMilliseconds(60))
			.Add(x => x.EventThrottleMs, 0));

		await component.Find("svg").MouseDownAsync(new MouseEventArgs());

		component.WaitForAssertion(() => component.Instance.Value.Should().Be(0.0), TimeSpan.FromSeconds(5));
		_events.Count.Should().BeGreaterThan(2);
		_events.Where(e => e.Value is > 0 and < 1).Should().NotBeEmpty();
	}

	/// <summary>A linear decay stops at a raised minimum rather than at zero.</summary>
	[Fact]
	public async Task LinearDecay_StopsAtTheMinimum()
	{
		var component = RenderPad(p => p
			.Add(x => x.Value, 0.0)
			.Add(x => x.DecayMode, DecayMode.Linear)
			.Add(x => x.MinValue, 0.25)
			.Add(x => x.ZeroBelow, null)
			.Add(x => x.DecayHalfLife, TimeSpan.FromMilliseconds(40)));

		await component.Find("svg").MouseDownAsync(new MouseEventArgs());

		component.WaitForAssertion(() => _values[^1].Should().Be(0.25), TimeSpan.FromSeconds(5));
		_values.Should().AllSatisfy(v => v.Should().BeGreaterThanOrEqualTo(0.25));
	}

	/// <summary>Pressing again during a decay restarts it from full.</summary>
	/// <remarks>
	/// The presses are awaited: the decay loop from the first press runs on the renderer's dispatcher, and
	/// a synchronous press dispatched while it holds the dispatcher returns before the handler has run.
	/// </remarks>
	[Fact]
	public async Task PressDuringDecay_RestartsFromFull()
	{
		var component = RenderPad(p => p
			.Add(x => x.Value, 0.0)
			.Add(x => x.DecayMode, DecayMode.Exponential)
			.Add(x => x.DecayHalfLife, TimeSpan.FromSeconds(30))
			.Add(x => x.EventThrottleMs, 10000));

		await component.Find("svg").MouseDownAsync(new MouseEventArgs());
		await component.Find("svg").MouseDownAsync(new MouseEventArgs());

		_events.Select(e => e.Value).Should().Equal(1.0, 1.0);
		component.Instance.Value.Should().BeGreaterThan(0.9);
	}

	/// <summary>
	/// A decay that ends because a newer press superseded it leaves the value alone and reports nothing
	/// (issue #219).
	/// </summary>
	/// <remarks>
	/// In real use the race is timing-dependent: the old decay's token is cancelled just after one of its
	/// delays completes, so it leaves its loop through the cancellation check rather than inside the delay.
	/// An already-cancelled token takes exactly that path every time, which makes the case deterministic.
	/// </remarks>
	[Fact]
	public async Task SupersededDecay_LeavesTheValueAndReportsNothing()
	{
		var component = RenderPad(p => p
			.Add(x => x.Value, 0.8)
			.Add(x => x.DecayMode, DecayMode.Exponential)
			.Add(x => x.DecayHalfLife, TimeSpan.FromSeconds(30)));
		using var superseded = new CancellationTokenSource();
		await superseded.CancelAsync();

		await component.InvokeAsync(() => component.Instance.DecayAsync(superseded.Token));

		component.Instance.Value.Should().Be(0.8);
		_values.Should().BeEmpty();
		_events.Should().BeEmpty();
	}

	/// <summary>Disposing during a decay does not throw.</summary>
	[Fact]
	public async Task Dispose_DuringADecay_DoesNotThrow()
	{
		var component = RenderPad(p => p
			.Add(x => x.DecayMode, DecayMode.Linear)
			.Add(x => x.DecayHalfLife, TimeSpan.FromSeconds(30)));
		await component.Find("svg").MouseDownAsync(new MouseEventArgs());

		var act = () => component.InvokeAsync(() => component.Instance.DisposeAsync().AsTask());

		await act.Should().NotThrowAsync();
	}
}
