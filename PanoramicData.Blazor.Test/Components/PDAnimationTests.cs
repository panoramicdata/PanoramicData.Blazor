using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDAnimation"/> tracks its element's position through the JS module and animates
/// from the previous position to the current one.
/// </summary>
public class PDAnimationTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDAnimation.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDAnimationTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private (IRenderedComponent<PDAnimation> Component, BunitJSModuleInterop Module, JSRuntimeInvocationHandler<ElementPosition?> Position)
		RenderAnimation(AnimationTransition transition = AnimationTransition.EaseOut, ElementPosition? initial = null)
	{
		var module = JSInterop.SetupModule(ModulePath);
		var position = module.Setup<ElementPosition?>("getPosition", _ => true);
		position.SetResult(initial ?? new ElementPosition { Top = 10, Left = 20 });

		var component = Render<PDAnimation>(parameters => parameters
			.Add(p => p.Id, "anim")
			.Add(p => p.Transition, transition)
			.Add(p => p.AnimationTime, 0.5)
			.Add(p => p.Element, "<span class=\"moving\">Card</span>"));

		return (component, module, position);
	}

	/// <summary>
	/// Verifies that the element is rendered inside a container carrying the animation id, which is what
	/// the JS module looks up.
	/// </summary>
	[Fact]
	public void Element_IsRenderedInsideTheIdentifiedContainer()
	{
		var (component, module, _) = RenderAnimation();

		component.Find("div#anim span.moving").TextContent.Should().Be("Card");
		module.VerifyInvoke("getPosition").Arguments.Should().Equal("anim");
	}

	/// <summary>
	/// Verifies that a move animates from the position recorded at render to the new one, passing the
	/// duration and the CSS timing function for the configured transition.
	/// </summary>
	[Theory]
	[InlineData(AnimationTransition.Initial, "initial")]
	[InlineData(AnimationTransition.Inherit, "inherit")]
	[InlineData(AnimationTransition.Linear, "linear")]
	[InlineData(AnimationTransition.Ease, "ease")]
	[InlineData(AnimationTransition.EaseIn, "ease-in")]
	[InlineData(AnimationTransition.EaseOut, "ease-out")]
	[InlineData(AnimationTransition.EaseInOut, "ease-in-out")]
	[InlineData(AnimationTransition.StepStart, "step-start")]
	[InlineData(AnimationTransition.StepEnd, "step-end")]
	[InlineData((AnimationTransition)999, "ease-in-out")]
	public async Task AnimateElementAsync_AnimatesFromThePreviousToTheCurrentPosition(AnimationTransition transition, string expectedStyle)
	{
		var (component, module, position) = RenderAnimation(transition);
		position.SetResult(new ElementPosition { Top = 50, Left = 60 });

		await component.InvokeAsync(() => component.Instance.AnimateElementAsync());

		var args = module.VerifyInvoke("animate").Arguments;
		args[0].Should().Be("anim");
		args[1].Should().Be(new ElementPosition { Top = 10, Left = 20 });
		args[2].Should().Be(new ElementPosition { Top = 50, Left = 60 });
		args[3].Should().Be(0.5);
		args[4].Should().Be(expectedStyle);
	}

	/// <summary>
	/// Verifies that an element that has not moved is not animated.
	/// </summary>
	[Fact]
	public async Task AnimateElementAsync_WhenThePositionIsUnchanged_DoesNotAnimate()
	{
		var (component, module, _) = RenderAnimation();

		await component.InvokeAsync(() => component.Instance.AnimateElementAsync());

		module.VerifyNotInvoke("animate");
	}

	/// <summary>
	/// Verifies that an element whose position cannot be found is not animated.
	/// </summary>
	[Fact]
	public async Task AnimateElementAsync_WhenThePositionIsUnknown_DoesNotAnimate()
	{
		var (component, module, position) = RenderAnimation();
		position.SetResult(null);

		await component.InvokeAsync(() => component.Instance.AnimateElementAsync());

		module.VerifyNotInvoke("animate");
	}

	/// <summary>
	/// Verifies that a second move requested straight after the first is throttled rather than animated.
	/// </summary>
	[Fact]
	public async Task AnimateElementAsync_CalledAgainImmediately_IsThrottled()
	{
		var (component, module, position) = RenderAnimation();
		position.SetResult(new ElementPosition { Top = 50, Left = 60 });
		await component.InvokeAsync(() => component.Instance.AnimateElementAsync());

		position.SetResult(new ElementPosition { Top = 70, Left = 80 });
		await component.InvokeAsync(() => component.Instance.UpdatePositionAsync());
		position.SetResult(new ElementPosition { Top = 90, Left = 100 });
		await component.InvokeAsync(() => component.Instance.AnimateElementAsync());

		module.Invocations["animate"].Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that only the two most recent positions are kept, so an animation always runs from the
	/// last position to the current one.
	/// </summary>
	[Fact]
	public async Task UpdatePositionAsync_KeepsOnlyTheLastTwoPositions()
	{
		var (component, module, position) = RenderAnimation();
		position.SetResult(new ElementPosition { Top = 30, Left = 30 });
		await component.InvokeAsync(() => component.Instance.UpdatePositionAsync());
		position.SetResult(new ElementPosition { Top = 40, Left = 40 });

		await component.InvokeAsync(() => component.Instance.AnimateElementAsync());

		var args = module.VerifyInvoke("animate").Arguments;
		args[1].Should().Be(new ElementPosition { Top = 30, Left = 30 });
		args[2].Should().Be(new ElementPosition { Top = 40, Left = 40 });
	}

	/// <summary>
	/// Verifies that clearing the positions forgets the starting point, so the next move has nothing to
	/// animate from.
	/// </summary>
	[Fact]
	public async Task ClearPositions_ForgetsTheStartingPoint()
	{
		var (component, module, position) = RenderAnimation();
		component.Instance.ClearPositions();
		position.SetResult(new ElementPosition { Top = 50, Left = 60 });

		await component.InvokeAsync(() => component.Instance.AnimateElementAsync());

		module.VerifyNotInvoke("animate");
	}

	/// <summary>
	/// Verifies that cancelling asks the module to cancel the animation for this element.
	/// </summary>
	[Fact]
	public async Task CancelAnimationAsync_CallsTheModule()
	{
		var (component, module, _) = RenderAnimation();

		await component.InvokeAsync(() => component.Instance.CancelAnimationAsync());

		module.VerifyInvoke("cancelAnimation").Arguments.Should().Equal("anim");
	}

	/// <summary>
	/// Verifies that after disposal the module is released, so cancelling no longer reaches it, and that
	/// disposing twice is harmless.
	/// </summary>
	[Fact]
	public async Task Dispose_ReleasesTheModule()
	{
		var (component, module, _) = RenderAnimation();

		component.Instance.Dispose();
		component.Instance.Dispose();
		await component.InvokeAsync(() => component.Instance.CancelAnimationAsync());

		module.VerifyNotInvoke("cancelAnimation");
	}
}
