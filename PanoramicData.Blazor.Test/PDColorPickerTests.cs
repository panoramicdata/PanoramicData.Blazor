using Bunit;
using Microsoft.Extensions.DependencyInjection;
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
public class PDColorPickerTests : BunitContext
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

	private static void Open(IRenderedComponent<PDColorPicker> component)
		=> component.Find(".pd-color-picker-button").Click();

	/// <summary>
	/// Changes the red channel through its input, which is one of the paths that reports an
	/// intermediate value while the popup is still open.
	/// </summary>
	private static void SetRed(IRenderedComponent<PDColorPicker> component, string value)
		=> component.FindAll(".pd-color-input")[0].Change(value);

	/// <summary>
	/// Cancelling puts back the colour the caller started with.
	/// </summary>
	[Fact]
	public void Cancel_RestoresTheColourTheCallerStartedWith()
	{
		// Issue #99: live preview reports each intermediate value, and cancelling used to
		// restore the component's own state without telling the caller - so the caller kept the
		// colour that was being moved towards. For a caller that renders on every change, that
		// meant cancelling kept the abandoned colour.
		var (component, reported) = Render("#000000");

		Open(component);
		SetRed(component, "200");

		reported.ShouldNotBeEmpty("live preview reports intermediate values while the popup is open");

		component.FindAll(".pd-color-picker-popup button")
			.Single(b => b.TextContent.Contains("Cancel", StringComparison.OrdinalIgnoreCase))
			.Click();

		reported[^1].ShouldBe("#000000");
	}

	/// <summary>
	/// Confirming keeps the colour that was chosen.
	/// </summary>
	[Fact]
	public void Ok_KeepsTheChosenColour()
	{
		// The complement of the test above: confirming has to keep the change, or "restore on
		// cancel" could be satisfied by never reporting anything at all.
		var (component, reported) = Render("#000000");

		Open(component);
		SetRed(component, "200");

		component.FindAll(".pd-color-picker-popup button")
			.Single(b => b.TextContent.Contains("OK", StringComparison.OrdinalIgnoreCase))
			.Click();

		reported[^1].ShouldBe("#C80000");
	}

	/// <summary>
	/// With live preview off, cancelling reports nothing at all.
	/// </summary>
	[Fact]
	public void Cancel_WithoutLivePreview_ReportsNothing()
	{
		// With live preview off, nothing is reported until the choice is confirmed, so
		// cancelling has nothing to put back and must stay silent rather than reporting the
		// original as though it were a change.
		var reported = new List<string>();
		var component = Render<PDColorPicker>(parameters => parameters
			.Add(p => p.Value, "#000000")
			.Add(p => p.Options, new ColorPickerOptions { LivePreview = false })
			.Add(p => p.ValueChanged, value => reported.Add(value)));

		Open(component);
		SetRed(component, "200");

		component.FindAll(".pd-color-picker-popup button")
			.Single(b => b.TextContent.Contains("Cancel", StringComparison.OrdinalIgnoreCase))
			.Click();

		reported.ShouldBeEmpty();
	}
}
