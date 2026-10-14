using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDTable{TItem}"/> re-renders the rows a fetch loads, even when the fetch was started
/// from outside the table (issue #241).
/// </summary>
/// <remarks>
/// A fetch set <c>ItemsToDisplay</c> but never re-rendered, so a parent calling
/// <see cref="PDTable{TItem}.RefreshAsync()"/> after deleting an item saw the pager count drop while the
/// deleted row stayed on screen.
/// </remarks>
public class PDTableRefreshRenderTests : BunitContext
{
	private readonly RowProvider _provider = new();

	/// <summary>Sets up the rendering context.</summary>
	public PDTableRefreshRenderTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDTable<Row>> RenderTable() => Render<PDTable<Row>>(parameters => parameters
		.Add(p => p.DataProvider, _provider)
		.Add(p => p.KeyField, row => row.Id)
		.Add(p => p.SelectionMode, TableSelectionMode.Multiple)
		.Add(p => p.ShowPager, false)
		.Add(p => p.ChildContent, builder =>
		{
			builder.OpenComponent<PDColumn<Row>>(0);
			builder.AddAttribute(1, nameof(PDColumn<Row>.Id), nameof(Row.Name));
			builder.AddAttribute(2, nameof(PDColumn<Row>.Field), (System.Linq.Expressions.Expression<Func<Row, object>>)(row => row.Name));
			builder.CloseComponent();
		}));

	private static List<string> RenderedNames(IRenderedComponent<PDTable<Row>> table)
		=> [.. table.FindAll("tbody tr.pdtablerow").Select(tr => tr.TextContent.Trim())];

	/// <summary>A refresh started from outside the table shows the rows the provider now returns.</summary>
	[Fact]
	public async Task Refresh_FromOutsideTheTable_RendersTheNewRows()
	{
		var table = RenderTable();
		table.WaitForAssertion(() => RenderedNames(table).Should().HaveCount(3));

		_provider.Rows.RemoveAll(r => r.Id == 2);
		await table.InvokeAsync(() => table.Instance.RefreshAsync());

		RenderedNames(table).Should().Equal("Alpha", "Gamma");
	}

	/// <summary>
	/// A refresh after deleting the selected row removes it from the screen: clearing the selection re-renders
	/// before the fetch, so that render alone still shows the old rows.
	/// </summary>
	[Fact]
	public async Task Refresh_AfterDeletingTheSelectedRow_RemovesItFromTheScreen()
	{
		var table = RenderTable();
		table.WaitForAssertion(() => RenderedNames(table).Should().HaveCount(3));
		await table.FindAll("tbody tr.pdtablerow")[1].MouseUpAsync(new MouseEventArgs { Button = 0 });
		table.Instance.Selection.Should().Equal("2");

		_provider.Rows.RemoveAll(r => r.Id == 2);
		await table.InvokeAsync(() => table.Instance.RefreshAsync());

		RenderedNames(table).Should().Equal("Alpha", "Gamma");
	}

	/// <summary>A row in the table under test.</summary>
	/// <param name="Id">Key.</param>
	/// <param name="Name">Display name.</param>
	public sealed record Row(int Id, string Name);

	/// <summary>An in-memory provider returning the requested page of whatever rows it currently holds.</summary>
	private sealed class RowProvider : DataProviderBase<Row>
	{
		public List<Row> Rows { get; } = [new(1, "Alpha"), new(2, "Beta"), new(3, "Gamma")];

		public override Task<DataResponse<Row>> GetDataAsync(DataRequest<Row> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var page = Rows.Skip(request.Skip ?? 0).Take(request.Take ?? Rows.Count);
			return Task.FromResult(new DataResponse<Row>([.. page], Rows.Count));
		}
	}
}
