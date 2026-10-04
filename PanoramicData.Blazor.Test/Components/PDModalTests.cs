using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDModal"/> renders its dialog, initialises its Bootstrap modal with the chosen
/// options, forwards show and hide, resolves a waiting caller with the button chosen, and hides itself on
/// navigation.
/// </summary>
public partial class PDModalTests : BunitContext
{
	/// <summary>
	/// How long to wait for a render that another thread or a timer brings about. Generous because a busy
	/// machine (the whole suite under coverage) can hold the renderer's dispatcher well past bUnit's default.
	/// </summary>
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	private const string ModulePath = "./_content/PanoramicData.Blazor/PDModal.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDModalTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>Verifies that the dialog renders its title, body and the default Yes and No buttons.</summary>
	[Fact]
	public void The_dialog_renders_title_body_and_default_buttons()
	{
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Id, "dialog")
			.Add(p => p.Title, "Confirm")
			.Add(p => p.CssClass, "fade")
			.Add(p => p.HeaderCssClass, "bg-light")
			.Add(p => p.BodyCssClass, "p-4")
			.AddChildContent("<p>Are you sure?</p>"));

		component.Find(".modal").Id.Should().Be("dialog");
		component.Find(".modal").ClassList.Should().Contain("fade");
		component.Find(".modal-header.bg-light .modal-title").TextContent.Should().Be("Confirm");
		component.Find(".modal-body.p-4 p").TextContent.Should().Be("Are you sure?");
		component.FindAll(".btn-close").Should().BeEmpty();
		FooterButtonTexts(component).Should().Equal("Yes", "No");
	}

	/// <summary>Verifies that the close button is shown on request and hides the dialog.</summary>
	[Fact]
	public async Task The_close_button_hides_the_dialog()
	{
		var modal = SetupModalObject();
		var component = Render<PDModal>(parameters => parameters.Add(p => p.ShowClose, true));

		await component.Find(".btn-close").ClickAsync(new MouseEventArgs());

		modal.VerifyInvoke("hide");
	}

	/// <summary>Verifies that header and footer templates replace the defaults.</summary>
	[Fact]
	public void Header_and_footer_templates_replace_the_defaults()
	{
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Title, "Unused")
			.Add(p => p.Header, (RenderFragment)(b => b.AddMarkupContent(0, "<h2>Custom</h2>")))
			.Add(p => p.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<span class=\"custom-footer\">Done</span>"))));

		component.Find(".modal-header h2").TextContent.Should().Be("Custom");
		component.FindAll(".modal-title").Should().BeEmpty();
		component.Find(".modal-footer .custom-footer").TextContent.Should().Be("Done");
		component.FindAll(".modal-footer button").Should().BeEmpty();
	}

	/// <summary>Verifies that the footer can be left out entirely.</summary>
	[Fact]
	public void The_footer_can_be_left_out()
	{
		var component = Render<PDModal>(parameters => parameters.Add(p => p.ShowFooter, false));

		component.FindAll(".modal-footer").Should().BeEmpty();
	}

	/// <summary>Verifies that each size maps to its Bootstrap dialog class, and centring adds its class.</summary>
	[Theory]
	[InlineData(ModalSizes.Small, false, "modal-dialog modal-sm")]
	[InlineData(ModalSizes.Medium, false, "modal-dialog")]
	[InlineData(ModalSizes.Large, false, "modal-dialog modal-lg")]
	[InlineData(ModalSizes.ExtraLarge, true, "modal-dialog modal-xl modal-dialog-centered")]
	[InlineData(ModalSizes.Medium, true, "modal-dialog modal-dialog-centered")]
	public void The_size_and_centring_map_to_dialog_classes(ModalSizes size, bool centred, string expected)
	{
		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Size, size)
			.Add(p => p.CenterVertically, centred));

		component.Find(".modal-dialog").ClassName!.Trim().Should().Be(expected);
	}

	/// <summary>Verifies that the Bootstrap modal is initialised with the id, backdrop, keyboard and callback reference.</summary>
	[Theory]
	[InlineData(false, true, "static", true)]
	[InlineData(true, false, true, false)]
	public void The_modal_is_initialised_with_its_options(bool hideOnBackgroundClick, bool closeOnEscape, object backdrop, bool keyboard)
	{
		var module = JSInterop.SetupModule(ModulePath);

		var component = Render<PDModal>(parameters => parameters
			.Add(p => p.Id, "dialog")
			.Add(p => p.HideOnBackgroundClick, hideOnBackgroundClick)
			.Add(p => p.CloseOnEscape, closeOnEscape));

		var init = module.VerifyInvoke("initialize");
		init.Arguments[0].Should().Be("dialog");
		var options = init.Arguments[1]!;
		options.GetType().GetProperty("backdrop")!.GetValue(options).Should().Be(backdrop);
		options.GetType().GetProperty("keyboard")!.GetValue(options).Should().Be(keyboard);
		options.GetType().GetProperty("focus")!.GetValue(options).Should().Be(true);
		init.Arguments[2].Should().BeOfType<DotNetObjectReference<PDModal>>().Which.Value.Should().BeSameAs(component.Instance);
	}

	private BunitJSModuleInterop SetupModalObject()
		=> JSInterop.SetupModule(ModulePath).SetupModule("initialize", _ => true);

	private static AngleSharp.Dom.IElement FooterButton(IRenderedComponent<PDModal> component, string text)
		=> component.FindAll(".modal-footer button").Single(b => b.TextContent.Trim() == text);

	private static List<string> FooterButtonTexts(IRenderedComponent<PDModal> component)
		=> [.. component.FindAll(".modal-footer button").Select(b => b.TextContent.Trim())];
}
