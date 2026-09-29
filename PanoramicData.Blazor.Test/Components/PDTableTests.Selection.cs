using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Mouse and keyboard selection tests for <see cref="PDTable{TItem}"/>.
/// </summary>
public partial class PDTableTests
{
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
}
