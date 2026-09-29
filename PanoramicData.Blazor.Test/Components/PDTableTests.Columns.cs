using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Column title and computed field tests for <see cref="PDTable{TItem}"/>.
/// </summary>
public partial class PDTableTests
{
	/// <summary>A title given in ColumnsConfig replaces the column's own title in the header (#172).</summary>
	[Fact]
	public void ColumnsConfig_title_is_shown_in_the_header()
	{
		var table = RenderTable(p => p.Add(x => x.ColumnsConfig,
		[
			new PDColumnConfig { Id = "col-name", Title = "Full name" },
			new PDColumnConfig { Id = "col-score" }
		]));

		table.FindAll("thead th span.text-nowrap").Select(s => s.TextContent).Should().Equal("Full name", "Score");
	}

	/// <summary>
	/// A computed field, which selects no member, renders its value under an empty title rather than
	/// throwing (#171), and is sortable through the compiled expression.
	/// </summary>
	[Fact]
	public async Task A_computed_field_renders_and_sorts()
	{
		var table = RenderTable(columns:
		[
			new Col("col-name", x => x.Name),
			new Col("col-shout", x => x.Name + "!")
		]);

		Rows(table)[0].QuerySelectorAll("td").Select(td => td.TextContent.Trim()).Should().Equal("Alpha", "Alpha!");
		var computed = table.Instance.Columns.Single(c => c.Id == "col-shout");
		computed.GetTitle().Should().BeEmpty();
		computed.Type.Should().BeNull();
		computed.PropertyInfo.Should().BeNull();

		await table.InvokeAsync(() => table.Find("th#col-shout span.pd-sort").ClickAsync(new MouseEventArgs()));

		_provider.Requests[^1].SortFieldExpression.Should().BeSameAs(computed.Field);
		string[] expected = computed.SortDirection == SortDirection.Descending ? ["Gamma", "Beta", "Alpha"] : ["Alpha", "Beta", "Gamma"];
		table.WaitForAssertion(() => Names(table).Should().Equal(expected));

		await table.InvokeAsync(() => table.Find("th#col-shout span.pd-sort").ClickAsync(new MouseEventArgs()));

		table.WaitForAssertion(() => Names(table).Should().Equal(expected.Reverse()));
	}

	/// <summary>
	/// In edit mode a computed column without an edit template stays read-only, so no value is offered
	/// that could never be written back, while member columns still get their input (#171).
	/// </summary>
	[Fact]
	public async Task A_computed_field_is_read_only_when_editing()
	{
		Exception? caught = null;
		var table = RenderEditable(
			p => p.Add(x => x.ExceptionHandler, ex => caught = ex),
			[
				new Col("col-name", x => x.Name),
				new Col("col-double", x => x.Score * 2)
			]);
		await table.InvokeAsync(() => Rows(table)[0].MouseUpAsync(new MouseEventArgs { Button = 0 }));

		await table.InvokeAsync(table.Instance.BeginEditAsync);

		table.FindAll("input.pdtable_edit").Should().ContainSingle().Which.GetAttribute("value").Should().Be("Alpha");
		table.Instance.OnEditInput("col-double", "5");
		await table.InvokeAsync(table.Instance.CommitEditAsync);

		caught.Should().BeNull();
		_provider.Items[0].Score.Should().Be(10);
	}
}
