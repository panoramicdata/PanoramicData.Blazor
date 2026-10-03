using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Models.ColorPicker;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for <see cref="PDColorPicker"/>.
/// </summary>
/// <remarks>
/// Issue #99. The component takes a colour in and hands one back through a callback, and the
/// defect was entirely in what it handed back - so these assert on the values the caller
/// receives rather than on the markup.
/// </remarks>
public partial class PDColorPickerTests : BunitContext
{
	/// <summary>
	/// Sets up the rendering context.
	/// </summary>
	public PDColorPickerTests()
		// The picker imports a JavaScript module for pointer tracking. Loose mode returns a
		// stub for it, which is all these tests need: none of them drag anything.
		=> JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Renders a picker and records every value it reports back.
	/// </summary>
	private (IRenderedComponent<PDColorPicker> Component, List<string> Reported) Render(string initial)
	{
		var reported = new List<string>();
		var component = base.Render<PDColorPicker>(parameters => parameters
			.Add(p => p.Value, initial)
			.Add(p => p.ValueChanged, value => reported.Add(value)));

		return (component, reported);
	}

	private static Task OpenAsync(IRenderedComponent<PDColorPicker> component)
		=> component.Find(".pd-color-picker-button").ClickAsync(new MouseEventArgs());

	/// <summary>
	/// Changes the red channel through its input, which is one of the paths that reports an
	/// intermediate value while the popup is still open.
	/// </summary>
	private static Task SetRedAsync(IRenderedComponent<PDColorPicker> component, string value)
		=> component.FindAll(".pd-color-input")[0].ChangeAsync(new ChangeEventArgs { Value = value });

	private const string ModulePath = "./_content/PanoramicData.Blazor/PDColorPicker.razor.js";

	/// <summary>
	/// Renders a picker with the given options and extra parameters, recording every reported value.
	/// </summary>
	private (IRenderedComponent<PDColorPicker> Component, List<string> Reported) RenderPicker(
		string initial,
		ColorPickerOptions? options = null,
		Action<ComponentParameterCollectionBuilder<PDColorPicker>>? more = null)
	{
		var reported = new List<string>();
		var component = base.Render<PDColorPicker>(parameters =>
		{
			parameters
				.Add(p => p.Value, initial)
				.Add(p => p.Options, options ?? new ColorPickerOptions())
				.Add(p => p.ValueChanged, value => reported.Add(value));
			more?.Invoke(parameters);
		});

		return (component, reported);
	}

	private static AngleSharp.Dom.IElement PopupButton(IRenderedComponent<PDColorPicker> component, string text)
		=> component.FindAll(".pd-color-picker-popup button").Single(b => b.TextContent.Trim() == text);

	/// <summary>
	/// Verifies that the trigger button carries its text, size, tooltip and toolbar placement.
	/// </summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "btn-sm")]
	[InlineData(ButtonSizes.Large, "btn-lg")]
	public void Button_CarriesItsTextSizeTooltipAndPlacement(ButtonSizes size, string sizeClass)
	{
		var (component, _) = RenderPicker("#112233", more: p => p
			.Add(x => x.Text, "Fill")
			.Add(x => x.TextCssClass, "fill-text")
			.Add(x => x.Size, size)
			.Add(x => x.ToolTip, "Pick a fill")
			.Add(x => x.ShiftRight, true)
			.Add(x => x.ItemCssClass, "my-item"));

		var button = component.Find(".pd-color-picker-button");
		button.ClassList.Should().Contain(sizeClass).And.Contain("btn-secondary");
		button.GetAttribute("title").Should().Be("Pick a fill");
		component.Find(".pd-color-text").ClassList.Should().Contain("fill-text");
		component.Find(".pd-color-text").TextContent.Should().Be("Fill");
		component.Find(".pdtoolbaritem").ClassList.Should().Contain("align-right").And.Contain("my-item");
		component.Find(".pd-color-swatch-color").GetAttribute("style").Should().Contain("#112233");
	}

	/// <summary>
	/// Verifies that a hidden or disabled picker says so on its toolbar item and button.
	/// </summary>
	[Fact]
	public void HiddenAndDisabled_AreReflected()
	{
		var (component, _) = RenderPicker("#112233", more: p => p
			.Add(x => x.IsVisible, false)
			.Add(x => x.IsEnabled, false));

		component.Find(".pdtoolbaritem").ClassList.Should().Contain("pd-hidden");
		component.Find(".pd-color-picker-button").HasAttribute("disabled").Should().BeTrue();
		component.FindAll(".pd-color-text").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the swatch shows a translucent colour as rgba, and an empty value as transparent.
	/// </summary>
	[Theory]
	[InlineData("#FF000080", "rgba(255, 0, 0,")]
	[InlineData("", "transparent")]
	public void Swatch_ShowsTranslucentAndEmptyValues(string value, string expected)
	{
		var (component, _) = RenderPicker(value);

		component.Find(".pd-color-swatch-color").GetAttribute("style").Should().Contain(expected);
	}

	/// <summary>
	/// Verifies that the trigger opens the popup, and a second press closes it and releases the outside-click handler.
	/// </summary>
	[Fact]
	public async Task Trigger_OpensAndClosesThePopup()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var (component, _) = RenderPicker("#112233");

		await OpenAsync(component);
		component.FindAll(".pd-color-picker-popup").Should().ContainSingle();
		component.Find(".pd-color-picker-button i").ClassList.Should().Contain("fa-angle-up");
		module.VerifyInvoke("initialize").Arguments[0].Should().Be(component.Instance.Id);

		await OpenAsync(component);
		component.FindAll(".pd-color-picker-popup").Should().BeEmpty();
		module.VerifyInvoke("dispose");
	}

	/// <summary>
	/// Verifies that a picker that does not close on outside clicks never registers the handler.
	/// </summary>
	[Fact]
	public async Task WithoutCloseOnOutsideClick_NoHandlerIsRegistered()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var (component, _) = RenderPicker("#112233", new ColorPickerOptions { CloseOnOutsideClick = false });

		await OpenAsync(component);

		module.Invocations["initialize"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a click outside the picker, reported by the script, closes an open popup.
	/// </summary>
	[Fact]
	public async Task OutsideClick_ClosesThePopup()
	{
		var (component, reported) = RenderPicker("#112233");
		await OpenAsync(component);

		await component.InvokeAsync(component.Instance.OnOutsideClick);
		await component.InvokeAsync(component.Instance.OnOutsideClick);

		component.FindAll(".pd-color-picker-popup").Should().BeEmpty();
		reported.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that disposing the picker releases its outside-click handler.
	/// </summary>
	[Fact]
	public async Task Dispose_ReleasesTheOutsideClickHandler()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var (component, _) = RenderPicker("#000000");
		var id = component.Instance.Id;

		await DisposeComponentsAsync();

		module.VerifyInvoke("dispose").Arguments[0].Should().Be(id);
	}
}
