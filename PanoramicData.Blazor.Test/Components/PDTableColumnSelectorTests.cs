using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDTableColumnSelector{TItem}"/>: the list of columns it offers, toggling a column's
/// visibility and committing a new column order to the table.
/// </summary>
public class PDTableColumnSelectorTests : BunitContext
{
	private readonly RowProvider _provider = new();

	/// <summary>Sets up the rendering context.</summary>
	public PDTableColumnSelectorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDTable<Row>> RenderTable()
	{
		var table = Render<PDTable<Row>>(p => p
			.Add(x => x.DataProvider, _provider)
			.Add(x => x.KeyField, r => r.Id)
			.Add(x => x.ShowPager, false)
			.AddChildContent<PDColumn<Row>>(c => c.Add(x => x.Id, "a").Add(x => x.Field, r => r.Name).Add(x => x.Ordinal, 2))
			.AddChildContent<PDColumn<Row>>(c => c.Add(x => x.Id, "b").Add(x => x.Field, r => r.Size).Add(x => x.Name, "Named").Add(x => x.Ordinal, 1).Add(x => x.CanToggleVisible, false))
			.AddChildContent<PDColumn<Row>>(c => c.Add(x => x.Id, "c").Add(x => x.Field, r => r.Hidden).Add(x => x.IsVisible, false).Add(x => x.Ordinal, 3))
			.AddChildContent<PDColumn<Row>>(c => c.Add(x => x.Id, "d").Add(x => x.Field, r => r.Name).Add(x => x.ShowInList, false)));

		table.WaitForAssertion(() => table.Instance.Columns.Should().HaveCount(4));
		return table;
	}

	private IRenderedComponent<PDTableColumnSelector<Row>> RenderSelector(PDTable<Row>? table, bool canChangeVisible = true)
		=> Render<PDTableColumnSelector<Row>>(p => p
			.Add(x => x.Table, table)
			.Add(x => x.CanChangeVisible, canChangeVisible));

	private static PDColumn<Row> Column(IRenderedComponent<PDTable<Row>> table, string id)
		=> table.Instance.Columns.Single(c => c.Id == id);

	/// <summary>Every column that can be listed is offered, in ordinal order, labelled by name or title.</summary>
	[Fact]
	public void OffersListableColumns_InOrdinalOrder()
	{
		var table = RenderTable();

		var selector = RenderSelector(table.Instance);

		selector.FindAll(".pd-label").Select(x => x.TextContent.Trim()).Should().Equal("Named", "Name", "Hidden");
	}

	/// <summary>Checkboxes reflect visibility, and a column that cannot be toggled has its checkbox disabled.</summary>
	[Fact]
	public void Checkboxes_ReflectVisibilityAndToggleability()
	{
		var table = RenderTable();

		var boxes = RenderSelector(table.Instance).FindAll("input[type=checkbox]");

		boxes.Should().HaveCount(3);
		boxes[0].HasAttribute("disabled").Should().BeTrue();
		boxes[0].HasAttribute("checked").Should().BeTrue();
		boxes[1].HasAttribute("disabled").Should().BeFalse();
		boxes[1].HasAttribute("checked").Should().BeTrue();
		boxes[2].HasAttribute("checked").Should().BeFalse();
	}

	/// <summary>When visibility cannot be changed the columns are offered without checkboxes.</summary>
	[Fact]
	public void CanChangeVisibleFalse_OffersPlainItems()
	{
		var table = RenderTable();

		var selector = RenderSelector(table.Instance, canChangeVisible: false);

		selector.FindAll("input[type=checkbox]").Should().BeEmpty();
		selector.FindAll(".pd-label").Should().HaveCount(3);
	}

	/// <summary>Unticking a column hides it; ticking a hidden column shows it; untoggleable columns are untouched.</summary>
	[Fact]
	public void TogglingCheckboxes_ChangesColumnVisibility()
	{
		var table = RenderTable();
		var selector = RenderSelector(table.Instance);

		selector.FindAll("input[type=checkbox]")[1].Input(false);
		Column(table, "a").State.Visible.Should().BeFalse();

		selector.FindAll("input[type=checkbox]")[2].Input(true);
		Column(table, "c").State.Visible.Should().BeTrue();
		Column(table, "b").State.Visible.Should().BeTrue();
	}

	/// <summary>A new order sets each listed column's ordinal to its position; a column left out goes to the end.</summary>
	[Fact]
	public async Task OrderChanged_SetsOrdinals()
	{
		var table = RenderTable();
		var selector = RenderSelector(table.Instance);
		var c = new BasicItem { Id = "c" };
		IDisplayItem[] order = [c, new BasicItem { Id = "a" }];

		await selector.InvokeAsync(() => selector.Instance.OnOrderChanged(new DragOrderChangeArgs<IDisplayItem>(order, c)));

		Column(table, "c").State.Ordinal.Should().Be(0);
		Column(table, "a").State.Ordinal.Should().Be(1);
		Column(table, "b").State.Ordinal.Should().Be(1000);
		Column(table, "d").State.Ordinal.Should().Be(1000);
	}

	/// <summary>An order change reported by the drag panel is committed to the table.</summary>
	[Fact]
	public async Task DragPanelOrderChange_IsCommitted()
	{
		var table = RenderTable();
		var selector = RenderSelector(table.Instance);
		var panel = selector.FindComponent<PDDragPanel<IDisplayItem>>();
		var b = new BasicItem { Id = "b" };

		await panel.InvokeAsync(() => panel.Instance.ItemOrderChanged.InvokeAsync(new DragOrderChangeArgs<IDisplayItem>([new BasicItem { Id = "a" }, b], b)));

		Column(table, "a").State.Ordinal.Should().Be(0);
		Column(table, "b").State.Ordinal.Should().Be(1);
	}

	/// <summary>Without a table the selector renders nothing to choose and its handlers do nothing.</summary>
	[Fact]
	public async Task NoTable_OffersNothing_AndHandlersDoNothing()
	{
		var selector = RenderSelector(null);
		var item = new BasicItem { Id = "a" };

		await selector.InvokeAsync(() => selector.Instance.OnOrderChanged(new DragOrderChangeArgs<IDisplayItem>([item], item)));
		await selector.InvokeAsync(() => selector.Instance.OnSelectionChanged([item]));

		selector.FindAll(".pd-label").Should().BeEmpty();
		selector.Instance.CanChangeOrder.Should().BeTrue();
	}

	/// <summary>A row for the table under test.</summary>
	public sealed class Row
	{
		/// <summary>Gets or sets the key.</summary>
		public int Id { get; set; }

		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets the size.</summary>
		public int Size { get; set; }

		/// <summary>Gets or sets a value shown in a hidden column.</summary>
		public string Hidden { get; set; } = string.Empty;
	}

	/// <summary>A provider serving one row.</summary>
	private sealed class RowProvider : DataProviderBase<Row>
	{
		public override Task<DataResponse<Row>> GetDataAsync(DataRequest<Row> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			List<Row> rows = [new Row { Id = 1, Name = "Alpha" }];
			return Task.FromResult(new DataResponse<Row>(rows, rows.Count));
		}
	}
}
