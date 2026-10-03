using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFilter"/> fetches, displays and selects all of its candidate values.
/// </summary>
public partial class PDFilterTests
{
	/// <summary>
	/// Verifies that opening fetches values containing the values filter for the filter's key, lists them,
	/// and ticks those already in the filter.
	/// </summary>
	[Fact]
	public async Task Opening_fetches_lists_and_ticks_the_current_values()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.In, "name", "Beta|\"Gamma Ray\""));

		await OpenAsync(component);

		_fetchRequests.Should().ContainSingle();
		_fetchRequests[0].Key.Should().Be("name");
		_fetchRequests[0].FilterType.Should().Be(FilterTypes.Contains);
		ValueLabels(component).Select(l => l.TextContent.Trim()).Should().Equal("Alpha", "Beta", "Gamma Ray");
		TickedValues(component).Should().Equal("Beta", "Gamma Ray");
	}

	/// <summary>
	/// Verifies that a display function changes the listed text of each value.
	/// </summary>
	[Fact]
	public async Task A_display_function_changes_the_listed_text()
	{
		var component = RenderFilter(FilterDataTypes.Text, display: v => v.ToUpperInvariant());

		await OpenAsync(component);

		ValueLabels(component).Select(l => l.TextContent.Trim()).Should().Equal("ALPHA", "BETA", "GAMMA RAY");
	}

	/// <summary>
	/// Verifies that the values list is not shown, nor fetched, when ShowValues is off.
	/// </summary>
	[Fact]
	public async Task Values_are_not_fetched_or_shown_when_turned_off()
	{
		var component = RenderFilter(FilterDataTypes.Text, showValues: false);

		await OpenAsync(component);

		_fetchRequests.Should().BeEmpty();
		component.FindAll(".filter-values").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that Select all ticks every value into an In list, and a second click clears them.
	/// </summary>
	[Fact]
	public async Task Select_all_ticks_every_value_then_clears_them()
	{
		var component = RenderFilter(FilterDataTypes.Text, showSelectAll: true);
		await OpenAsync(component);
		component.Find(".values-select-all-count").TextContent.Should().Be("0 / 3");

		await component.InvokeAsync(() => component.Find(".values-select-all").ClickAsync(new MouseEventArgs()));
		component.Find(".values-select-all-count").TextContent.Should().Be("3 / 3");
		await ClickFilterAsync(component);
		_changes[^1].FilterType.Should().Be(FilterTypes.In);
		_changes[^1].Value.Should().Be("Alpha|Beta|\"Gamma Ray\"");

		await OpenAsync(component);
		await component.InvokeAsync(() => component.Find(".values-select-all").ClickAsync(new MouseEventArgs()));
		TickedValues(component).Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that Select all shows a partial icon when only some values are ticked.
	/// </summary>
	[Fact]
	public async Task Select_all_shows_a_partial_icon_for_some_values()
	{
		var component = RenderFilter(FilterDataTypes.Text, showSelectAll: true, filter: new Filter(FilterTypes.In, "name", "Beta"));

		await OpenAsync(component);

		component.Find(".values-select-all i").ClassList.Should().Contain("fa-minus-square");
	}

	/// <summary>
	/// Verifies that typing in the values filter fetches the values again with that text.
	/// </summary>
	[Fact]
	public async Task The_values_filter_refetches_with_its_text()
	{
		var component = RenderFilter(FilterDataTypes.Text);
		await OpenAsync(component);
		var valuesBox = component.FindComponents<PDTextBox>().Single(t => t.Instance.DebounceWait > 0);

		await component.InvokeAsync(() => valuesBox.Instance.OnDebouncedInput("gam"));

		_fetchRequests.Should().HaveCount(2);
		_fetchRequests[^1].Value.Should().Be("gam");
	}
}
