using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDTable{TItem}"/> keeps its selection across a plain refresh when
/// <see cref="PDTable{TItem}.RetainSelectionOnRefresh"/> is set, and behaves as before when it is not (issue #151).
/// </summary>
/// <remarks>
/// Every fetch used to clear the selection and raise <see cref="PDTable{TItem}.SelectionChanged"/> unless
/// <see cref="PDTable{TItem}.RetainSelectionOnPage"/> was set, and <see cref="PDTable{TItem}.RefreshAsync()"/>
/// is a fetch. A page that auto-refreshes its table therefore deselected the user's row every few seconds,
/// and anything bound to the selection (a detail panel) reset with it. Found on the Magic Suite Ops Dashboard
/// GitHub Actions page (MS-26826).
/// </remarks>
public class PDTableRefreshSelectionTests : BunitContext
{
	private readonly RefreshRowProvider _provider = new(4);
	private int _selectionChangedCount;

	/// <summary>Sets up the rendering context.</summary>
	public PDTableRefreshSelectionTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDTable<RefreshRow>> RenderTable(bool retainSelectionOnPage = false, PageCriteria? pageCriteria = null, bool retainSelectionOnRefresh = true)
	{
		var table = Render<PDTable<RefreshRow>>(parameters => parameters
			.Add(p => p.DataProvider, _provider)
			.Add(p => p.KeyField, row => row.Id)
			.Add(p => p.SelectionMode, TableSelectionMode.Single)
			.Add(p => p.RetainSelectionOnPage, retainSelectionOnPage)
			.Add(p => p.RetainSelectionOnRefresh, retainSelectionOnRefresh)
			.Add(p => p.PageCriteria, pageCriteria)
			.Add(p => p.ShowPager, false)
			.Add(p => p.SelectionChanged, () => _selectionChangedCount++)
			.Add(p => p.ChildContent, builder => RefreshColumns.Name(builder, sortable: true)));

		table.WaitForAssertion(() => table.FindAll("tbody tr.pdtablerow").Should().NotBeEmpty());
		return table;
	}

	private void Select(IRenderedComponent<PDTable<RefreshRow>> table, string name)
	{
		var row = table.FindAll("tbody tr.pdtablerow").Single(tr => tr.TextContent.Contains(name, StringComparison.Ordinal));
		row.MouseUp(new MouseEventArgs { Button = 0 });
		table.Instance.Selection.Should().ContainSingle();
		_selectionChangedCount = 0;
	}

	private IRenderedComponent<PDTable<RefreshRow>> RenderWithBetaSelected(bool retainSelectionOnPage = false, PageCriteria? pageCriteria = null, bool retainSelectionOnRefresh = true)
	{
		var table = RenderTable(retainSelectionOnPage, pageCriteria, retainSelectionOnRefresh);
		Select(table, "Beta");
		return table;
	}

	private static Task ActAsync(IRenderedComponent<PDTable<RefreshRow>> table, Func<PDTable<RefreshRow>, Task> action)
		=> table.InvokeAsync(() => action(table.Instance));

	private void AssertSelectionKept(IRenderedComponent<PDTable<RefreshRow>> table)
	{
		table.Instance.Selection.Should().Equal("2");
		_selectionChangedCount.Should().Be(0);
	}

	private void AssertSelectionCleared(IRenderedComponent<PDTable<RefreshRow>> table)
	{
		table.Instance.Selection.Should().BeEmpty();
		_selectionChangedCount.Should().Be(1);
	}

	/// <summary>A refresh over the same data keeps the selected row and announces nothing.</summary>
	[Fact]
	public async Task Refresh_KeepsTheSelection_AndRaisesNoSelectionChanged()
	{
		var table = RenderWithBetaSelected();

		await ActAsync(table, t => t.RefreshAsync());

		AssertSelectionKept(table);
	}

