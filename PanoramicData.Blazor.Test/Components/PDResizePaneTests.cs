using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests the markup of <see cref="PDResizePane"/>: the resize handle's corner class and the child content.
/// </summary>
/// <remarks>
/// The component must import its collocated module and call the module's exported <c>init</c>: before #163 it
/// called a global <c>init</c> that nothing defined, which in a browser threw and took down the circuit.
/// </remarks>
public class PDResizePaneTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDResizePane.razor.js";
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

		await component.Instance.DisposeAsync();
		component.Find(".pdresizepane-content em").TextContent.Should().Be("inner");
	}

	/// <summary>
	/// Verifies that the first render imports the module and initialises it with the container, the handle and
	/// the corner class (#163). Strict mode means a call to an unset-up global function would fail the render.
	/// </summary>
	[Fact]
	public void FirstRender_ImportsTheModule_AndInitialisesIt()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("init", _ => true).SetVoidResult();

		Render<PDResizePane>(parameters => parameters.Add(p => p.Corner, PDResizePane.ResizeCorner.BottomRight));

		var init = module.VerifyInvoke("init");
		init.Arguments.Should().HaveCount(3);
		init.Arguments[0].Should().BeOfType<ElementReference>();
		init.Arguments[1].Should().BeOfType<ElementReference>();
		init.Arguments[2].Should().Be("handle-br");
		JSInterop.Invocations.Where(i => i.Identifier == "import").Should().ContainSingle()
			.Which.Arguments.Should().Equal(ModulePath);
	}

	/// <summary>
	/// Verifies that disposal removes the handle's listeners through the module and then releases the module
	/// (#163).
	/// </summary>
	[Fact]
	public async Task DisposeAsync_TearsDownTheModule()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var component = Render<PDResizePane>();

		await component.InvokeAsync(async () => await component.Instance.DisposeAsync());

		var dispose = module.VerifyInvoke("dispose");
		dispose.Arguments.Should().ContainSingle().Which.Should().BeOfType<ElementReference>();
	}

	/// <summary>
	/// Verifies that a circuit which has gone away during initialisation or disposal does not throw (#163).
	/// </summary>
	[Fact]
	public async Task DisconnectedCircuit_IsTolerated()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("init", _ => true).SetException(new JSDisconnectedException("gone"));
		module.SetupVoid("dispose", _ => true).SetException(new JSDisconnectedException("gone"));

		var component = Render<PDResizePane>();
		var dispose = async () => await component.InvokeAsync(async () => await component.Instance.DisposeAsync());

		await dispose.Should().NotThrowAsync();
		module.VerifyInvoke("init");
	}
}
