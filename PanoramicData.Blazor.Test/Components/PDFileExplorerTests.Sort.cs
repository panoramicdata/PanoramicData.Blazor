using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Sorting of the file table of <see cref="PDFileExplorer"/>: the starting sort a consumer gives, and the sort
/// changes it reports.
/// </summary>
public partial class PDFileExplorerTests
{
	/// <summary>Without a starting sort the table is sorted by Name, ascending.</summary>
	[Fact]
	public async Task NoStartingSort_SortsByNameAscending()
	{
		var cut = RenderExplorer();

		await NavigateAsync(cut, "/Docs");

		Table(cut).Instance.SortCriteria.Key.Should().Be("Name");
		Table(cut).Instance.SortCriteria.Direction.Should().Be(SortDirection.Ascending);
		RowNames(cut).Should().Equal("..", "Sub", "a.docx", "b.xlsx");
	}

	/// <summary>A starting sort orders the rows by that column, and its header shows the direction.</summary>
	[Fact]
	public async Task StartingSort_OrdersTheRowsByThatColumn()
	{
		var cut = RenderExplorer(p => p.Add(x => x.TableSortCriteria, new SortCriteria("Size", SortDirection.Descending)));

		await NavigateAsync(cut, "/Docs");

		RowNames(cut).Should().Equal("..", "Sub", "b.xlsx", "a.docx");
		Table(cut).Find("th#Size").InnerHtml.Should().Contain("fa-sort-down");
	}

	/// <summary>A blank starting key falls back to Name, ascending.</summary>
	[Fact]
	public async Task BlankStartingSortKey_SortsByNameAscending()
	{
		var cut = RenderExplorer(p => p.Add(x => x.TableSortCriteria, new SortCriteria(" ", SortDirection.Descending)));

		await NavigateAsync(cut, "/Docs");

		Table(cut).Instance.SortCriteria.Key.Should().Be("Name");
		Table(cut).Instance.SortCriteria.Direction.Should().Be(SortDirection.Ascending);
	}

	/// <summary>Sorting by a header reports the column and direction, and leaves the given starting sort untouched.</summary>
	[Fact]
	public async Task SortingByAHeader_RaisesTableSortChanged_AndKeepsTheStartingSort()
	{
		var starting = new SortCriteria("Name", SortDirection.Ascending);
		var sorts = new List<SortCriteria>();
		var cut = RenderExplorer(p => p
			.Add(x => x.TableSortCriteria, starting)
			.Add(x => x.TableSortChanged, s => sorts.Add(s)));
		await NavigateAsync(cut, "/Docs");

		Table(cut).Find("th#Size span.pd-pointer").Click();

		cut.WaitForAssertion(() => RowNames(cut).Should().Equal("..", "Sub", "b.xlsx", "a.docx"));
		sorts.Select(s => (s.Key, s.Direction)).Should().Equal(("Size", SortDirection.Descending));
		(starting.Key, starting.Direction).Should().Be(("Name", SortDirection.Ascending));
	}
}
