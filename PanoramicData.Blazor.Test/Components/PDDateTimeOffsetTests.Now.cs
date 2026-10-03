using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests of the live clock ("now") option of <see cref="PDDateTimeOffset"/>.
/// </summary>
public partial class PDDateTimeOffsetTests
{
	private IRenderedComponent<PDDateTimeOffset> RenderNow(bool isNow, List<bool> nowChanges, int intervalMs = 20)
		=> Render<PDDateTimeOffset>(parameters => parameters
			.Add(p => p.Value, _value)
			.Add(p => p.ShowTime, true)
			.Add(p => p.ShowOffset, true)
			.Add(p => p.ShowNow, true)
			.Add(p => p.IsNow, isNow)
			.Add(p => p.LiveUpdateIntervalMs, intervalMs)
			.Add(p => p.IsNowChanged, (bool b) => nowChanges.Add(b))
			.Add(p => p.ValueChanged, (DateTimeOffset v) => _changes.Add(v)));

	/// <summary>Ticking Now raises IsNowChanged, jumps to the current instant, disables the inputs and keeps ticking.</summary>
	[Fact]
	public async Task TickingNow_FollowsTheLiveClock()
	{
		var nowChanges = new List<bool>();
		var component = RenderNow(false, nowChanges);
		var before = DateTimeOffset.UtcNow;

		await component.Find("input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = true });

		nowChanges.Should().Equal(true);
		_changes.Should().NotBeEmpty();
		_changes[0].UtcDateTime.Should().BeOnOrAfter(before.UtcDateTime.AddSeconds(-1));
		component.Find("input.date").HasAttribute("disabled").Should().BeTrue();
		component.Find("select.offset").HasAttribute("disabled").Should().BeTrue();
		component.WaitForAssertion(() => _changes.Count.Should().BeGreaterThanOrEqualTo(3), TimeSpan.FromSeconds(5));
	}

	/// <summary>Unticking Now raises IsNowChanged and re-enables the inputs.</summary>
	[Fact]
	public async Task UntickingNow_ReenablesTheInputs()
	{
		var nowChanges = new List<bool>();
		var component = RenderNow(true, nowChanges);

		// awaited: the live clock is already ticking, and the synchronous Change returns before the handler
		// runs whenever a tick holds the renderer's dispatcher
		await component.Find("input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = "false" });

		nowChanges.Should().Equal(false);
		component.Find("input.date").HasAttribute("disabled").Should().BeFalse();
		component.Find("input[type=checkbox]").HasAttribute("checked").Should().BeFalse();
	}

	/// <summary>A checkbox value that is neither a boolean nor readable as one counts as unticked.</summary>
	[Fact]
	public async Task UnreadableNowValue_CountsAsUnticked()
	{
		var nowChanges = new List<bool>();
		var component = RenderNow(false, nowChanges);

		await component.Find("input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = "on" });

		nowChanges.Should().Equal(false);
		_changes.Should().BeEmpty();
	}

	/// <summary>Starting with Now ticked follows the clock from the first render, even with no interval given.</summary>
	[Theory]
	[InlineData(20)]
	[InlineData(0)]
	public void StartingWithNow_FollowsTheClock(int intervalMs)
	{
		var component = RenderNow(true, [], intervalMs);

		component.WaitForAssertion(() => _changes.Should().NotBeEmpty(), TimeSpan.FromSeconds(5));
		component.Find("input[type=checkbox]").HasAttribute("checked").Should().BeTrue();
	}

	/// <summary>Disposing stops the clock and may safely be repeated.</summary>
	[Fact]
	public async Task Dispose_StopsTheClock_AndIsRepeatable()
	{
		var component = RenderNow(true, []);
		component.WaitForAssertion(() => _changes.Should().NotBeEmpty(), TimeSpan.FromSeconds(5));

		await component.InvokeAsync(component.Instance.Dispose);
		var act = () => component.Instance.Dispose();

		act.Should().NotThrow();
	}
}
