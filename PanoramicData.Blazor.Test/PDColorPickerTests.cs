using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Models.ColorPicker;
using Shouldly;
using Xunit;

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

	/// <summary>
	/// Cancelling puts back the colour the caller started with.
	/// </summary>
	[Fact]
	public async Task Cancel_RestoresTheColourTheCallerStartedWith()
	{
		// Issue #99: live preview reports each intermediate value, and cancelling used to
		// restore the component's own state without telling the caller - so the caller kept the
		// colour that was being moved towards. For a caller that renders on every change, that
		// meant cancelling kept the abandoned colour.
		var (component, reported) = Render("#000000");

		await OpenAsync(component);
		await SetRedAsync(component, "200");

		reported.ShouldNotBeEmpty("live preview reports intermediate values while the popup is open");

		await component.FindAll(".pd-color-picker-popup button")
			.Single(b => b.TextContent.Contains("Cancel", StringComparison.OrdinalIgnoreCase))
		.ClickAsync(new MouseEventArgs());

		reported[^1].ShouldBe("#000000");
	}

	/// <summary>
	/// Confirming keeps the colour that was chosen.
	/// </summary>
	[Fact]
	public async Task Ok_KeepsTheChosenColour()
	{
		// The complement of the test above: confirming has to keep the change, or "restore on
		// cancel" could be satisfied by never reporting anything at all.
		var (component, reported) = Render("#000000");

		await OpenAsync(component);
		await SetRedAsync(component, "200");

		await component.FindAll(".pd-color-picker-popup button")
			.Single(b => b.TextContent.Contains("OK", StringComparison.OrdinalIgnoreCase))
		.ClickAsync(new MouseEventArgs());

		reported[^1].ShouldBe("#C80000");
	}

	/// <summary>
	/// With live preview off, cancelling reports nothing at all.
	/// </summary>
	[Fact]
	public async Task Cancel_WithoutLivePreview_ReportsNothing()
	{
		// With live preview off, nothing is reported until the choice is confirmed, so
		// cancelling has nothing to put back and must stay silent rather than reporting the
		// original as though it were a change.
		var reported = new List<string>();
		var component = Render<PDColorPicker>(parameters => parameters
			.Add(p => p.Value, "#000000")
			.Add(p => p.Options, new ColorPickerOptions { LivePreview = false })
			.Add(p => p.ValueChanged, value => reported.Add(value)));

		await OpenAsync(component);
		await SetRedAsync(component, "200");

		await component.FindAll(".pd-color-picker-popup button")
			.Single(b => b.TextContent.Contains("Cancel", StringComparison.OrdinalIgnoreCase))
		.ClickAsync(new MouseEventArgs());

		reported.ShouldBeEmpty();
	}

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

	/// <summary>
	/// Verifies that the preview shows the original and new colours, and that pressing the original reverts to it.
	/// </summary>
	[Fact]
	public async Task Preview_ShowsBothColours_AndReverts()
	{
		var (component, reported) = RenderPicker("#000000");
		await OpenAsync(component);
		await SetRedAsync(component, "255");

		component.Find(".pd-color-preview-new .pd-color-preview-color").GetAttribute("style").Should().Contain("#FF0000");
		await component.Find(".pd-color-preview-old").ClickAsync(new MouseEventArgs());

		reported.Should().Equal("#FF0000", "#000000");
		component.Find(".pd-color-preview-new .pd-color-preview-color").GetAttribute("style").Should().Contain("#000000");
	}

	/// <summary>
	/// Verifies that turning off the preview and inputs removes that row.
	/// </summary>
	[Fact]
	public async Task WithoutPreviewOrInputs_ThereIsNoPreviewRow()
	{
		var (component, _) = RenderPicker("#000000", new ColorPickerOptions { ShowPreview = false, ShowInputs = false });
		await OpenAsync(component);

		component.FindAll(".pd-color-preview-row").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the palette is drawn, marks the current colour, and picking a swatch reports it.
	/// </summary>
	[Fact]
	public async Task Palette_MarksTheCurrentColour_AndPickingReportsIt()
	{
		var palette = new List<PaletteColor> { new("#FF0000", "Red"), new("#00FF00") };
		var (component, reported) = RenderPicker("#FF0000", more: p => p.Add(x => x.Palette, palette));
		await OpenAsync(component);

		var swatches = component.FindAll(".pd-color-palette-swatch");
		swatches.Should().HaveCount(2);
		swatches[0].ClassList.Should().Contain("selected");
		swatches[0].GetAttribute("title").Should().Be("Red");
		swatches[1].GetAttribute("title").Should().Be("#00FF00");
		await swatches[1].ClickAsync(new MouseEventArgs());

		reported.Should().Equal("#00FF00");
		component.FindAll(".pd-color-picker-popup").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that with close-on-select and no buttons, picking a swatch confirms the choice and closes.
	/// </summary>
	[Fact]
	public async Task Palette_WithCloseOnSelect_ConfirmsAndCloses()
	{
		var selected = new List<string>();
		var (component, _) = RenderPicker("#FF0000",
			new ColorPickerOptions { CloseOnSelect = true, ShowButtons = false },
			p => p
				.Add(x => x.Palette, [new PaletteColor("#0000FF")])
				.Add(x => x.ColorSelected, value => selected.Add(value)));
		await OpenAsync(component);

		component.FindAll(".pd-color-buttons").Should().BeEmpty();
		await component.Find(".pd-color-palette-swatch").ClickAsync(new MouseEventArgs());

		selected.Should().Equal("#0000FF");
		component.FindAll(".pd-color-picker-popup").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that recent colours are offered up to the maximum, and picking one reports it.
	/// </summary>
	[Fact]
	public async Task RecentColours_AreOfferedUpToTheMaximum()
	{
		var recent = new List<string> { "#111111", "#222222", "#333333" };
		var (component, reported) = RenderPicker("#000000",
			new ColorPickerOptions { MaxRecentColors = 2 },
			p => p.Add(x => x.RecentColors, recent));
		await OpenAsync(component);

		var swatches = component.FindAll(".pd-color-recent-swatch");
		swatches.Select(s => s.GetAttribute("title")).Should().Equal("#111111", "#222222");
		await swatches[1].ClickAsync(new MouseEventArgs());

		reported.Should().Equal("#222222");
	}

	/// <summary>
	/// Verifies that confirming moves the chosen colour to the front of the recent colours, trims them and reports the list.
	/// </summary>
	[Fact]
	public async Task Ok_PutsTheColourAtTheFrontOfTheRecentColours()
	{
		var recent = new List<string> { "#111111", "#C80000", "#333333" };
		List<string>? changed = null;
		var (component, _) = RenderPicker("#000000",
			new ColorPickerOptions { MaxRecentColors = 2 },
			p => p
				.Add(x => x.RecentColors, recent)
				.Add(x => x.RecentColorsChanged, value => changed = value));
		await OpenAsync(component);
		await SetRedAsync(component, "200");

		await PopupButton(component, "OK").ClickAsync(new MouseEventArgs());

		recent.Should().Equal("#C80000", "#111111");
		changed.Should().BeSameAs(recent);
	}

	/// <summary>
	/// Verifies that confirming leaves the recent colours alone when they are not shown.
	/// </summary>
	[Fact]
	public async Task Ok_WithoutRecentColoursShown_LeavesThemAlone()
	{
		var recent = new List<string> { "#111111" };
		var selected = new List<string>();
		var (component, _) = RenderPicker("#000000",
			new ColorPickerOptions { ShowRecentColors = false },
			p => p
				.Add(x => x.RecentColors, recent)
				.Add(x => x.ColorSelected, value => selected.Add(value)));
		await OpenAsync(component);
		await SetRedAsync(component, "200");

		await PopupButton(component, "OK").ClickAsync(new MouseEventArgs());

		recent.Should().Equal("#111111");
		selected.Should().Equal("#C80000");
	}

	/// <summary>
	/// Verifies that choosing no colour reports transparent, selects it, and closes.
	/// </summary>
	[Fact]
	public async Task NoColour_ReportsTransparentAndCloses()
	{
		var selected = new List<string>();
		var (component, reported) = RenderPicker("#000000",
			new ColorPickerOptions { ShowNoColor = true, NoColorText = "None" },
			p => p.Add(x => x.ColorSelected, value => selected.Add(value)));
		await OpenAsync(component);

		component.Find(".pd-color-no-color").TextContent.Should().Contain("None");
		await component.Find(".pd-color-no-color").ClickAsync(new MouseEventArgs());

		reported.Should().Equal("transparent");
		selected.Should().Equal("transparent");
		component.FindAll(".pd-color-picker-popup").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a new value from the caller is taken up while closed, but does not disturb an open popup.
	/// </summary>
	[Fact]
	public async Task NewValue_IsTakenUpOnlyWhileClosed()
	{
		var (component, _) = RenderPicker("#000000");

		component.Render(p => p.Add(x => x.Value, "#00FF00"));
		component.Find(".pd-color-swatch-color").GetAttribute("style").Should().Contain("#00FF00");
		await OpenAsync(component);
		await SetRedAsync(component, "255");
		component.Render(p => p.Add(x => x.Value, "#0000FF"));

		component.FindAll(".pd-color-input")[0].GetAttribute("value").Should().Be("255");
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
