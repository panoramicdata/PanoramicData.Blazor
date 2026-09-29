using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDBusyOverlay"/> shows its overlay only while busy, and always keeps its child content.
/// </summary>
public class PDBusyOverlayTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDBusyOverlayTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that a component that is not busy is marked hidden, renders no overlay and still shows its content.
	/// </summary>
	[Fact]
	public void NotBusy_HidesOverlay_AndShowsChildContent()
	{
		var component = Render<PDBusyOverlay>(parameters => parameters
			.Add(p => p.CssClass, "custom")
			.Add(p => p.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p class=\"child\">Body</p>"))));

		var root = component.Find("div.pd-busy-overlay");
		root.ClassList.Should().Contain(["hide", "custom"]);
		root.ClassList.Should().NotContain("show");
		component.FindAll(".overlay-content").Should().BeEmpty();
		component.Find("p.child").TextContent.Should().Be("Body");
	}

	/// <summary>
	/// Verifies that a busy component shows the default spinner icon using <see cref="PDBusyOverlay.OverlayCssClass"/>.
	/// </summary>
	[Fact]
	public void Busy_WithoutOverlayContent_ShowsIconWithOverlayCssClass()
	{
		var component = Render<PDBusyOverlay>(parameters => parameters
			.Add(p => p.IsBusy, true)
			.Add(p => p.OverlayCssClass, "my-spinner"));

		component.Find("div.pd-busy-overlay").ClassList.Should().Contain("show");
		component.Find(".overlay-content i").ClassName.Should().Be("my-spinner");
	}

	/// <summary>
	/// Verifies that custom overlay content replaces the default icon while busy.
	/// </summary>
	[Fact]
	public void Busy_WithOverlayContent_RendersContentInsteadOfIcon()
	{
		var component = Render<PDBusyOverlay>(parameters => parameters
			.Add(p => p.IsBusy, true)
			.Add(p => p.OverlayContent, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"please-wait\">Wait</span>"))));

		component.FindAll(".overlay-content i").Should().BeEmpty();
		component.Find(".overlay-content .please-wait").TextContent.Should().Be("Wait");
	}
}
