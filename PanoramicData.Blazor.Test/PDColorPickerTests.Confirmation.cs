using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models.ColorPicker;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Confirming, cancelling, previewing and parameter tests for <see cref="PDColorPicker"/>.
/// </summary>
public partial class PDColorPickerTests
{
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
}
