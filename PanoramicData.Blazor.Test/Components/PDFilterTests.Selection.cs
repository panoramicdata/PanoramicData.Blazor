using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFilter"/> builds its filter from clicked and typed values.
/// </summary>
public partial class PDFilterTests
{
	/// <summary>
	/// Verifies that clicking one value selects it for Equals, and a second switches to an In list,
	/// quoting values that contain whitespace.
	/// </summary>
	[Fact]
	public async Task Clicking_values_builds_an_equals_then_an_in_filter()
	{
		var component = RenderFilter(FilterDataTypes.Text);
		await OpenAsync(component);

		await ClickValueAsync(component, 0);
		TickedValues(component).Should().Equal("Alpha");
		await ClickValueAsync(component, 2);
		await ClickFilterAsync(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.In);
		filter.Value.Should().Be("Alpha|\"Gamma Ray\"");
	}

	/// <summary>
	/// Verifies that two values under Does not equal become a Not In list.
	/// </summary>
	[Fact]
	public async Task Two_values_under_does_not_equal_become_not_in()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.DoesNotEqual, "name", string.Empty));
		await OpenAsync(component);

		await ClickValueAsync(component, 0);
		await ClickValueAsync(component, 1);
		await ClickFilterAsync(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.NotIn);
		filter.Value.Should().Be("Alpha|Beta");
	}

	/// <summary>
	/// Verifies that clicking a selected value again deselects it and clears the value.
	/// </summary>
	[Fact]
	public async Task Clicking_a_selected_value_again_deselects_it()
	{
		var component = RenderFilter(FilterDataTypes.Text);
		await OpenAsync(component);

		await ClickValueAsync(component, 1);
		await ClickValueAsync(component, 1);
		await ClickFilterAsync(component);

		TickedValues(component).Should().BeEmpty();
		_changes.Should().ContainSingle().Which.Value.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that when In is not allowed a click replaces the selection instead of adding to it.
	/// </summary>
	[Fact]
	public async Task Without_in_a_click_replaces_the_selection()
	{
		var component = RenderFilter(FilterDataTypes.Text, options: FilterOptions.SingleValue());
		await OpenAsync(component);

		await ClickValueAsync(component, 0);
		await ClickValueAsync(component, 1);
		await ClickFilterAsync(component);

		TickedValues(component).Should().Equal("Beta");
		_changes.Should().ContainSingle().Which.Value.Should().Be("Beta");
	}

	/// <summary>
	/// Verifies that an ordering operator keeps a single selection, replacing it on each click.
	/// </summary>
	[Fact]
	public async Task An_ordering_operator_keeps_a_single_value()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, filter: new Filter(FilterTypes.GreaterThan, "size", "Alpha"));
		await OpenAsync(component);
		TickedValues(component).Should().Equal("Alpha");

		await ClickValueAsync(component, 1);
		await ClickFilterAsync(component);

		TickedValues(component).Should().Equal("Beta");
		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.GreaterThan);
		filter.Value.Should().Be("Beta");
	}

	/// <summary>
	/// Verifies that two clicked values under Range become its sorted lower and upper bounds.
	/// </summary>
	[Fact]
	public async Task Two_values_under_range_become_sorted_bounds()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, filter: new Filter(FilterTypes.Range, "size", string.Empty));
		await OpenAsync(component);
		component.FindAll(".filter-body input").Should().HaveCount(2, "a range has from and to boxes");

		await ClickValueAsync(component, 2);
		await ClickValueAsync(component, 0);
		await ClickFilterAsync(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.Range);
		filter.Value.Should().Be("Alpha");
		filter.Value2.Should().Be("Gamma Ray");
	}

	/// <summary>
	/// Verifies that opening an existing range ticks both bounds, and typed bounds are applied.
	/// </summary>
	[Fact]
	public async Task An_existing_range_is_ticked_and_typed_bounds_are_applied()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, filter: new Filter(FilterTypes.Range, "size", "Alpha", "Beta"));
		await OpenAsync(component);
		TickedValues(component).Should().Equal("Alpha", "Beta");

		await TypeValueAsync(component, 0, "1");
		await TypeValueAsync(component, 1, "9");
		await ClickFilterAsync(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.Value.Should().Be("1");
		filter.Value2.Should().Be("9");
	}

	/// <summary>
	/// Verifies that typing an In list ticks the values it names.
	/// </summary>
	[Fact]
	public async Task Typing_an_in_list_ticks_the_named_values()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.In, "name", string.Empty));
		await OpenAsync(component);

		await TypeValueAsync(component, 0, "Alpha|\"Gamma Ray\"");

		TickedValues(component).Should().Equal("Alpha", "Gamma Ray");
	}
}
