using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDCardDeckLoadingIcon"/> appears after its short start-up delay, counts elapsed
/// seconds, and goes away when disposed.
/// </summary>
public class PDCardDeckLoadingIconTests : BunitContext
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

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
		component.Find(".loading-message").TextContent.Should().Contain("current elapsed time 0 seconds");
	}

	/// <summary>
	/// Verifies that the elapsed-time counter advances while the icon is active.
	/// </summary>
	[Fact]
	public void ElapsedTime_Advances()
	{
		var component = Render<PDCardDeckLoadingIcon>();

		component.WaitForAssertion(
			() => component.Find(".loading-message").TextContent.Should().Contain("current elapsed time 1 seconds"),
			_timeout);
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
