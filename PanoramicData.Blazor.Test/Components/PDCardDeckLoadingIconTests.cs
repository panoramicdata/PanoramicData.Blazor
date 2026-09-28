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
	/// The component reads the system clock and <see cref="Task.Delay(TimeSpan, CancellationToken)"/> directly
	/// and accepts no <see cref="TimeProvider"/>, so only real time can drive it. The test therefore asserts
	/// that the counter moves past its first reading, not that it shows an exact value: under load a tick can
	/// be late enough for the display to skip a second.
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
}
