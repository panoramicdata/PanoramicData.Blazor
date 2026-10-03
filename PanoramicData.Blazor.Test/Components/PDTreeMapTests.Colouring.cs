using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Colouring tests for <see cref="PDTreeMap{TItem}"/>.
/// </summary>
public partial class PDTreeMapTests
{
	/// <summary>
	/// Verifies that an explicit colour selector overrides the colour mode, and a blank result falls back
	/// to it.
	/// </summary>
	[Fact]
	public void An_explicit_colour_overrides_the_colour_mode()
	{
		var component = RenderMap(p => p
			.Add(x => x.ColourSelector, n => n == _b ? "red" : " ")
			.Add(x => x.ColourMode, TreeMapColourMode.Custom));

		FillOf(component, _b).Should().Be("red");
		FillOf(component, _c).Should().Be(TreeMapPalette.Fallback());
	}

	/// <summary>
	/// Verifies that category colouring uses the palette colour for each item's category.
	/// </summary>
	[Fact]
	public void Category_colouring_uses_the_category_palette()
	{
		var component = RenderMap(p => p.Add(x => x.CategorySelector, n => n.Name[..1]));

		FillOf(component, _a1).Should().Be(TreeMapPalette.ForCategory("A"));
		FillOf(component, _b).Should().Be(TreeMapPalette.ForCategory("B"));
	}

	/// <summary>
	/// Verifies that depth colouring shades by each rectangle's depth.
	/// </summary>
	[Fact]
	public void Depth_colouring_shades_by_depth()
	{
		var component = RenderMap(p => p.Add(x => x.ColourMode, TreeMapColourMode.Depth));

		FillOf(component, _a).Should().Be(TreeMapPalette.ForDepth(0, 2));
		FillOf(component, _a1).Should().Be(TreeMapPalette.ForDepth(1, 2));
	}

	/// <summary>
	/// Verifies that heat colouring scales across the laid out values, ignoring values that are not
	/// numbers.
	/// </summary>
	[Fact]
	public void Heat_colouring_scales_across_the_values()
	{
		var heat = new Dictionary<string, double>
		{
			["A"] = double.NaN,
			["A1"] = 10,
			["A2"] = 20,
			["B"] = double.PositiveInfinity,
			["C"] = 50
		};

		var component = RenderMap(p => p
			.Add(x => x.ColourMode, TreeMapColourMode.Heat)
			.Add(x => x.HeatSelector, n => heat[n.Name]));

		FillOf(component, _a1).Should().Be(TreeMapPalette.ForHeat(10, 10, 50));
		FillOf(component, _c).Should().Be(TreeMapPalette.ForHeat(50, 10, 50));
	}

	/// <summary>
	/// Verifies that heat colouring with no heat selector, or with no usable values, still colours.
	/// </summary>
	[Fact]
	public void Heat_colouring_without_usable_values_falls_back()
	{
		var withoutSelector = RenderMap(p => p.Add(x => x.ColourMode, TreeMapColourMode.Heat));
		FillOf(withoutSelector, _b).Should().Be(TreeMapPalette.Fallback());

		var allNaN = RenderMap(p => p
			.Add(x => x.ColourMode, TreeMapColourMode.Heat)
			.Add(x => x.HeatSelector, _ => double.NaN));
		FillOf(allNaN, _b).Should().Be(TreeMapPalette.ForHeat(double.NaN, 0, 0));
	}

	/// <summary>
	/// Verifies that a throwing colour selector is reported and the fallback colour used.
	/// </summary>
	[Fact]
	public void A_throwing_colour_selector_is_reported_and_falls_back()
	{
		var component = RenderMap(p => p.Add(x => x.ColourSelector, _ => throw new InvalidOperationException("colour")));

		FillOf(component, _b).Should().Be(TreeMapPalette.Fallback());
		_errors.Should().NotBeEmpty().And.OnlyContain(e => e.Message == "colour");
	}
}
