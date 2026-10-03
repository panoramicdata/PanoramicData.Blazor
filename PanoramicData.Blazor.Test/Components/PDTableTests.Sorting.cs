using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Sorting tests for <see cref="PDTable{TItem}"/>: header clicks, default directions and sort criteria.
/// </summary>
public partial class PDTableTests
{
	/// <summary>
	/// Clicking a string column sorts ascending, a second click reverses it, and SortChanged reports each.
	/// </summary>
	[Fact]
	public void Clicking_a_header_sorts_and_a_second_click_reverses()
	{
		var sorts = new List<SortCriteria>();
		var table = RenderTable(p => p.Add(x => x.SortChanged, s => sorts.Add(s)));

		table.Find("th#col-name span.pd-pointer").Click();
		Names(table).Should().Equal("Alpha", "Beta", "Gamma");
		table.Find("th#col-name span.pd-pointer").Click();
		Names(table).Should().Equal("Gamma", "Beta", "Alpha");

		sorts.Select(s => (s.Key, s.Direction)).Should().Equal(
			("col-name", SortDirection.Ascending),
			("col-name", SortDirection.Descending));
		table.Find("th#col-name").InnerHtml.Should().Contain("fa-sort-down");
		_common.Invocations["scrollIntoViewEx"].Should().HaveCount(2);
	}

	/// <summary>A non-string column sorts descending first, and the previously sorted column is reset.</summary>
	[Fact]
	public void A_numeric_column_sorts_descending_first_and_resets_the_previous_column()
	{
		var table = RenderTable();

		table.Find("th#col-name span.pd-pointer").Click();
		table.Find("th#col-score span.pd-pointer").Click();

		Names(table).Should().Equal("Gamma", "Beta", "Alpha");
		table.Instance.Columns.Single(c => c.Id == "col-name").SortDirection.Should().Be(SortDirection.None);
		table.Instance.SortCriteria.Key.Should().Be("col-score");
	}

	/// <summary>A column's default sort direction is used for its first sort.</summary>
	[Fact]
	public void A_column_default_sort_direction_is_used_first()
	{
		var table = RenderTable(columns:
		[
			new Col("col-name", x => x.Name) { Extra = { [nameof(PDColumn<Item>.DefaultSortDirection)] = SortDirection.Descending } }
		]);

		table.Find("th#col-name span.pd-pointer").Click();

		Names(table).Should().Equal("Gamma", "Beta", "Alpha");
	}

	/// <summary>An unsortable column has no sort control.</summary>
	[Fact]
	public void An_unsortable_column_has_no_sort_control()
	{
		var table = RenderTable(columns: [new Col("col-name", x => x.Name) { Extra = { [nameof(PDColumn<Item>.Sortable)] = false } }]);

		table.FindAll("th#col-name span.pd-pointer").Should().BeEmpty();
	}

	/// <summary>SortAsync sorts by property name in the direction given, and ignores an unknown property.</summary>
	[Fact]
	public async Task SortAsync_sorts_by_property_name()
	{
		var table = RenderTable();

		await table.InvokeAsync(() => table.Instance.SortAsync(new SortCriteria(nameof(Item.Score), SortDirection.Ascending)));
		Names(table).Should().Equal("Alpha", "Beta", "Gamma");

		var requests = _provider.Requests.Count;
		await table.InvokeAsync(() => table.Instance.SortAsync(new SortCriteria("Nope", SortDirection.Ascending)));
		_provider.Requests.Should().HaveCount(requests);
	}

	/// <summary>An initial sort criteria sorts the first load and marks the column.</summary>
	[Fact]
	public void An_initial_sort_criteria_applies_to_the_first_load()
	{
		var table = RenderTable(p => p.Add(x => x.SortCriteria, new SortCriteria("col-name", SortDirection.Descending)));

		Names(table).Should().Equal("Gamma", "Beta", "Alpha");
	}

	/// <summary>SetSortCriteria replaces the criteria used by the next fetch.</summary>
	[Fact]
	public async Task SetSortCriteria_applies_on_the_next_refresh()
	{
		var table = RenderTable();
		table.Instance.Columns.Single(c => c.Id == "col-score").SortDirection = SortDirection.Descending;

		table.Instance.SetSortCriteria(new SortCriteria("col-score", SortDirection.Descending));
		await table.InvokeAsync(table.Instance.RefreshAsync);

		Names(table).Should().Equal("Gamma", "Beta", "Alpha");
	}
}
