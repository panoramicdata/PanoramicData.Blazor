using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tabular results: the studio shows a service's result set in a grid that sorts, styles NULLs and numbers,
/// and summarises the rows in the status bar.
/// </summary>
public partial class PDStudioTests
{
	private static PDStudioResultSet Devices() => new()
	{
		Columns = [new("device", "text"), new("clients", "integer"), new("last_seen", "timestamp")],
		Rows =
		[
			["HO-AP-14", 12, new DateTime(2026, 10, 21, 17, 40, 0)],
			["WH2-AP-07", 3, null],
			["BN-MX-01", 40, new DateTime(2026, 10, 29, 6, 12, 0)]
		],
		IsTruncated = true,
		RowLimit = 3,
		Duration = TimeSpan.FromMilliseconds(142)
	};

	private static List<string> Column(IRenderedComponent<PDStudio> studio, int column)
		=> [.. studio.FindAll(".pd-studio-grid tbody tr").Select(r => r.Children[column + 1].TextContent)];

	private async Task<IRenderedComponent<PDStudio>> RunDevicesAsync()
	{
		_service.ResultSet = Devices();
		var studio = RenderStudio();
		await TypeAsync(studio, "SELECT * FROM devices");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());
		studio.WaitForAssertion(() => studio.FindAll(".pd-studio-grid").Should().ContainSingle());
		return studio;
	}

	/// <summary>
	/// Verifies that a result set is shown in the grid with each column's type, NULLs marked, numbers aligned
	/// right, and the rows, time and limit in the status bar, instead of the HTML results frame.
	/// </summary>
	[Fact]
	public async Task ResultGrid_ShowsTheResultSet()
	{
		var studio = await RunDevicesAsync();

		studio.FindAll("iframe").Should().BeEmpty();
		studio.FindAll(".pd-studio-grid-type").Select(t => t.TextContent).Should().Equal("text", "integer", "timestamp");
		Column(studio, 0).Should().Equal("HO-AP-14", "WH2-AP-07", "BN-MX-01");
		Column(studio, 2).Should().Equal("2026-10-21 17:40:00", "NULL", "2026-10-29 06:12:00");
		studio.Find(".pd-studio-grid-null").TextContent.Should().Be("NULL");
		studio.FindAll(".pd-studio-grid-number").Select(c => c.TextContent).Should().Equal("12", "3", "40");
		studio.Find(".pd-studio-results-summary").TextContent.Should().Be("3 rows, 142 ms, limited to 3 rows");
		Status(studio).Should().Be("Complete");
		studio.Instance.ResultSet.Should().BeSameAs(_service.ResultSet);
		LogMessages(studio).Should().Contain("3 rows returned in 142 ms");
	}

	/// <summary>
	/// Verifies that only NULL is muted: zero, false and an empty string are values and keep the normal style.
	/// </summary>
	[Fact]
	public async Task ResultGrid_MutesOnlyNull()
	{
		_service.ResultSet = new PDStudioResultSet
		{
			Columns = [new("count", "integer"), new("enabled", "boolean"), new("note", "text"), new("seen", "timestamp")],
			Rows = [[0, false, "", null]]
		};
		var studio = RenderStudio();
		await TypeAsync(studio, "SELECT 0, false, '', NULL");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());
		studio.WaitForAssertion(() => studio.FindAll(".pd-studio-grid").Should().ContainSingle());

		var cells = studio.FindAll(".pd-studio-grid tbody td").Skip(1).ToList();
		cells.Select(c => c.TextContent).Should().Equal("0", "False", "", "NULL");
		cells.Select(c => c.ClassList.Contains("pd-studio-grid-null")).Should().Equal(false, false, false, true);
	}

	/// <summary>
	/// Verifies that a query returning no rows still reports how long it took.
	/// </summary>
	[Fact]
	public async Task ResultGrid_NoRows_StillShowsTheTimeTaken()
	{
		_service.ResultSet = new PDStudioResultSet { Columns = [new("id", "bigint")], Duration = TimeSpan.FromSeconds(2.4) };
		var studio = RenderStudio();
		await TypeAsync(studio, "SELECT id FROM nothing");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		studio.WaitForAssertion(() => studio.Find(".pd-studio-results-summary").TextContent.Should().Be("0 rows, 2.4 s"));
		LogMessages(studio).Should().Contain("0 rows returned in 2.4 s");
	}

	/// <summary>
	/// Verifies that a heading sorts ascending, then descending with NULLs last, then restores the original order.
	/// </summary>
	[Fact]
	public async Task ResultGrid_HeadingCyclesTheSortOrder()
	{
		var studio = await RunDevicesAsync();
		var heading = () => studio.FindAll(".pd-studio-grid-heading")[2];

		heading().Click();
		Column(studio, 0).Should().Equal("WH2-AP-07", "HO-AP-14", "BN-MX-01");
		studio.FindAll("thead th")[3].GetAttribute("aria-sort").Should().Be("ascending");

		heading().Click();
		Column(studio, 0).Should().Equal("BN-MX-01", "HO-AP-14", "WH2-AP-07");

		heading().Click();
		Column(studio, 0).Should().Equal("HO-AP-14", "WH2-AP-07", "BN-MX-01");
	}

	/// <summary>
	/// Verifies that a result set with no rows says so, and that HTML results replace an earlier grid.
	/// </summary>
	[Fact]
	public async Task ResultGrid_EmptyThenHtml()
	{
		_service.ResultSet = new PDStudioResultSet { Columns = [new("id", "bigint")] };
		var studio = RenderStudio();
		await TypeAsync(studio, "SELECT id FROM nothing");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());
		studio.WaitForAssertion(() => studio.Find(".pd-studio-grid-empty").TextContent.Should().Be("No rows"));

		_service.ResultSet = null;
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		studio.WaitForAssertion(() => studio.FindAll("iframe").Should().ContainSingle());
		studio.FindAll(".pd-studio-grid").Should().BeEmpty();
		studio.Instance.ResultSet.Should().BeNull();
	}

	/// <summary>
	/// Verifies the CSV: a heading row, invariant formatting, NULL as empty, and quoting where needed.
	/// </summary>
	[Fact]
	public void ResultSet_ToCsv_QuotesWhereNeeded()
	{
		var set = new PDStudioResultSet
		{
			Columns = [new("name", "text"), new("ratio", "numeric"), new("note", "text")],
			Rows = [["Head Office", 1.5m, "says \"hi\", twice"], ["Branch", null, "line\nbreak"]]
		};

		set.ToCsv().Should().Be(
			"name,ratio,note" + Environment.NewLine
			+ "Head Office,1.5,\"says \"\"hi\"\", twice\"" + Environment.NewLine
			+ "Branch,,\"line\nbreak\"" + Environment.NewLine);
	}
}
