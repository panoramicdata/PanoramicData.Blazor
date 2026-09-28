using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDCanvas"/> renders a canvas element with the requested identity, size and attributes.
/// </summary>
public class PDCanvasTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDCanvasTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that an unconfigured canvas is 400 by 300 and gets a generated, unique identifier.
	/// </summary>
	[Fact]
	public void Defaults_RenderA400By300CanvasWithAGeneratedId()
	{
		var first = Render<PDCanvas>();
		var second = Render<PDCanvas>();

		var canvas = first.Find("canvas");
		canvas.GetAttribute("width").Should().Be("400");
		canvas.GetAttribute("height").Should().Be("300");
		canvas.Id.Should().StartWith("pd-canvas-");
		second.Find("canvas").Id.Should().NotBe(canvas.Id);
	}

	/// <summary>
	/// Verifies that explicit dimensions, an explicit identifier and unmatched attributes all reach the element.
	/// </summary>
	[Fact]
	public void Parameters_AndUnmatchedAttributes_AreAppliedToTheCanvas()
	{
		var component = Render<PDCanvas>(parameters => parameters
			.Add(p => p.Id, "my-canvas")
			.Add(p => p.Width, 640)
			.Add(p => p.Height, 480)
			.AddUnmatched("class", "drawing")
			.AddUnmatched("data-role", "chart"));

		var canvas = component.Find("canvas");
		canvas.Id.Should().Be("my-canvas");
		canvas.GetAttribute("width").Should().Be("640");
		canvas.GetAttribute("height").Should().Be("480");
		canvas.ClassName.Should().Be("drawing");
		canvas.GetAttribute("data-role").Should().Be("chart");
		component.Instance.Attributes.Should().ContainKey("data-role");
	}
}
