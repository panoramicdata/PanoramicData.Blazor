using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Filtering, drag and drop, and saved column state tests for <see cref="PDTable{TItem}"/>.
/// </summary>
public partial class PDTableTests
{
	/// <summary>A filterable column renders a filter and maps its filter key to the property on the provider.</summary>
	[Fact]
	public void Filterable_columns_render_a_filter_and_register_the_mapping()
	{
		var table = RenderTable(columns: [FilterColumn()]);

		table.FindComponents<PDFilter>().Should().ContainSingle();
		_provider.KeyPropertyMappings["name"].Should().Be(nameof(Item.Name));
	}

	/// <summary>Changing a filter builds the search text, raises SearchTextChanged and refetches with it.</summary>
	[Fact]
	public async Task Changing_a_filter_searches_with_it()
	{
		var searches = new List<string?>();
		var table = RenderTable(p => p.Add(x => x.SearchTextChanged, s => searches.Add(s)), columns: [FilterColumn()]);
		var column = table.Instance.Columns.Single();
		column.Filter.FilterType = FilterTypes.Equals;
		column.Filter.Value = "Beta";

		var filter = table.FindComponent<PDFilter>();
		await table.InvokeAsync(() => filter.Instance.FilterChanged.InvokeAsync(column.Filter));

		searches.Should().Equal("name:Beta");
		_provider.Requests.Last().SearchText.Should().Be("name:Beta");
	}

	/// <summary>Filter values come from the distinct values of the provider, formatted as the column asks.</summary>
	[Fact]
	public async Task Filter_values_come_from_the_provider()
	{
		var table = RenderTable(columns:
		[
			FilterColumn(),
			new Col("col-score", x => x.Score) { Extra = { [nameof(PDColumn<Item>.Filterable)] = true, [nameof(PDColumn<Item>.FilterKey)] = "score", [nameof(PDColumn<Item>.Format)] = "D3" } }
		]);
		var filters = table.FindComponents<PDFilter>();

		var names = await table.InvokeAsync(() => filters[0].Instance.FetchValuesAsync!(new Filter(FilterTypes.Equals, "name", "Alpha")));
		var scores = await table.InvokeAsync(() => filters[1].Instance.FetchValuesAsync!(new Filter()));

		names.Should().Equal("Alpha", "Beta", "Gamma");
		scores.Should().Equal("010", "020", "030");
	}

	/// <summary>Suggested values take the place of values from the provider.</summary>
	[Fact]
	public async Task Suggested_filter_values_are_used_when_given()
	{
		var column = FilterColumn();
		column.Extra[nameof(PDColumn<Item>.FilterSuggestedValues)] = new object[] { "x", "y" };
		var table = RenderTable(columns: [column]);

		var values = await table.InvokeAsync(() => table.FindComponent<PDFilter>().Instance.FetchValuesAsync!(new Filter()));

		values.Should().Equal("x", "y");
	}

	/// <summary>A provider without filter support is queried directly for the distinct values.</summary>
	[Fact]
	public async Task Filter_values_from_a_plain_provider()
	{
		var provider = new ListDataProviderService<Item>([.. _provider.Items, new Item { Id = 4, Name = "Alpha" }]);
		var table = Render<PDTable<Item>>(parameters => parameters
			.Add(p => p.DataProvider, provider)
			.Add(p => p.ChildContent, Columns([FilterColumn()])));
		table.WaitForAssertion(() => table.FindComponents<PDFilter>().Should().ContainSingle());

		var values = await table.InvokeAsync(() => table.FindComponent<PDFilter>().Instance.FetchValuesAsync!(new Filter()));

		values.Should().Equal("Alpha", "Beta", "Gamma");
	}

	/// <summary>Search text given as a parameter is loaded into the matching column filter, and clearing it clears the filter.</summary>
	[Fact]
	public void SearchText_parameter_updates_the_column_filters()
	{
		var table = RenderTable(columns: [FilterColumn()]);
		var column = table.Instance.Columns.Single();

		table.Render(p => p.Add(x => x.SearchText, "name:Beta"));
		column.Filter.Value.Should().Be("Beta");

		table.Render(p => p.Add(x => x.SearchText, string.Empty));
		column.Filter.Value.Should().BeEmpty();
	}

