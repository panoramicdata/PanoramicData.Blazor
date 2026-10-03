using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models.ColorPicker;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Pointer, slider and input control tests for <see cref="PDColorPicker"/>.
/// </summary>
public partial class PDColorPickerTests
{
	/// <summary>
	/// Verifies that pressing and dragging in the saturation/value square sets those channels from the pointer.
	/// </summary>
	[Fact]
	public async Task SaturationValueSquare_SetsTheColourFromThePointer()
	{
		var (component, reported) = RenderPicker("#FF0000");
		await OpenAsync(component);

		// The square falls back to the popup width less its padding (256) and the selector height (150).
		await component.Find(".pd-color-sv-gradient").PointerDownAsync(new PointerEventArgs { OffsetX = 128, OffsetY = 75 });
		await component.Find(".pd-color-sv-gradient").PointerMoveAsync(new PointerEventArgs { OffsetX = 256, OffsetY = 0 });
		await component.Find(".pd-color-sv-gradient").PointerUpAsync(new PointerEventArgs());
		await component.Find(".pd-color-sv-gradient").PointerMoveAsync(new PointerEventArgs { OffsetX = 0, OffsetY = 150 });

		reported.Should().Equal(ColorValue.FromHsv(0, 0.5, 0.5).ToHex(), "#FF0000");
	}

	/// <summary>
	/// Verifies that the saturation/value square uses the size the script measured, rather than the configured one.
	/// </summary>
	[Fact]
	public async Task SaturationValueSquare_UsesTheMeasuredSize()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module
			.Setup<PDColorPicker.ElementBounds?>("getElementBounds", _ => true)
			.SetResult(new PDColorPicker.ElementBounds(512, 300, 10, 20));
		var (component, reported) = RenderPicker("#FF0000");
		await OpenAsync(component);

		// Halfway across and down the measured 512 x 300 square, not the configured 256 x 150 one.
		await component.Find(".pd-color-sv-gradient").PointerDownAsync(new PointerEventArgs { OffsetX = 256, OffsetY = 150 });

