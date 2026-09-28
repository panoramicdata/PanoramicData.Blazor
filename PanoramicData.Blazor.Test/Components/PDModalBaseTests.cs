using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDModalBase"/> supplies its documented defaults and forwards show and hide requests to
/// the <see cref="PDModal"/> it wraps.
/// </summary>
public class PDModalBaseTests : BunitContext
{
	private const string ModalModulePath = "./_content/PanoramicData.Blazor/PDModal.razor.js";

	private readonly BunitJSModuleInterop _modalObject;

	/// <summary>Sets up the rendering context and the modal's JavaScript module.</summary>
	public PDModalBaseTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		var module = JSInterop.SetupModule(ModalModulePath);
		_modalObject = module.SetupModule(invocation => invocation.Identifier == "initialize");
	}

	/// <summary>The defaults are Yes, No and Cancel buttons, a large size, escape to close and a close button.</summary>
	[Fact]
	public void Defaults_are_as_documented()
	{
		var component = Render<TestModal>();
		var modal = component.Instance;

		modal.Buttons.OfType<ToolbarButton>().Select(b => b.Key)
			.Should().Equal(ModalResults.YES, ModalResults.NO, ModalResults.CANCEL);
		modal.Buttons.OfType<ToolbarButton>().First().CssClass.Should().Be("btn-primary");
		modal.Size.Should().Be(ModalSizes.Large);
		modal.CloseOnEscape.Should().BeTrue();
		modal.ShowClose.Should().BeTrue();
		modal.CenterVertically.Should().BeFalse();
		modal.HideOnBackgroundClick.Should().BeFalse();
		modal.Title.Should().BeEmpty();
	}

	/// <summary>The parameters flow through to the wrapped modal's markup.</summary>
	[Fact]
	public void Parameters_reach_the_wrapped_modal()
	{
		var hidden = new List<string>();
		var component = Render<TestModal>(parameters => parameters
			.Add(p => p.Title, "Hello")
			.Add(p => p.ShowClose, false)
			.Add(p => p.CenterVertically, true)
			.Add(p => p.HideOnBackgroundClick, true)
			.Add(p => p.CloseOnEscape, false)
			.Add(p => p.Size, ModalSizes.Small)
			.Add(p => p.ModalHidden, (string s) => hidden.Add(s))
			.Add(p => p.Buttons, [new ToolbarButton { Key = "ok", Text = "OK" }]));

		component.Find(".modal-title").TextContent.Should().Be("Hello");
		component.FindAll(".btn-close").Should().BeEmpty();
		component.Markup.Should().Contain("OK");
		component.Instance.ModalHidden.HasDelegate.Should().BeTrue();
		component.Instance.HideOnBackgroundClick.Should().BeTrue();
	}

	/// <summary>ShowAsync, with or without a token, asks the modal's JavaScript object to show.</summary>
	[Fact]
	public async Task ShowAsync_shows_the_modal()
	{
		var component = Render<TestModal>();

		await component.InvokeAsync(() => component.Instance.ShowAsync());
		await component.InvokeAsync(() => component.Instance.ShowAsync(CancellationToken.None));

		_modalObject.Invocations["show"].Should().HaveCount(2);
	}

	/// <summary>HideAsync, with or without a token, asks the modal's JavaScript object to hide.</summary>
	[Fact]
	public async Task HideAsync_hides_the_modal()
	{
		var component = Render<TestModal>();

		await component.InvokeAsync(() => component.Instance.HideAsync());
		await component.InvokeAsync(() => component.Instance.HideAsync(CancellationToken.None));

		_modalObject.Invocations["hide"].Should().HaveCount(2);
	}

	/// <summary>ShowAndWaitResultAsync shows the modal and completes with the key of the button clicked.</summary>
	[Fact]
	public async Task ShowAndWaitResultAsync_returns_the_clicked_button()
	{
		var component = Render<TestModal>();

		Task<string> result = Task.FromResult(string.Empty);
		await component.InvokeAsync(() => { result = component.Instance.ShowAndWaitResultAsync(); });
		component.WaitForAssertion(() => _modalObject.Invocations["show"].Should().ContainSingle());

		var noButton = component.FindAll(".modal-footer button").Single(b => b.TextContent.Contains("No", StringComparison.Ordinal));
		await noButton.ClickAsync(new());

		(await result).Should().Be(ModalResults.NO);
	}

	/// <summary>The token overload of ShowAndWaitResultAsync also completes with the clicked button.</summary>
	[Fact]
	public async Task ShowAndWaitResultAsync_with_token_returns_the_clicked_button()
	{
		var component = Render<TestModal>();

		Task<string> result = Task.FromResult(string.Empty);
		await component.InvokeAsync(() => { result = component.Instance.ShowAndWaitResultAsync(CancellationToken.None); });
		component.WaitForAssertion(() => _modalObject.Invocations["show"].Should().ContainSingle());
		var yesButton = component.FindAll(".modal-footer button").Single(b => b.TextContent.Contains("Yes", StringComparison.Ordinal));
		await yesButton.ClickAsync(new());

		(await result).Should().Be(ModalResults.YES);
	}

	/// <summary>A minimal concrete modal that renders a <see cref="PDModal"/> from the base class parameters.</summary>
	private sealed class TestModal : PDModalBase
	{
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<PDModal>(0);
			builder.AddComponentParameter(1, nameof(PDModal.Buttons), Buttons);
			builder.AddComponentParameter(2, nameof(PDModal.CenterVertically), CenterVertically);
			builder.AddComponentParameter(3, nameof(PDModal.CloseOnEscape), CloseOnEscape);
			builder.AddComponentParameter(4, nameof(PDModal.HideOnBackgroundClick), HideOnBackgroundClick);
			builder.AddComponentParameter(5, nameof(PDModal.ShowClose), ShowClose);
			builder.AddComponentParameter(6, nameof(PDModal.Size), Size);
			builder.AddComponentParameter(7, nameof(PDModal.Title), Title);
			builder.AddComponentReferenceCapture(8, reference => Modal = (PDModal)reference);
			builder.CloseComponent();
		}
	}
}
