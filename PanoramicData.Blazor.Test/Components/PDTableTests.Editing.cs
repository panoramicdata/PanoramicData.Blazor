using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// In-place editing tests for <see cref="PDTable{TItem}"/>.
/// </summary>
public partial class PDTableTests
{
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
}