		reported.Should().Equal(ColorValue.FromHsv(0, 0.5, 0.5).ToHex());
	}

	/// <summary>
	/// Verifies that the hue strip sets the hue from the pointer, and only while pressed.
	/// </summary>
	[Fact]
	public async Task HueStrip_SetsTheHueFromThePointer()
	{
		var (component, reported) = RenderPicker("#FF0000");
		await OpenAsync(component);

		// The strip is the popup width less 20 pixels wide, so its middle is a hue of 180.
		await component.Find(".pd-color-hue-strip").PointerMoveAsync(new PointerEventArgs { OffsetX = 10 });
		await component.Find(".pd-color-hue-strip").PointerDownAsync(new PointerEventArgs { OffsetX = 130 });
		await component.Find(".pd-color-hue-strip").PointerMoveAsync(new PointerEventArgs { OffsetX = 0 });
		await component.Find(".pd-color-hue-strip").PointerUpAsync(new PointerEventArgs());
		await component.Find(".pd-color-hue-strip").PointerMoveAsync(new PointerEventArgs { OffsetX = 130 });

		reported.Should().Equal("#00FFFF", "#FF0000");
	}

	/// <summary>
	/// Verifies that the alpha strip sets the opacity from the pointer, and only while pressed.
	/// </summary>
	[Fact]
	public async Task AlphaStrip_SetsTheOpacityFromThePointer()
	{
		var (component, reported) = RenderPicker("#FF0000");
		await OpenAsync(component);

		await component.Find(".pd-color-alpha-strip").PointerDownAsync(new PointerEventArgs { OffsetX = 130 });
		await component.Find(".pd-color-alpha-strip").PointerMoveAsync(new PointerEventArgs { OffsetX = 260 });
		await component.Find(".pd-color-alpha-strip").PointerUpAsync(new PointerEventArgs());
		await component.Find(".pd-color-alpha-strip").PointerMoveAsync(new PointerEventArgs { OffsetX = 0 });

		reported.Should().Equal(new ColorValue(255, 0, 0, 0.5).ToRgba(), "#FF0000");
	}

	/// <summary>
	/// Verifies that the alpha strip and alpha input are left out when transparency is not allowed.
	/// </summary>
	[Fact]
	public async Task WithoutTransparency_ThereIsNoAlphaControl()
	{
		var (component, _) = RenderPicker("#FF0000", new ColorPickerOptions { AllowTransparency = false });
		await OpenAsync(component);

		component.FindAll(".pd-color-alpha-strip").Should().BeEmpty();
		component.FindAll(".pd-color-input").Should().HaveCount(3);
	}

	/// <summary>
	/// Verifies that the RGB sliders each set their own channel, and ignore values that are not bytes.
	/// </summary>
	[Fact]
	public async Task RgbSliders_SetTheirChannels()
	{
		var (component, reported) = RenderPicker("#000000", new ColorPickerOptions { EnabledSelectors = ColorSpaceSelector.RGBSliders });
		await OpenAsync(component);

		await component.Find(".pd-color-slider-red").InputAsync(new ChangeEventArgs { Value = "16" });
		await component.Find(".pd-color-slider-green").InputAsync(new ChangeEventArgs { Value = "32" });
		await component.Find(".pd-color-slider-blue").InputAsync(new ChangeEventArgs { Value = "48" });
		await component.Find(".pd-color-slider-red").InputAsync(new ChangeEventArgs { Value = "300" });

		reported.Should().Equal("#100000", "#102000", "#102030");
		component.FindAll(".pd-color-sv-container").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the HSV sliders each set their own channel, and ignore values that are not numbers.
	/// </summary>
	[Fact]
	public async Task HsvSliders_SetTheirChannels()
	{
		var (component, reported) = RenderPicker("#FF0000", new ColorPickerOptions { EnabledSelectors = ColorSpaceSelector.HSVSliders });
		await OpenAsync(component);

		await component.Find(".pd-color-slider-hue").InputAsync(new ChangeEventArgs { Value = "120" });
		await component.Find(".pd-color-slider-saturation").InputAsync(new ChangeEventArgs { Value = "0" });
		await component.Find(".pd-color-slider-brightness").InputAsync(new ChangeEventArgs { Value = "0" });
		await component.Find(".pd-color-slider-hue").InputAsync(new ChangeEventArgs { Value = "green" });

		reported.Should().Equal("#00FF00", "#FFFFFF", "#000000");
	}

	/// <summary>
	/// Verifies that the RGB inputs set the green, blue and alpha channels, and ignore invalid entries.
	/// </summary>
	[Fact]
	public async Task RgbInputs_SetTheirChannels()
	{
		var (component, reported) = RenderPicker("#000000");
		await OpenAsync(component);

		await component.FindAll(".pd-color-input")[1].ChangeAsync(new ChangeEventArgs { Value = "32" });
		await component.FindAll(".pd-color-input")[2].ChangeAsync(new ChangeEventArgs { Value = "48" });
		await component.FindAll(".pd-color-input")[2].ChangeAsync(new ChangeEventArgs { Value = "-1" });
		await component.FindAll(".pd-color-input")[3].ChangeAsync(new ChangeEventArgs { Value = "50" });
		await component.FindAll(".pd-color-input")[3].ChangeAsync(new ChangeEventArgs { Value = "half" });

		reported.Should().Equal("#002000", "#002030", new ColorValue(0, 32, 48, 0.5).ToRgba());
	}

	/// <summary>
	/// Verifies that the mode toggle moves from RGB to HSV inputs, whose channels can then be set.
	/// </summary>
	[Fact]
	public async Task HsvInputs_SetTheirChannels()
	{
		var (component, reported) = RenderPicker("#FF0000");
		await OpenAsync(component);

		await component.Find(".pd-color-mode-toggle").ClickAsync(new MouseEventArgs());
		component.FindAll(".pd-color-input")[0].GetAttribute("max").Should().Be("360");
		await component.FindAll(".pd-color-input")[0].ChangeAsync(new ChangeEventArgs { Value = "240" });
		await component.FindAll(".pd-color-input")[1].ChangeAsync(new ChangeEventArgs { Value = "0" });
		await component.FindAll(".pd-color-input")[2].ChangeAsync(new ChangeEventArgs { Value = "50" });
		await component.FindAll(".pd-color-input")[0].ChangeAsync(new ChangeEventArgs { Value = "blue" });
		await component.FindAll(".pd-color-input")[3].ChangeAsync(new ChangeEventArgs { Value = "50" });

		reported.Should().HaveCount(4);
		reported[0].Should().Be("#0000FF");
		reported[1].Should().Be("#FFFFFF");
		reported[2].Should().Be(ColorValue.FromHsv(240, 0, 0.5).ToHex());
		reported[3].Should().Contain("rgba(");
	}

	/// <summary>
	/// Verifies that the toggle reaches the hex input, whose entry sets the whole colour, and then wraps back to RGB.
	/// </summary>
	[Fact]
	public async Task HexInput_SetsTheColour_AndTheToggleWraps()
	{
		var (component, reported) = RenderPicker("#FF0000");
		await OpenAsync(component);

		await component.Find(".pd-color-mode-toggle").ClickAsync(new MouseEventArgs());
		await component.Find(".pd-color-mode-toggle").ClickAsync(new MouseEventArgs());
		var hex = component.Find(".pd-color-input-hex");
		hex.GetAttribute("value").Should().Be("#FF0000");
		hex.GetAttribute("maxlength").Should().Be("9");
		await hex.ChangeAsync(new ChangeEventArgs { Value = "#123456" });
		await component.Find(".pd-color-mode-toggle").ClickAsync(new MouseEventArgs());

		reported.Should().Equal("#123456");
		component.FindAll(".pd-color-input-hex").Should().BeEmpty();
		component.FindAll(".pd-color-input")[0].GetAttribute("max").Should().Be("255");
	}

	/// <summary>
	/// Verifies that a translucent colour shows its alpha in the hex input.
	/// </summary>
	[Fact]
	public async Task HexInput_ShowsAlphaForATranslucentColour()
	{
		var (component, _) = RenderPicker("#FF000080", new ColorPickerOptions { EnabledModes = ColorMode.Hex });
		await OpenAsync(component);
		await component.Find(".pd-color-mode-toggle").ClickAsync(new MouseEventArgs());

		component.Find(".pd-color-input-hex").GetAttribute("value").Should().Be("#FF000080");
	}

	/// <summary>
	/// Verifies that with only the hex mode enabled the RGB inputs are not offered, and the toggle lands on hex.
	/// </summary>
	[Fact]
	public async Task OnlyHexMode_TogglesStraightToHex()
	{
		var (component, _) = RenderPicker("#FF0000", new ColorPickerOptions { EnabledModes = ColorMode.Hex, AllowTransparency = false });
		await OpenAsync(component);

		component.FindAll(".pd-color-input").Should().BeEmpty();
		await component.Find(".pd-color-mode-toggle").ClickAsync(new MouseEventArgs());
		component.Find(".pd-color-input-hex").GetAttribute("maxlength").Should().Be("7");
	}

	/// <summary>
	/// Verifies that with no input modes enabled the toggle does nothing.
	/// </summary>
	[Fact]
	public async Task NoModes_TheToggleDoesNothing()
	{
		var (component, _) = RenderPicker("#FF0000", new ColorPickerOptions { EnabledModes = ColorMode.None });
		await OpenAsync(component);

		await component.Find(".pd-color-mode-toggle").ClickAsync(new MouseEventArgs());

		component.FindAll(".pd-color-input").Should().BeEmpty();
	}
}