	/// <summary>A refresh that brings back new row objects for the same keys still keeps the selection.</summary>
	[Fact]
	public async Task Refresh_WithNewRowObjectsForTheSameKeys_KeepsTheSelection()
	{
		var table = RenderWithBetaSelected();

		_provider.Rows = [.. _provider.Rows.Select(r => new RefreshRow(r.Id, r.Name))];
		await ActAsync(table, t => t.RefreshAsync());

		table.Instance.GetSelectedItems().Should().ContainSingle().Which.Should().BeSameAs(_provider.Rows[1]);
		AssertSelectionKept(table);
	}

	/// <summary>A refresh in which the selected row has gone deselects it and says so once.</summary>
	[Fact]
	public async Task Refresh_WhenTheSelectedRowHasGone_DeselectsIt_AndRaisesSelectionChangedOnce()
	{
		var table = RenderWithBetaSelected();

		_provider.Rows.RemoveAll(r => r.Id == 2);
		await ActAsync(table, t => t.RefreshAsync());

		AssertSelectionCleared(table);
	}

	/// <summary>A new search is a different view, so the selection is still cleared as before.</summary>
	[Fact]
	public async Task RefreshWithNewSearchText_StillClearsTheSelection()
	{
		var table = RenderWithBetaSelected();

		await ActAsync(table, t => t.RefreshAsync("Alpha"));

		AssertSelectionCleared(table);
	}

	/// <summary>A sort is a different view, so the selection is still cleared as before.</summary>
	[Fact]
	public async Task Sort_StillClearsTheSelection()
	{
		var table = RenderWithBetaSelected();

		await ActAsync(table, t => t.SortAsync(new SortCriteria(nameof(RefreshRow.Name), SortDirection.Descending)));

		AssertSelectionCleared(table);
	}

	/// <summary>Moving to another page is a different view, so the selection is still cleared as before.</summary>
	[Fact]
	public async Task PageChange_StillClearsTheSelection()
	{
		var table = RenderWithBetaSelected(pageCriteria: new PageCriteria(1, 2));

		await ActAsync(table, t => t.PageAsync(new PageCriteria(2, 2)));

		AssertSelectionCleared(table);
	}

	/// <summary><see cref="PDTable{TItem}.RetainSelectionOnPage"/> keeps its meaning: the selection survives a page change.</summary>
	[Fact]
	public async Task PageChange_WithRetainSelectionOnPage_KeepsTheSelection()
	{
		var table = RenderWithBetaSelected(retainSelectionOnPage: true, pageCriteria: new PageCriteria(1, 2));

		await ActAsync(table, t => t.PageAsync(new PageCriteria(2, 2)));

		AssertSelectionKept(table);
	}

	/// <summary>
	/// With <see cref="PDTable{TItem}.RetainSelectionOnPage"/>, a refresh does not drop a selected key that is on
	/// another page: that selection deliberately spans pages.
	/// </summary>
	[Fact]
	public async Task Refresh_WithRetainSelectionOnPage_KeepsASelectionFromAnotherPage()
	{
		var table = RenderWithBetaSelected(retainSelectionOnPage: true, pageCriteria: new PageCriteria(1, 2));
		await ActAsync(table, t => t.PageAsync(new PageCriteria(2, 2)));

		await ActAsync(table, t => t.RefreshAsync());

		AssertSelectionKept(table);
	}

	/// <summary>
	/// Without <see cref="PDTable{TItem}.RetainSelectionOnRefresh"/> (the default), a refresh still clears the
	/// selection and raises <see cref="PDTable{TItem}.SelectionChanged"/>, exactly as earlier versions did, so
	/// consumers that cache the selected row and rely on a refresh to clear it are unaffected.
	/// </summary>
	[Fact]
	public async Task Refresh_ByDefault_StillClearsTheSelection_AsBefore()
	{
		var table = RenderWithBetaSelected(retainSelectionOnRefresh: false);

		await ActAsync(table, t => t.RefreshAsync());

		AssertSelectionCleared(table);
	}

	/// <summary>The opt-in is off unless a consumer sets it.</summary>
	[Fact]
	public void RetainSelectionOnRefresh_IsOffByDefault()
		=> new PDTable<RefreshRow>().RetainSelectionOnRefresh.Should().BeFalse();
}
