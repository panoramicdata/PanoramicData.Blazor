using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models.ColorPicker;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Palette and recent colour tests for <see cref="PDColorPicker"/>.
/// </summary>
public partial class PDColorPickerTests
{
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
}
