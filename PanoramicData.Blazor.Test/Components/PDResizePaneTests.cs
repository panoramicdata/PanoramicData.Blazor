using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests the markup of <see cref="PDResizePane"/>: the resize handle's corner class and the child content.
/// </summary>
/// <remarks>
/// The JavaScript side is deliberately not asserted on here. The component calls a global <c>init</c>
/// function that its collocated module neither exports nor is ever loaded as, which is reported as a
/// suspected defect rather than pinned by a test.
/// </remarks>
public class PDResizePaneTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDResizePaneTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that each corner renders its own handle class.
	/// </summary>
	/// <param name="corner">The corner used for resizing.</param>
	/// <param name="expectedClass">The class the handle should carry.</param>
	[Theory]
	[InlineData(PDResizePane.ResizeCorner.TopLeft, "handle-tl")]
	[InlineData(PDResizePane.ResizeCorner.TopRight, "handle-tr")]
	[InlineData(PDResizePane.ResizeCorner.BottomLeft, "handle-bl")]
	[InlineData(PDResizePane.ResizeCorner.BottomRight, "handle-br")]
	public void Corner_SetsHandleClass(PDResizePane.ResizeCorner corner, string expectedClass)
	{
		var component = Render<PDResizePane>(parameters => parameters
			.Add(p => p.Corner, corner));

		component.Find(".pdresizepane-handle").ClassList.Should().Contain(expectedClass);
	}

	/// <summary>
	/// Verifies that an undefined corner value falls back to the top-left handle.
	/// </summary>
	[Fact]
	public void UnknownCorner_FallsBackToTopLeft()
	{
		var component = Render<PDResizePane>(parameters => parameters
			.Add(p => p.Corner, (PDResizePane.ResizeCorner)99));

		component.Find(".pdresizepane-handle").ClassList.Should().Contain("handle-tl");
	}

	/// <summary>
	/// Verifies that the default corner is top-left and that child content is rendered inside the content area.
	/// </summary>
	[Fact]
	public async Task Default_IsTopLeft_AndRendersChildContent()
	{
		var component = Render<PDResizePane>(parameters => parameters
			.Add(p => p.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<em>inner</em>"))));

		component.Instance.Corner.Should().Be(PDResizePane.ResizeCorner.TopLeft);
		component.Find(".pdresizepane-content em").TextContent.Should().Be("inner");

		// Disposal holds no resources, so it must complete without touching anything.
		await component.Instance.DisposeAsync();
		component.Find(".pdresizepane-content em").TextContent.Should().Be("inner");
	}
}