	/// <summary>Rows are draggable with a download url when dragging is allowed.</summary>
	[Fact]
	public void Draggable_rows_carry_the_download_url()
	{
		var table = RenderTable(p => p
			.Add(x => x.AllowDrag, true)
			.Add(x => x.DownloadUrlFunc, item => item.Id == 1 ? $"text/plain:{item.Name}.txt:http://x/{item.Id}" : null));

		Rows(table)[0].GetAttribute("draggable").Should().Be("true");
		Rows(table)[0].GetAttribute("data-downloadurl").Should().Be("text/plain:Alpha.txt:http://x/1");
		Rows(table)[1].HasAttribute("data-downloadurl").Should().BeFalse();
		table.Instance.GetRowAttributes(null).Should().ContainKey("draggable").And.HaveCount(1);
	}

	/// <summary>Dragging a row selects it and puts it in the drag context; dropping raises Drop with the target.</summary>
	[Fact]
	public void Dragging_and_dropping_rows_raises_Drop()
	{
		var drops = new List<DropEventArgs>();
		var context = new PDDragContext();
		var table = RenderInDragContext(context, p => p
			.Add(x => x.AllowDrag, true)
			.Add(x => x.AllowDrop, true)
			.Add(x => x.SelectionMode, TableSelectionMode.Multiple)
			.Add(x => x.Drop, (DropEventArgs a) => drops.Add(a)));

		Rows(table)[1].DragStart();
		table.Instance.Selection.Should().Equal("2");
		context.Payload.Should().BeEquivalentTo(new[] { _provider.Items[1] });

		Rows(table)[0].Drop(new DragEventArgs { CtrlKey = true });
		table.Find("div.pdtable").Drop();
		Rows(table)[1].DragEnd();

		drops.Should().HaveCount(2);
		drops[0].Target.Should().BeSameAs(_provider.Items[0]);
		drops[0].Ctrl.Should().BeTrue();
		drops[1].Target.Should().BeNull();
	}

	/// <summary>Dragging a row that is already selected drags the whole selection.</summary>
	[Fact]
	public void Dragging_a_selected_row_drags_the_selection()
	{
		var context = new PDDragContext();
		var table = RenderInDragContext(context, p => p
			.Add(x => x.AllowDrag, true)
			.Add(x => x.SelectionMode, TableSelectionMode.Multiple));
		MouseUp(table, 0);
		MouseUp(table, 2, ctrl: true);

		Rows(table)[2].DragStart();

		context.Payload.Should().BeEquivalentTo(new[] { _provider.Items[0], _provider.Items[2] });
	}

	/// <summary>A disabled table neither starts drags nor raises Drop.</summary>
	[Fact]
	public void A_disabled_table_ignores_drag_and_drop()
	{
		var drops = 0;
		var context = new PDDragContext();
		var table = RenderInDragContext(context, p => p
			.Add(x => x.AllowDrag, true)
			.Add(x => x.IsEnabled, false)
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.Drop, (DropEventArgs _) => drops++));

		Rows(table)[0].DragStart();
		Rows(table)[0].Drop();
		table.Find("div.pdtable").Drop();

		context.Payload.Should().BeNull();
		drops.Should().Be(0);
	}

	/// <summary>Saved column state is loaded on render and the current state can be saved back.</summary>
	[Fact]
	public async Task Column_state_is_loaded_and_saved()
	{
		var state = new StateStore();
		state.Saved["tbl"] = new TableState { Columns = { ["col-score"] = new ColumnState { Visible = false } } };
		var table = Render<PDTable<Item>>(parameters => parameters
			.Add(p => p.Id, "tbl")
			.Add(p => p.DataProvider, _provider)
			.Add(p => p.KeyField, item => item.Id)
			.AddCascadingValue<IAsyncStateManager>(state)
			.Add(p => p.ChildContent, Columns(DefaultColumns())));

		table.WaitForAssertion(() => table.FindAll("thead th").Select(th => th.Id).Should().Equal("col-name"));
		state.Initialized.Should().BeTrue();

		await table.InvokeAsync(table.Instance.SaveStateAsync);
		var saved = state.Saved["tbl"].Should().BeOfType<TableState>().Subject;
		saved.Columns.Keys.Should().BeEquivalentTo(["col-name", "col-score"]);
	}
}
