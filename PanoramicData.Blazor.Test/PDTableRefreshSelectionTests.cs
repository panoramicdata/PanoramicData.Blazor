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
	private readonly RowProvider _provider = new();
	private int _selectionChangedCount;

	/// <summary>Sets up the rendering context.</summary>
	public PDTableRefreshSelectionTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDTable<Row>> RenderTable(bool retainSelectionOnPage = false, PageCriteria? pageCriteria = null, bool retainSelectionOnRefresh = true)
	{
		var table = Render<PDTable<Row>>(parameters => parameters
			.Add(p => p.DataProvider, _provider)
			.Add(p => p.KeyField, row => row.Id)
			.Add(p => p.SelectionMode, TableSelectionMode.Single)
			.Add(p => p.RetainSelectionOnPage, retainSelectionOnPage)
			.Add(p => p.RetainSelectionOnRefresh, retainSelectionOnRefresh)
			.Add(p => p.PageCriteria, pageCriteria)
			.Add(p => p.ShowPager, false)
			.Add(p => p.SelectionChanged, () => _selectionChangedCount++)
			.Add(p => p.ChildContent, builder =>
			{
				builder.OpenComponent<PDColumn<Row>>(0);
				builder.AddAttribute(1, nameof(PDColumn<Row>.Id), nameof(Row.Name));
				builder.AddAttribute(2, nameof(PDColumn<Row>.Field), (System.Linq.Expressions.Expression<Func<Row, object>>)(row => row.Name));
				builder.AddAttribute(3, nameof(PDColumn<Row>.Sortable), true);
				builder.CloseComponent();
			}));

		table.WaitForAssertion(() => table.FindAll("tbody tr.pdtablerow").Should().NotBeEmpty());
		return table;
	}

	private void Select(IRenderedComponent<PDTable<Row>> table, string name)
	{
		var row = table.FindAll("tbody tr.pdtablerow").Single(tr => tr.TextContent.Contains(name, StringComparison.Ordinal));
		row.MouseUp(new MouseEventArgs { Button = 0 });
		table.Instance.Selection.Should().ContainSingle();
		_selectionChangedCount = 0;
	}

	/// <summary>A refresh over the same data keeps the selected row and announces nothing.</summary>
	[Fact]
	public async Task Refresh_KeepsTheSelection_AndRaisesNoSelectionChanged()
	{
		var table = RenderTable();
		Select(table, "Beta");

		await table.InvokeAsync(() => table.Instance.RefreshAsync());

		table.Instance.Selection.Should().Equal("2");
		_selectionChangedCount.Should().Be(0);
	}

	/// <summary>A refresh that brings back new row objects for the same keys still keeps the selection.</summary>
	[Fact]
	public async Task Refresh_WithNewRowObjectsForTheSameKeys_KeepsTheSelection()
	{
		var table = RenderTable();
		Select(table, "Beta");

		_provider.Rows = [.. _provider.Rows.Select(r => new Row(r.Id, r.Name))];
		await table.InvokeAsync(() => table.Instance.RefreshAsync());

		table.Instance.Selection.Should().Equal("2");
		table.Instance.GetSelectedItems().Should().ContainSingle().Which.Should().BeSameAs(_provider.Rows[1]);
		_selectionChangedCount.Should().Be(0);
	}

	/// <summary>A refresh in which the selected row has gone deselects it and says so once.</summary>
	[Fact]
	public async Task Refresh_WhenTheSelectedRowHasGone_DeselectsIt_AndRaisesSelectionChangedOnce()
	{
		var table = RenderTable();
		Select(table, "Beta");

		_provider.Rows.RemoveAll(r => r.Id == 2);
		await table.InvokeAsync(() => table.Instance.RefreshAsync());

		table.Instance.Selection.Should().BeEmpty();
		_selectionChangedCount.Should().Be(1);
	}

	/// <summary>A new search is a different view, so the selection is still cleared as before.</summary>
	[Fact]
	public async Task RefreshWithNewSearchText_StillClearsTheSelection()
	{
		var table = RenderTable();
		Select(table, "Beta");

		await table.InvokeAsync(() => table.Instance.RefreshAsync("Alpha"));

		table.Instance.Selection.Should().BeEmpty();
		_selectionChangedCount.Should().Be(1);
	}

	/// <summary>A sort is a different view, so the selection is still cleared as before.</summary>
	[Fact]
	public async Task Sort_StillClearsTheSelection()
	{
		var table = RenderTable();
		Select(table, "Beta");

		await table.InvokeAsync(() => table.Instance.SortAsync(new SortCriteria(nameof(Row.Name), SortDirection.Descending)));

		table.Instance.Selection.Should().BeEmpty();
		_selectionChangedCount.Should().Be(1);
	}

	/// <summary>Moving to another page is a different view, so the selection is still cleared as before.</summary>
	[Fact]
	public async Task PageChange_StillClearsTheSelection()
	{
		var table = RenderTable(pageCriteria: new PageCriteria(1, 2));
		Select(table, "Beta");

		await table.InvokeAsync(() => table.Instance.PageAsync(new PageCriteria(2, 2)));

		table.Instance.Selection.Should().BeEmpty();
		_selectionChangedCount.Should().Be(1);
	}

	/// <summary><see cref="PDTable{TItem}.RetainSelectionOnPage"/> keeps its meaning: the selection survives a page change.</summary>
	[Fact]
	public async Task PageChange_WithRetainSelectionOnPage_KeepsTheSelection()
	{
		var table = RenderTable(retainSelectionOnPage: true, pageCriteria: new PageCriteria(1, 2));
		Select(table, "Beta");

		await table.InvokeAsync(() => table.Instance.PageAsync(new PageCriteria(2, 2)));

		table.Instance.Selection.Should().Equal("2");
		_selectionChangedCount.Should().Be(0);
	}

	/// <summary>
	/// With <see cref="PDTable{TItem}.RetainSelectionOnPage"/>, a refresh does not drop a selected key that is on
	/// another page: that selection deliberately spans pages.
	/// </summary>
	[Fact]
	public async Task Refresh_WithRetainSelectionOnPage_KeepsASelectionFromAnotherPage()
	{
		var table = RenderTable(retainSelectionOnPage: true, pageCriteria: new PageCriteria(1, 2));
		Select(table, "Beta");
		await table.InvokeAsync(() => table.Instance.PageAsync(new PageCriteria(2, 2)));

		await table.InvokeAsync(() => table.Instance.RefreshAsync());

		table.Instance.Selection.Should().Equal("2");
		_selectionChangedCount.Should().Be(0);
	}

	/// <summary>
	/// Without <see cref="PDTable{TItem}.RetainSelectionOnRefresh"/> (the default), a refresh still clears the
	/// selection and raises <see cref="PDTable{TItem}.SelectionChanged"/>, exactly as earlier versions did, so
	/// consumers that cache the selected row and rely on a refresh to clear it are unaffected.
	/// </summary>
	[Fact]
	public async Task Refresh_ByDefault_StillClearsTheSelection_AsBefore()
	{
		var table = RenderTable(retainSelectionOnRefresh: false);
		Select(table, "Beta");

		await table.InvokeAsync(() => table.Instance.RefreshAsync());

		table.Instance.Selection.Should().BeEmpty();
		_selectionChangedCount.Should().Be(1);
	}

	/// <summary>The opt-in is off unless a consumer sets it.</summary>
	[Fact]
	public void RetainSelectionOnRefresh_IsOffByDefault()
		=> new PDTable<Row>().RetainSelectionOnRefresh.Should().BeFalse();

	/// <summary>A row in the table under test.</summary>
	/// <param name="Id">Key.</param>
	/// <param name="Name">Display name.</param>
	public sealed record Row(int Id, string Name);

	/// <summary>An in-memory provider that honours search text, sort and paging, as a real one does.</summary>
	private sealed class RowProvider : DataProviderBase<Row>
	{
		public List<Row> Rows { get; set; } = [new(1, "Alpha"), new(2, "Beta"), new(3, "Gamma"), new(4, "Delta")];

		public override Task<DataResponse<Row>> GetDataAsync(DataRequest<Row> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			IEnumerable<Row> rows = Rows;
			if (!string.IsNullOrWhiteSpace(request.SearchText))
			{
				rows = rows.Where(r => r.Name.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase));
			}

			if (request.SortFieldExpression is not null)
			{
				var key = request.SortFieldExpression.Compile();
				rows = request.SortDirection == SortDirection.Descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
			}

			var all = rows.ToList();
			IEnumerable<Row> page = all;
			if (request.Skip is { } skip)
			{
				page = page.Skip(skip);
			}

			if (request.Take is { } take)
			{
				page = page.Take(take);
			}

			return Task.FromResult(new DataResponse<Row>([.. page], all.Count));
		}

	}
}
