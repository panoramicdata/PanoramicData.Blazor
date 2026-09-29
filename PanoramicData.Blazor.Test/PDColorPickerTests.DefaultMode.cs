using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.ColorPicker;
using Xunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for the input mode <see cref="PDColorPicker"/> opens in, set by <see cref="ColorPickerOptions.DefaultMode"/> (#177).
/// </summary>
public partial class PDColorPickerTests
{
	/// <summary>
	/// Verifies that an explicitly chosen hex default mode opens the hex input rather than the RGB inputs.
	/// </summary>
	[Fact]
	public async Task DefaultMode_Hex_OpensTheHexInput()
	{
		var (component, _) = RenderPicker("#FF0000", new ColorPickerOptions { DefaultMode = ColorMode.Hex });
		await OpenAsync(component);

		component.Find(".pd-color-input-hex").GetAttribute("value").Should().Be("#FF0000");
		component.FindAll(".pd-color-input-row-rgb").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that an explicitly chosen HSV (or HSL) default mode opens the HSV inputs.
	/// </summary>
	[Theory]
	[InlineData(ColorMode.HSV)]
	[InlineData(ColorMode.HSL)]
	public async Task DefaultMode_Hsv_OpensTheHsvInputs(ColorMode mode)
	{
		var (component, _) = RenderPicker("#FF0000", new ColorPickerOptions { DefaultMode = mode });
		await OpenAsync(component);

		component.FindAll(".pd-color-input")[0].GetAttribute("max").Should().Be("360");
	}

	/// <summary>
	/// Verifies that an explicitly chosen RGB (or RGBA) default mode opens the RGB inputs.
	/// </summary>
	[Theory]
	[InlineData(ColorMode.RGB)]
	[InlineData(ColorMode.RGBA)]
	public async Task DefaultMode_Rgb_OpensTheRgbInputs(ColorMode mode)
	{
		var (component, _) = RenderPicker("#FF0000", new ColorPickerOptions { DefaultMode = mode });
		await OpenAsync(component);

		component.FindAll(".pd-color-input")[0].GetAttribute("max").Should().Be("255");
	}

	/// <summary>
	/// Verifies that when DefaultMode is left unset the picker keeps opening in RGB, as it always has, even though
	/// the property reports its documented default of Hex.
	/// </summary>
	[Fact]
	public async Task DefaultMode_NotSet_StillOpensTheRgbInputs()
	{
		var options = new ColorPickerOptions();
		var (component, _) = RenderPicker("#FF0000", options);
		await OpenAsync(component);

		options.DefaultMode.Should().Be(ColorMode.Hex);
		component.FindAll(".pd-color-input")[0].GetAttribute("max").Should().Be("255");
		component.FindAll(".pd-color-input-hex").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a default mode that is not a single input mode (a combination) falls back to RGB.
	/// </summary>
	[Fact]
	public async Task DefaultMode_Combination_FallsBackToRgb()
	{
		var (component, _) = RenderPicker("#FF0000", new ColorPickerOptions { DefaultMode = ColorMode.RGB | ColorMode.Hex });
		await OpenAsync(component);

		component.FindAll(".pd-color-input")[0].GetAttribute("max").Should().Be("255");
	}
}
