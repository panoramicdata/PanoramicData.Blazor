using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests of the operators <see cref="PDFilter"/> offers, and of how changing the operator reshapes the values.
/// </summary>
public partial class PDFilterTests
{
	/// <summary>
	/// Verifies the operators offered for text: the text operators, with no ordering or null operators.
	/// </summary>
	[Fact]
	public void Text_offers_text_operators_only()
	{
		var component = RenderFilter(FilterDataTypes.Text);

		OptionValues(component).Should().Equal(
			"Equals", "DoesNotEqual", "In", "NotIn",
			"StartsWith", "EndsWith", "Contains", "DoesNotContain", "IsEmpty", "IsNotEmpty");
	}

	/// <summary>
	/// Verifies the operators offered for a nullable number: ordering, range and null operators.
	/// </summary>
	[Fact]
	public void A_nullable_number_offers_ordering_range_and_null_operators()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, nullable: true);

		OptionValues(component).Should().Equal(
			"Equals", "DoesNotEqual", "In", "NotIn",
			"GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Range",
			"IsNull", "IsNotNull");
	}

	/// <summary>
	/// Verifies that operators disabled in the options are not offered.
	/// </summary>
	[Fact]
	public void Disabled_operators_are_not_offered()
	{
		var options = new FilterOptions
		{
			AllowEquals = false,
			AllowDoesNotEqual = false,
			AllowIn = false,
			AllowNotIn = false,
			AllowGreaterThan = false,
			AllowGreaterThanOrEqual = false,
			AllowLessThan = false,
			AllowLessThanOrEqual = false,
			AllowRange = false,
			AllowIsNull = false
		};

		var component = RenderFilter(FilterDataTypes.Date, nullable: true, options: options);

		OptionValues(component).Should().Equal("IsNotNull");
	}

	/// <summary>
	/// Verifies that the text operators can each be withheld by the options.
	/// </summary>
	[Fact]
	public void Disabled_text_operators_are_not_offered()
	{
		var options = new FilterOptions
		{
			AllowStartsWith = false,
			AllowEndsWith = false,
			AllowContains = false,
			AllowDoesNotContain = false,
			AllowIsEmpty = false,
			AllowIsNotEmpty = false
		};

		var component = RenderFilter(FilterDataTypes.Text, options: options);

		OptionValues(component).Should().Equal("Equals", "DoesNotEqual", "In", "NotIn");
	}

	/// <summary>
	/// Verifies that operators needing no value hide the value box.
	/// </summary>
	[Fact]
	public async Task Operators_needing_no_value_hide_the_value_box()
	{
		var component = RenderFilter(FilterDataTypes.Text, showValues: false, filter: new Filter(FilterTypes.IsEmpty, "name", string.Empty));

		await OpenAsync(component);

		component.FindAll(".filter-body input").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that changing the operator to In joins the selected values into a list.
	/// </summary>
	[Fact]
	public async Task Changing_the_operator_to_in_joins_the_selection()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.In, "name", "Alpha|Beta"));
		await OpenAsync(component);

		await component.InvokeAsync(() => component.Find("select").ChangeAsync(new ChangeEventArgs { Value = nameof(FilterTypes.NotIn) }));
		await ClickFilterAsync(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.NotIn);
		filter.Value.Should().Be("Alpha|Beta");
	}

	/// <summary>
	/// Verifies that changing to a single-value operator keeps only the first selected value.
	/// </summary>
	[Fact]
	public async Task Changing_to_a_single_value_operator_keeps_the_first_value()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.In, "name", "Beta|Alpha"));
		await OpenAsync(component);

		await component.InvokeAsync(() => component.Find("select").ChangeAsync(new ChangeEventArgs { Value = nameof(FilterTypes.Equals) }));
		await ClickFilterAsync(component);

		TickedValues(component).Should().Equal("Beta");
		_changes.Should().ContainSingle().Which.Value.Should().Be("Beta");
	}

	/// <summary>
	/// Verifies that changing to Range sorts the selection into lower and upper bounds.
	/// </summary>
	[Fact]
	public async Task Changing_to_range_sorts_the_selection_into_bounds()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, filter: new Filter(FilterTypes.In, "size", "Gamma Ray|Alpha"));
		await OpenAsync(component);

		await component.InvokeAsync(() => component.Find("select").ChangeAsync(new ChangeEventArgs { Value = nameof(FilterTypes.Range) }));
		await ClickFilterAsync(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.Value.Should().Be("Alpha");
		filter.Value2.Should().Be("Gamma Ray");
	}
}
