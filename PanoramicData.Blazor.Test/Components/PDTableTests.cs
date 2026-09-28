using System.Linq.Expressions;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests the rendering, data loading, sorting, paging, selection, keyboard, editing, filtering, drag and drop
/// and state persistence behaviour of <see cref="PDTable{TItem}"/>.
/// </summary>
/// <remarks>
/// Refresh keeping the selection is covered separately by <see cref="PDTableRefreshSelectionTests"/>.
/// </remarks>
public class PDTableTests : BunitContext
{
	private readonly ItemProvider _provider = new();
	private readonly BunitJSModuleInterop _common;

	/// <summary>Sets up the rendering context and the table's common JavaScript module.</summary>
	public PDTableTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		_common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
	}

	#region Rendering

	/// <summary>Rows render one cell per column, keyed by the key field, with the column titles as headers.</summary>
	[Fact]
	public void Rows_and_headers_render_from_the_data_provider()
	{
		var table = RenderTable();

		var rows = Rows(table);
		rows.Select(r => r.Id).Should().Equal("1", "2", "3");
		rows[1].QuerySelectorAll("td").Select(td => td.TextContent.Trim()).Should().Equal("Beta", "20");
		table.FindAll("thead th").Select(th => th.Id).Should().Equal("col-name", "col-score");
		table.Find("div.pdtable").ClassList.Should().NotContain("disabled");
	}

	/// <summary>An empty result shows the no-data message across all columns.</summary>
	[Fact]
	public void An_empty_result_shows_the_no_data_message()
	{
		_provider.Items.Clear();
		var table = RenderTable(p => p.Add(x => x.NoDataMessage, "Nothing here"), waitForRows: false);

		table.WaitForAssertion(() => table.Find("tbody td").TextContent.Trim().Should().Be("Nothing here"));
		table.Find("tbody td").GetAttribute("colspan").Should().Be("2");
	}

	/// <summary>A disabled table carries the disabled class and ignores row clicks.</summary>
	[Fact]
	public void A_disabled_table_is_marked_and_ignores_clicks()
	{
		var clicks = 0;
		var table = RenderTable(p => p
			.Add(x => x.IsEnabled, false)
			.Add(x => x.CssClass, "mine")
			.Add(x => x.Click, (Item _) => clicks++));

		table.Find("div.pdtable").ClassList.Should().Contain("disabled").And.Contain("mine");
		Rows(table)[0].Click();
		clicks.Should().Be(0);
	}

	/// <summary>Clicking and double-clicking a row raise Click and DoubleClick with the row's item.</summary>
	[Fact]
	public void Row_click_and_double_click_raise_callbacks()
	{
		var clicked = new List<string>();
		var table = RenderTable(p => p
			.Add(x => x.Click, (Item item) => clicked.Add($"click {item.Name}"))
			.Add(x => x.DoubleClick, (Item item) => clicked.Add($"double {item.Name}")));

		Rows(table)[0].Click();
		Rows(table)[1].DoubleClick();

		clicked.Should().Equal("click Alpha", "double Beta");
	}

	/// <summary>Row classes combine the selection, disabled state and the RowClass function.</summary>
	[Fact]
	public void Row_classes_reflect_selection_enabled_state_and_row_class()
	{
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.RowIsEnabled, item => item.Id != 3)
			.Add(x => x.RowClass, item => item.Id == 2 ? "special" : string.Empty));

		Rows(table)[0].MouseUp(new MouseEventArgs { Button = 0 });

		var rows = Rows(table);
		rows[0].ClassList.Should().Contain("selected");
		rows[1].ClassList.Should().Contain("special").And.NotContain("selected");
		rows[2].ClassList.Should().Contain("disabled");
	}

	/// <summary>
	/// Cells are unselectable by default; a column may opt back in, carries its td and th classes, a header
	/// template, help text and a copy button.
	/// </summary>
	[Fact]
	public void Column_presentation_options_are_rendered()
	{
		var table = RenderTable(columns:
		[
			new Col("col-name", x => x.Name)
			{
				Extra =
				{
					[nameof(PDColumn<Item>.TdClass)] = "td-x",
					[nameof(PDColumn<Item>.ThClass)] = "th-x",
					[nameof(PDColumn<Item>.HelpText)] = "The name",
					[nameof(PDColumn<Item>.ShowCopyButton)] = (Func<Item?, bool>)(_ => true)
				}
			},
			new Col("col-score", x => x.Score)
			{
				Extra =
				{
					[nameof(PDColumn<Item>.UserSelectable)] = true,
					[nameof(PDColumn<Item>.HeaderTemplate)] = (RenderFragment)(b => b.AddMarkupContent(0, "<em>Points</em>")),
					[nameof(PDColumn<Item>.Template)] = (RenderFragment<Item>)(item => b => b.AddContent(0, $"#{item.Score}"))
				}
			}
		]);

		var nameHeader = table.Find("th#col-name");
		nameHeader.ClassList.Should().Contain("th-x").And.Contain("noselect");
		nameHeader.QuerySelector("span.text-nowrap")!.GetAttribute("title").Should().Be("The name");
		table.Find("th#col-score").ClassList.Should().NotContain("noselect");
		table.Find("th#col-score em").TextContent.Should().Be("Points");
		var cells = Rows(table)[0].QuerySelectorAll("td");
		cells[0].ClassList.Should().Contain("td-x").And.Contain("noselect");
		cells[1].TextContent.Trim().Should().Be("#10");
		table.FindComponents<PDClipboard>().Should().HaveCount(3);
	}

	/// <summary>A scrolling table wraps its content in a container with the maximum height and a sticky header.</summary>
	[Fact]
	public void MaxHeight_renders_a_scrolling_container_with_sticky_header()
	{
		var table = RenderTable(p => p
			.Add(x => x.MaxHeight, "300px")
			.Add(x => x.StickyHeader, true)
			.Add(x => x.StickyPager, true)
			.Add(x => x.ShowPager, true)
			.Add(x => x.PagerPosition, PagerPositions.Bottom)
			.Add(x => x.PageCriteria, new PageCriteria(1, 10))
			.Add(x => x.PagerCssClass, "pager-x"));

		var container = table.Find(".pdtable-container");
		container.GetAttribute("style").Should().Contain("max-height: 300px");
		table.Find("thead").ClassList.Should().Contain("pdtable-sticky-header");
		table.FindAll("nav.pdpager").Should().ContainSingle();
		container.QuerySelector("nav.pdpager.pager-x").Should().NotBeNull();
		container.QuerySelector(".pdtable")!.Id.Should().Be(table.Instance.Id);
	}

	/// <summary>A scrolling table without a sticky pager puts the pager after the container.</summary>
	[Fact]
	public void MaxHeight_without_sticky_pager_places_the_pager_outside()
	{
		var table = RenderTable(p => p
			.Add(x => x.MaxHeight, "300px")
			.Add(x => x.ShowPager, true)
			.Add(x => x.PagerPosition, PagerPositions.Bottom)
			.Add(x => x.PagerCssClass, "pager-x")
			.Add(x => x.PageCriteria, new PageCriteria(1, 10)));

		table.FindAll("nav.pdpager").Should().ContainSingle();
		table.Find(".pdtable-container").QuerySelector("nav.pdpager").Should().BeNull();
		table.Find("nav.pdpager").ClassList.Should().Contain("pager-x");
		table.Find("thead").ClassList.Should().NotContain("pdtable-sticky-header");
	}

	/// <summary>The pager is rendered above, below or on both sides according to the pager position.</summary>
	[Theory]
	[InlineData(PagerPositions.Top, 1)]
	[InlineData(PagerPositions.Bottom, 1)]
	[InlineData(PagerPositions.Both, 2)]
	public void Pager_position_controls_how_many_pagers_render(PagerPositions position, int expected)
	{
		var table = RenderTable(p => p
			.Add(x => x.ShowPager, true)
			.Add(x => x.PagerPosition, position)
			.Add(x => x.PageCriteria, new PageCriteria(1, 10)));

		table.FindComponents<PDPager>().Should().HaveCount(expected);
	}

	/// <summary>ColumnsConfig selects and orders the columns shown, ignoring ids it does not know.</summary>
	[Fact]
	public void ColumnsConfig_selects_and_orders_columns()
	{
		var table = RenderTable(p => p.Add(x => x.ColumnsConfig,
		[
			new PDColumnConfig { Id = "col-score" },
			new PDColumnConfig { Id = "col-missing" },
			new PDColumnConfig { Id = "col-name" }
		]));

		table.FindAll("thead th").Select(th => th.Id).Should().Equal("col-score", "col-name");
		Rows(table)[0].QuerySelectorAll("td").Select(td => td.TextContent.Trim()).Should().Equal("10", "Alpha");
	}

	/// <summary>A column registered with a default id is given one derived from its title.</summary>
	[Fact]
	public void A_default_column_id_is_replaced_with_one_from_the_title()
	{
		var table = RenderTable(columns: [new Col(null, x => x.Name) { Extra = { [nameof(PDColumn<Item>.Title)] = "Full Name!" } }]);

		table.Instance.Columns.Single().Id.Should().Be("col-fullname");
	}

	/// <summary>Selection without a key field is refused with a clear exception.</summary>
	[Fact]
	public void Selection_without_a_key_field_is_refused()
	{
		var act = () => Render<PDTable<Item>>(parameters => parameters
			.Add(p => p.DataProvider, _provider)
			.Add(p => p.SelectionMode, TableSelectionMode.Single));

		act.Should().Throw<PDTableException>().WithMessage("*KeyField*");
	}

	#endregion

	#region Data loading

	/// <summary>BeforeFetch, AfterFetch, Ready and ItemsLoaded are all called around the first load.</summary>
	[Fact]
	public void The_first_load_raises_its_callbacks_and_lets_items_be_modified()
	{
		var events = new List<string>();
		var table = RenderTable(p => p
			.Add(x => x.BeforeFetch, () => events.Add("before"))
			.Add(x => x.AfterFetch, () => events.Add("after"))
			.Add(x => x.Ready, () => events.Add("ready"))
			.Add(x => x.ItemsLoaded, items => items.RemoveAt(0)));

		events.Should().Equal("before", "after", "ready");
		table.Instance.ItemsToDisplay.Select(i => i.Name).Should().Equal("Beta", "Gamma");
		table.Instance.IsBusy.Should().BeFalse();
		table.Instance.IsCancelled.Should().BeFalse();
	}

	/// <summary>With AutoLoad off nothing is fetched until a refresh is asked for.</summary>
	[Fact]
	public async Task AutoLoad_off_waits_for_a_refresh()
	{
		var ready = 0;
		var table = RenderTable(p => p
			.Add(x => x.AutoLoad, false)
			.Add(x => x.Ready, () => ready++), waitForRows: false);

		ready.Should().Be(1);
		_provider.Requests.Should().BeEmpty();

		await table.InvokeAsync(table.Instance.RefreshAsync);
		table.Instance.ItemsToDisplay.Should().HaveCount(3);
	}

	/// <summary>A provider failure is passed to the exception handler rather than thrown.</summary>
	[Fact]
	public void A_provider_failure_reaches_the_exception_handler()
	{
		_provider.Failure = new InvalidOperationException("boom");
		Exception? caught = null;
		RenderTable(p => p.Add(x => x.ExceptionHandler, ex => caught = ex), waitForRows: false);

		caught.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("boom");
	}

	/// <summary>A table with no data provider reports the problem through the exception handler.</summary>
	[Fact]
	public void A_missing_data_provider_reaches_the_exception_handler()
	{
		var caught = new List<Exception>();
		Render<PDTable<Item>>(parameters => parameters
			.Add(p => p.ExceptionHandler, ex => caught.Add(ex)));

		caught.Should().NotBeEmpty();
	}

	/// <summary>HandleExceptionAsync logs and forwards an exception to the handler.</summary>
	[Fact]
	public async Task HandleExceptionAsync_forwards_to_the_handler()
	{
		Exception? caught = null;
		var table = RenderTable(p => p.Add(x => x.ExceptionHandler, ex => caught = ex));
		var error = new InvalidOperationException("forwarded");

		await table.InvokeAsync(() => table.Instance.HandleExceptionAsync(error));

		caught.Should().BeSameAs(error);
	}

	/// <summary>The search text parameter is passed to the provider.</summary>
	[Fact]
	public void SearchText_is_sent_to_the_provider()
	{
		RenderTable(p => p.Add(x => x.SearchText, "Be"));

		_provider.Requests.Last().SearchText.Should().Be("Be");
	}

	/// <summary>Enable, Disable, SetEnabled and SetStateHasChanged update the rendered state.</summary>
	[Fact]
	public async Task Enable_and_disable_update_the_markup()
	{
		var table = RenderTable();

		await table.InvokeAsync(table.Instance.Disable);
		table.Find("div.pdtable").ClassList.Should().Contain("disabled");
		await table.InvokeAsync(table.Instance.Enable);
		table.Find("div.pdtable").ClassList.Should().NotContain("disabled");
		await table.InvokeAsync(() => table.Instance.SetEnabled(false));
		await table.InvokeAsync(table.Instance.SetStateHasChanged);
		table.Find("div.pdtable").ClassList.Should().Contain("disabled");
	}

	/// <summary>CancelAsync cancels a fetch in progress, which the provider sees through its token.</summary>
	[Fact]
	public async Task CancelAsync_cancels_the_fetch_in_progress()
	{
		var table = RenderTable();
		_provider.Gate = new TaskCompletionSource();

		var refresh = table.InvokeAsync(table.Instance.RefreshAsync);
		table.WaitForAssertion(() => table.Instance.IsBusy.Should().BeTrue());
		await table.InvokeAsync(table.Instance.CancelAsync);
		table.Instance.IsCancelled.Should().BeTrue();

		_provider.Gate.SetResult();
		await refresh;
		_provider.LastToken.IsCancellationRequested.Should().BeTrue();
		table.Instance.IsBusy.Should().BeFalse();
	}

	/// <summary>CancelAsync with nothing in progress does nothing.</summary>
	[Fact]
	public async Task CancelAsync_when_idle_does_nothing()
	{
		var table = RenderTable();

		await table.InvokeAsync(table.Instance.CancelAsync);

		table.Instance.IsCancelled.Should().BeFalse();
	}

	#endregion

	#region Sorting

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

	#endregion

	#region Paging

	/// <summary>Changing the page on the page criteria refetches that page and raises PageChanged.</summary>
	[Fact]
	public async Task Changing_the_page_refetches_and_raises_PageChanged()
	{
		var criteria = new PageCriteria(1, 2);
		var pages = new List<uint>();
		var table = RenderTable(p => p
			.Add(x => x.PageCriteria, criteria)
			.Add(x => x.PageChanged, c => pages.Add(c.Page)));
		criteria.TotalCount.Should().Be(3);
		Names(table).Should().Equal("Alpha", "Beta");

		await table.InvokeAsync(() => criteria.Page = 2);

		table.WaitForAssertion(() => Names(table).Should().Equal("Gamma"));
		pages.Should().Equal(2u);
	}

	/// <summary>Changing the page size refetches and raises PageSizeChanged.</summary>
	[Fact]
	public async Task Changing_the_page_size_refetches_and_raises_PageSizeChanged()
	{
		var criteria = new PageCriteria(1, 2);
		var sizes = new List<uint>();
		var table = RenderTable(p => p
			.Add(x => x.PageCriteria, criteria)
			.Add(x => x.PageSizeChanged, c => sizes.Add(c.PageSize)));

		await table.InvokeAsync(() => criteria.PageSize = 10);

		table.WaitForAssertion(() => Names(table).Should().HaveCount(3));
		sizes.Should().Equal(10u);
	}

	/// <summary>After disposal the table no longer follows its page criteria.</summary>
	[Fact]
	public async Task Disposal_unsubscribes_from_the_page_criteria()
	{
		var criteria = new PageCriteria(1, 2);
		var table = RenderTable(p => p.Add(x => x.PageCriteria, criteria));
		var requests = _provider.Requests.Count;

		await table.Instance.DisposeAsync();
		criteria.Page = 2;

		_provider.Requests.Should().HaveCount(requests);
	}

	#endregion

	#region Selection

	/// <summary>In single mode a click selects a row, another click replaces it, and a repeat click does nothing.</summary>
	[Fact]
	public void Single_selection_selects_and_replaces()
	{
		var changes = 0;
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.SelectionChanged, () => changes++));

		MouseUp(table, 0);
		MouseUp(table, 1);
		MouseUp(table, 1);

		table.Instance.Selection.Should().Equal("2");
		changes.Should().Be(2);
		table.Instance.GetSelectedItems().Single().Name.Should().Be("Beta");
		table.Instance.IsSelected(_provider.Items[1]).Should().BeTrue();
		table.Instance.IsSelected(_provider.Items[0]).Should().BeFalse();
	}

	/// <summary>In multiple mode ctrl toggles rows, shift selects a range and a plain click selects one row.</summary>
	[Fact]
	public void Multiple_selection_supports_ctrl_shift_and_plain_clicks()
	{
		var table = RenderTable(p => p.Add(x => x.SelectionMode, TableSelectionMode.Multiple));

		MouseUp(table, 0);
		MouseUp(table, 2, shift: true);
		table.Instance.Selection.Should().Equal("1", "2", "3");

		MouseUp(table, 1, ctrl: true);
		table.Instance.Selection.Should().Equal("1", "3");
		MouseUp(table, 1, ctrl: true);
		table.Instance.Selection.Should().Contain("2");

		MouseUp(table, 0);
		table.Instance.Selection.Should().Equal("1");
	}

	/// <summary>A right click selects an unselected row, but not when right-click selection is turned off.</summary>
	[Fact]
	public void Right_click_selects_only_when_enabled()
	{
		var table = RenderTable(p => p.Add(x => x.SelectionMode, TableSelectionMode.Single));
		MouseUp(table, 1, button: 2);
		table.Instance.Selection.Should().Equal("2");

		var other = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.RightClickSelectsRow, false));
		MouseUp(other, 1, button: 2);
		other.Instance.Selection.Should().BeEmpty();
	}

	/// <summary>Disabled rows, a disabled table and selection mode None never select.</summary>
	[Fact]
	public void Rows_that_cannot_be_selected_are_ignored()
	{
		var rowDisabled = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.RowIsEnabled, item => item.Id != 1));
		MouseUp(rowDisabled, 0);
		rowDisabled.Instance.Selection.Should().BeEmpty();

		var tableDisabled = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.IsEnabled, false));
		MouseUp(tableDisabled, 0);
		tableDisabled.Instance.Selection.Should().BeEmpty();

		var none = RenderTable();
		MouseUp(none, 0);
		none.Instance.Selection.Should().BeEmpty();
		none.Instance.IsSelected(_provider.Items[0]).Should().BeFalse();
	}

	/// <summary>A mouse down on the table area outside any row clears the selection.</summary>
	[Fact]
	public void Mouse_down_outside_the_rows_clears_the_selection()
	{
		var changes = 0;
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.SelectionChanged, () => changes++));
		MouseUp(table, 0);

		table.Find("table").MouseDown();
		table.Instance.Selection.Should().Equal("1");

		table.Find("div.pdtable").MouseDown();
		table.Instance.Selection.Should().BeEmpty();
		changes.Should().Be(2);
	}

	/// <summary>A right mouse down outside the rows keeps the selection when right-click selection is off.</summary>
	[Fact]
	public void Right_mouse_down_outside_keeps_the_selection_when_right_click_is_off()
	{
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.RightClickSelectsRow, false));
		MouseUp(table, 0);

		table.Find("div.pdtable").MouseDown(new MouseEventArgs { Button = 2 });

		table.Instance.Selection.Should().Equal("1");
	}

	/// <summary>The select-all checkbox selects and deselects every enabled row.</summary>
	[Fact]
	public void Select_all_checkbox_toggles_enabled_rows()
	{
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Multiple)
			.Add(x => x.ShowCheckboxes, true)
			.Add(x => x.RowIsEnabled, item => item.Id != 3));

		table.Find("thead input[type=checkbox]").Input(true);
		table.Instance.Selection.Should().Equal("1", "2");
		table.FindAll("tbody input[type=checkbox]")[2].HasAttribute("disabled").Should().BeTrue();

		table.Find("thead input[type=checkbox]").Input(false);
		table.Instance.Selection.Should().BeEmpty();
	}

	/// <summary>A row checkbox selects and deselects its row.</summary>
	[Fact]
	public void Row_checkbox_toggles_its_row()
	{
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Multiple)
			.Add(x => x.ShowCheckboxes, true));

		table.FindAll("tbody input[type=checkbox]")[1].Input(true);
		table.Instance.Selection.Should().Equal("2");
		table.FindAll("tbody input[type=checkbox]")[1].HasAttribute("checked").Should().BeTrue();

		table.FindAll("tbody input[type=checkbox]")[1].Input(false);
		table.Instance.Selection.Should().BeEmpty();
	}

	/// <summary>ClearSelectionAsync empties the selection and SelectItemAsync ignores a blank key.</summary>
	[Fact]
	public async Task Clear_and_blank_keys()
	{
		var changes = 0;
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.SelectionChanged, () => changes++));

		await table.InvokeAsync(() => table.Instance.SelectItemAsync(" "));
		await table.InvokeAsync(table.Instance.ClearSelectionAsync);
		changes.Should().Be(0);

		await table.InvokeAsync(() => table.Instance.SelectItemAsync("3"));
		await table.InvokeAsync(table.Instance.ClearSelectionAsync);
		table.Instance.Selection.Should().BeEmpty();
		changes.Should().Be(2);
	}

	/// <summary>Without a key field no items are reported as selected.</summary>
	[Fact]
	public void GetSelectedItems_without_a_key_field_is_empty()
	{
		var table = Render<PDTable<Item>>(parameters => parameters.Add(p => p.DataProvider, _provider));

		table.Instance.GetSelectedItems().Should().BeEmpty();
	}

	#endregion

	#region Keyboard

	/// <summary>The arrow, home and end keys move a single selection and scroll the new row into view.</summary>
	[Fact]
	public void Navigation_keys_move_the_selection()
	{
		var keys = new List<string>();
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.KeyDown, (KeyboardEventArgs a) => keys.Add(a.Code)));
		MouseUp(table, 0);

		Key(table, "ArrowDown");
		table.Instance.Selection.Should().Equal("2");
		Key(table, "End");
		table.Instance.Selection.Should().Equal("3");
		Key(table, "ArrowUp");
		table.Instance.Selection.Should().Equal("2");
		Key(table, "Home");
		table.Instance.Selection.Should().Equal("1");

		keys.Should().Equal("ArrowDown", "End", "ArrowUp", "Home");
		_common.Invocations["scrollIntoView"].Should().HaveCount(4);
	}

	/// <summary>Keys that would move past either end, or by a page on a short list, leave the selection alone.</summary>
	[Fact]
	public void Navigation_keys_at_the_ends_keep_the_selection()
	{
		var table = RenderTable(p => p.Add(x => x.SelectionMode, TableSelectionMode.Single));
		MouseUp(table, 0);

		Key(table, "ArrowUp");
		Key(table, "Home");
		Key(table, "PageDown");
		table.Instance.Selection.Should().Equal("1");

		MouseUp(table, 2);
		Key(table, "ArrowDown");
		Key(table, "End");
		Key(table, "PageUp");
		table.Instance.Selection.Should().Equal("3");
	}

	/// <summary>Navigation with nothing selected does nothing; ctrl+A selects every row in multiple mode.</summary>
	[Fact]
	public void Ctrl_A_selects_all_in_multiple_mode()
	{
		var table = RenderTable(p => p.Add(x => x.SelectionMode, TableSelectionMode.Multiple));

		Key(table, "ArrowDown");
		table.Instance.Selection.Should().BeEmpty();

		Key(table, "KeyA");
		table.Instance.Selection.Should().BeEmpty();

		Key(table, "KeyA", ctrl: true);
		table.Instance.Selection.Should().Equal("1", "2", "3");
	}

	/// <summary>A disabled table ignores navigation keys but still reports the key press.</summary>
	[Fact]
	public void A_disabled_table_ignores_keys_but_reports_them()
	{
		var keys = 0;
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Multiple)
			.Add(x => x.IsEnabled, false)
			.Add(x => x.KeyDown, (KeyboardEventArgs _) => keys++));

		Key(table, "KeyA", ctrl: true);

		table.Instance.Selection.Should().BeEmpty();
		keys.Should().Be(1);
	}

	#endregion

	#region Editing

	/// <summary>
	/// F2 begins editing the selected row: BeforeEdit is raised, editable columns get inputs and the first is
	/// focused; Enter commits the typed value to the item and raises AfterEdit and AfterEditCommitted.
	/// </summary>
	[Fact]
	public void F2_edits_and_Enter_commits()
	{
		var events = new List<string>();
		var table = RenderEditable(p => p
			.Add(x => x.BeforeEdit, (TableBeforeEditEventArgs<Item> a) => events.Add($"before {a.Item.Name}"))
			.Add(x => x.AfterEdit, (TableAfterEditEventArgs<Item> a) => events.Add($"after {a.NewValues["col-name"]}"))
			.Add(x => x.AfterEditCommitted, (TableAfterEditCommittedEventArgs<Item> _) => events.Add("committed")));
		MouseUp(table, 0);

		Key(table, "F2");
		table.Instance.IsEditing.Should().BeTrue();
		table.Instance.EditItem.Should().BeSameAs(_provider.Items[0]);
		var input = table.Find("input.pdtable_edit");
		input.GetAttribute("value").Should().Be("Alpha");
		input.GetAttribute("type").Should().Be("text");
		table.WaitForAssertion(() => _common.Invocations["selectText"].Should().ContainSingle());

		input.Input("Alef");
		Key(table, "Enter");

		_provider.Items[0].Name.Should().Be("Alef");
		table.Instance.IsEditing.Should().BeFalse();
		table.Instance.EditItem.Should().BeNull();
		events.Should().Equal("before Alpha", "after Alef", "committed");
		_common.Invocations["focus"].Should().NotBeEmpty();
	}

	/// <summary>With SaveChanges the committed values are sent to the provider by property name.</summary>
	[Fact]
	public void SaveChanges_sends_the_delta_to_the_provider()
	{
		IDictionary<string, object?>? committed = null;
		var table = RenderEditable(p => p
			.Add(x => x.SaveChanges, true)
			.Add(x => x.AfterEditCommitted, (TableAfterEditCommittedEventArgs<Item> a) => committed = a.NewValues));
		MouseUp(table, 1);
		Key(table, "F2");

		table.FindAll("input.pdtable_edit")[1].Input("99");
		Key(table, "Return");

		_provider.Items[1].Score.Should().Be(99);
		_provider.Updates.Should().ContainSingle().Which.Should().ContainKey(nameof(Item.Score));
		committed.Should().ContainKey(nameof(Item.Score));
	}

	/// <summary>Escape abandons an edit without changing the item.</summary>
	[Fact]
	public void Escape_cancels_the_edit()
	{
		var table = RenderEditable();
		MouseUp(table, 0);
		Key(table, "F2");

		table.Find("input.pdtable_edit").Input("Changed");
		Key(table, "Escape");

		table.Instance.IsEditing.Should().BeFalse();
		_provider.Items[0].Name.Should().Be("Alpha");
		table.FindAll("input.pdtable_edit").Should().BeEmpty();
	}

	/// <summary>Cancelling in BeforeEdit prevents editing, and CancelEdit when not editing does nothing.</summary>
	[Fact]
	public async Task BeforeEdit_can_cancel_the_edit()
	{
		var table = RenderEditable(p => p.Add(x => x.BeforeEdit, (TableBeforeEditEventArgs<Item> a) => a.Cancel = true));
		MouseUp(table, 0);

		Key(table, "F2");
		await table.InvokeAsync(table.Instance.CancelEdit);

		table.Instance.IsEditing.Should().BeFalse();
	}

	/// <summary>Cancelling in AfterEdit leaves the item unchanged but still ends the edit.</summary>
	[Fact]
	public void AfterEdit_can_cancel_applying_the_values()
	{
		var table = RenderEditable(p => p.Add(x => x.AfterEdit, (TableAfterEditEventArgs<Item> a) => a.Cancel = true));
		MouseUp(table, 0);
		Key(table, "F2");

		table.Find("input.pdtable_edit").Input("Ignored");
		Key(table, "Enter");

		_provider.Items[0].Name.Should().Be("Alpha");
		table.Instance.IsEditing.Should().BeFalse();
	}

	/// <summary>A value that cannot be converted to the property type reaches the exception handler.</summary>
	[Fact]
	public void An_unconvertible_value_reaches_the_exception_handler()
	{
		Exception? caught = null;
		var table = RenderEditable(p => p.Add(x => x.ExceptionHandler, ex => caught = ex));
		MouseUp(table, 0);
		Key(table, "F2");

		table.FindAll("input.pdtable_edit")[1].Input("not a number");
		Key(table, "Enter");

		caught.Should().NotBeNull();
		_provider.Items[0].Score.Should().Be(10);
	}

	/// <summary>
	/// Non-editable columns get no input, ColumnsConfig can make them editable, a password column uses a
	/// password input and an edit template replaces the input.
	/// </summary>
	[Fact]
	public void Column_edit_options_are_honoured()
	{
		var table = RenderEditable(
			p => p.Add(x => x.ColumnsConfig, [new PDColumnConfig { Id = "col-name", Editable = true }, new PDColumnConfig { Id = "col-score" }]),
			[
				new Col("col-name", x => x.Name) { Extra = { [nameof(PDColumn<Item>.Editable)] = false, [nameof(PDColumn<Item>.IsPassword)] = true } },
				new Col("col-score", x => x.Score) { Extra = { [nameof(PDColumn<Item>.EditTemplate)] = (RenderFragment<Item?>)(_ => b => b.AddMarkupContent(0, "<b class=\"tpl\">T</b>")) } }
			]);
		MouseUp(table, 0);

		Key(table, "F2");

		table.FindAll("input.pdtable_edit").Should().ContainSingle().Which.GetAttribute("type").Should().Be("password");
		table.Find("b.tpl").TextContent.Should().Be("T");
	}

	/// <summary>With EditOnDoubleClick a double click begins the edit.</summary>
	[Fact]
	public void EditOnDoubleClick_begins_the_edit()
	{
		var table = RenderEditable(p => p.Add(x => x.EditOnDoubleClick, true));
		MouseUp(table, 0);

		Rows(table)[0].DoubleClick();

		table.Instance.IsEditing.Should().BeTrue();
	}

	/// <summary>Clicking the already selected row starts editing after a short delay.</summary>
	[Fact]
	public void Clicking_the_selected_row_begins_the_edit()
	{
		var table = RenderEditable();
		MouseUp(table, 0);

		MouseUp(table, 0);

		table.WaitForAssertion(() => table.Instance.IsEditing.Should().BeTrue());
	}

	/// <summary>Leaving the editors commits the edit; moving focus to another editor keeps it open.</summary>
	[Fact]
	public async Task Blur_commits_unless_focus_moves_to_another_editor()
	{
		var focused = _common.Setup<string>("getFocusedElementId");
		focused.SetResult("pd-table-edit--0-col-score");
		var table = RenderEditable();
		MouseUp(table, 0);
		Key(table, "F2");

		table.Find("input.pdtable_edit").Input("Kept");
		await table.InvokeAsync(table.Instance.OnEditBlurAsync);
		table.Instance.IsEditing.Should().BeTrue();

		_common.Setup<string>("getFocusedElementId").SetResult("elsewhere");
		await table.InvokeAsync(table.Instance.OnEditBlurAsync);
		table.Instance.IsEditing.Should().BeFalse();
		_provider.Items[0].Name.Should().Be("Kept");
	}

	/// <summary>An edit value can be supplied by column id.</summary>
	[Fact]
	public async Task OnEditInput_by_column_id_is_committed()
	{
		var table = RenderEditable();
		MouseUp(table, 0);
		Key(table, "F2");

		table.Instance.OnEditInput("col-name", "ById");
		await table.InvokeAsync(table.Instance.CommitEditAsync);

		_provider.Items[0].Name.Should().Be("ById");
	}

	/// <summary>Editing is not begun when nothing is selected or editing is not allowed.</summary>
	[Fact]
	public async Task BeginEdit_needs_a_single_selection_and_permission()
	{
		var table = RenderEditable();
		await table.InvokeAsync(table.Instance.BeginEditAsync);
		table.Instance.IsEditing.Should().BeFalse();

		var readOnly = RenderTable(p => p.Add(x => x.SelectionMode, TableSelectionMode.Single));
		MouseUp(readOnly, 0);
		Key(readOnly, "F2");
		readOnly.Instance.IsEditing.Should().BeFalse();
	}

	#endregion

	#region Filtering

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

	#endregion

	#region Drag and drop

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

	#endregion

	#region State

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

	#endregion

	private IRenderedComponent<PDTable<Item>> RenderEditable(
		Action<ComponentParameterCollectionBuilder<PDTable<Item>>>? configure = null,
		Col[]? columns = null)
		=> RenderTable(p =>
		{
			p.Add(x => x.AllowEdit, true).Add(x => x.SelectionMode, TableSelectionMode.Single);
			configure?.Invoke(p);
		}, columns: columns);

	private IRenderedComponent<PDTable<Item>> RenderInDragContext(PDDragContext context, Action<ComponentParameterCollectionBuilder<PDTable<Item>>> configure)
		=> RenderTable(p =>
		{
			p.AddCascadingValue(context);
			configure(p);
		});
	private static Col FilterColumn()
		=> new("col-name", x => x.Name) { Extra = { [nameof(PDColumn<Item>.Filterable)] = true, [nameof(PDColumn<Item>.FilterKey)] = "name" } };

	private static void MouseUp(IRenderedComponent<PDTable<Item>> table, int row, bool ctrl = false, bool shift = false, long button = 0)
		=> Rows(table)[row].MouseUp(new MouseEventArgs { Button = button, CtrlKey = ctrl, ShiftKey = shift });

	private static void Key(IRenderedComponent<PDTable<Item>> table, string code, bool ctrl = false)
		=> table.Find("div.pdtable").KeyDown(new KeyboardEventArgs { Code = code, CtrlKey = ctrl });

	private IRenderedComponent<PDTable<Item>> RenderTable(
		Action<ComponentParameterCollectionBuilder<PDTable<Item>>>? configure = null,
		bool waitForRows = true,
		Col[]? columns = null)
	{
		var table = Render<PDTable<Item>>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, _provider)
				.Add(p => p.KeyField, item => item.Id)
				.Add(p => p.ChildContent, Columns(columns ?? DefaultColumns()));
			configure?.Invoke(parameters);
		});

		if (waitForRows)
		{
			table.WaitForAssertion(() => table.FindAll("tbody tr.pdtablerow").Should().NotBeEmpty());
		}

		return table;
	}

	private static Col[] DefaultColumns() => [new Col("col-name", x => x.Name), new Col("col-score", x => x.Score)];

	private static RenderFragment Columns(Col[] columns) => builder =>
	{
		foreach (var column in columns)
		{
			AddColumn(builder, column);
		}
	};

	private static void AddColumn(RenderTreeBuilder builder, Col column)
	{
		builder.OpenComponent<PDColumn<Item>>(0);
		builder.SetKey(column);
		if (column.Id is not null)
		{
			builder.AddComponentParameter(1, nameof(PDColumn<Item>.Id), column.Id);
		}

		if (column.Field is not null)
		{
			builder.AddComponentParameter(2, nameof(PDColumn<Item>.Field), column.Field);
		}

		foreach (var (name, value) in column.Extra)
		{
			builder.AddComponentParameter(3, name, value);
		}

		builder.CloseComponent();
	}

	private static List<AngleSharp.Dom.IElement> Rows(IRenderedComponent<PDTable<Item>> table)
		=> [.. table.FindAll("tbody tr.pdtablerow")];

	private static List<string> Names(IRenderedComponent<PDTable<Item>> table)
		=> [.. table.Instance.ItemsToDisplay.Select(i => i.Name)];

	/// <summary>A column to render: its id, field and any further parameters.</summary>
	/// <param name="Id">The column id, or null to let the column choose.</param>
	/// <param name="Field">The field the column shows.</param>
	private sealed record Col(string? Id, Expression<Func<Item, object>>? Field)
	{
		public Dictionary<string, object?> Extra { get; } = [];
	}

	/// <summary>An in-memory state store.</summary>
	private sealed class StateStore : IAsyncStateManager
	{
		public Dictionary<string, object> Saved { get; } = [];

		public bool Initialized { get; private set; }

		public Task InitializeAsync()
		{
			Initialized = true;
			return Task.CompletedTask;
		}

		public Task<T?> LoadStateAsync<T>(string key)
			=> Task.FromResult(Saved.TryGetValue(key, out var value) ? (T?)value : default);

		public Task RemoveStateAsync(string key)
		{
			Saved.Remove(key);
			return Task.CompletedTask;
		}

		public Task SaveStateAsync(string key, object state)
		{
			Saved[key] = state;
			return Task.CompletedTask;
		}
	}

	/// <summary>A row in the table under test.</summary>
	public sealed class Item
	{
		/// <summary>Gets or sets the key.</summary>
		public int Id { get; set; }

		/// <summary>Gets or sets the display name.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets a numeric value.</summary>
		public int Score { get; set; }
	}

	/// <summary>An in-memory provider that honours sort and paging and records what it is asked.</summary>
	private sealed class ItemProvider : DataProviderBase<Item>
	{
		public List<Item> Items { get; } =
		[
			new() { Id = 1, Name = "Alpha", Score = 10 },
			new() { Id = 2, Name = "Beta", Score = 20 },
			new() { Id = 3, Name = "Gamma", Score = 30 }
		];

		public List<DataRequest<Item>> Requests { get; } = [];

		public List<IDictionary<string, object?>> Updates { get; } = [];

		public Exception? Failure { get; set; }

		public TaskCompletionSource? Gate { get; set; }

		public CancellationToken LastToken { get; private set; }

		public override async Task<DataResponse<Item>> GetDataAsync(DataRequest<Item> request, CancellationToken cancellationToken)
		{
			Requests.Add(request);
			LastToken = cancellationToken;
			if (Gate is { } gate)
			{
				await gate.Task;
			}

			if (Failure is { } failure)
			{
				throw failure;
			}

			IEnumerable<Item> rows = Items;
			if (request.SortFieldExpression is not null)
			{
				var key = request.SortFieldExpression.Compile();
				rows = request.SortDirection == SortDirection.Descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
			}

			var all = rows.ToList();
			var page = all.Skip(request.Skip ?? 0).Take(request.Take ?? all.Count);
			return new DataResponse<Item>([.. page], all.Count);
		}

		public override Task<OperationResponse> UpdateAsync(Item item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Updates.Add(delta);
			return Task.FromResult(new OperationResponse { Success = Items.Contains(item) });
		}
	}
}
