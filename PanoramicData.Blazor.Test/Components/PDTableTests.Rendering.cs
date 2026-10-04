using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Rendering, layout and pager tests for <see cref="PDTable{TItem}"/>.
/// </summary>
public partial class PDTableTests
{
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
}
