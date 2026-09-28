using System.Linq.Expressions;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDColumnGrouper{TItem}"/> presents one pill per column group of its table, switches the
/// table's active group when a pill is clicked, and follows groups registered after it rendered.
/// </summary>
public class PDColumnGrouperTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDColumnGrouperTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>
	/// The pills are All, then registered groups by ordinal, then bare string groups, each with its column count
	/// and the registered group's icon and tooltip.
	/// </summary>
	[Fact]
	public void Pills_follow_the_table_groups_with_counts()
	{
		var table = RenderTable();
		var grouper = RenderGrouper(table.Instance, p => p.Add(x => x.ShowCounts, true).Add(x => x.AllIcon, "fas fa-all"));

		var pills = grouper.FindAll("button.pdcg-pill");
		pills.Select(b => b.QuerySelector(".pdcg-text")!.TextContent).Should().Equal("All", "Metrics", "Other");
		pills.Select(b => b.QuerySelector(".pdcg-count")!.TextContent).Should().Equal("4", "2", "1");
		pills[0].QuerySelector("i.pdcg-icon")!.ClassList.Should().Contain("fa-all");
		pills[1].QuerySelector("i.pdcg-icon")!.ClassList.Should().Contain("fa-chart");
		pills[1].GetAttribute("title").Should().Be("Numbers");
		pills[2].QuerySelector("i.pdcg-icon").Should().BeNull();
		pills[0].ClassList.Should().Contain("active");
	}

	/// <summary>The segmented variant is the default and the pills variant switches the container class.</summary>
	[Theory]
	[InlineData(PDColumnGroupVariant.Segmented, "segmented")]
	[InlineData(PDColumnGroupVariant.Pills, "pills")]
	public void Variant_sets_the_container_class(PDColumnGroupVariant variant, string expected)
	{
		var table = RenderTable();
		var grouper = RenderGrouper(table.Instance, p => p.Add(x => x.Variant, variant).Add(x => x.CssClass, "mine"));

		var container = grouper.Find(".pd-column-grouper");
		container.ClassList.Should().Contain(expected).And.Contain("mine");
		container.GetAttribute("role").Should().Be("group");
	}

	/// <summary>Counts are hidden by default, the All pill can be hidden and relabelled.</summary>
	[Fact]
	public void All_pill_and_counts_are_optional()
	{
		var table = RenderTable();
		var withoutAll = RenderGrouper(table.Instance, p => p.Add(x => x.ShowAllPill, false));
		withoutAll.FindAll(".pdcg-text").Select(e => e.TextContent).Should().Equal("Metrics", "Other");
		withoutAll.FindAll(".pdcg-count").Should().BeEmpty();

		var relabelled = RenderGrouper(table.Instance, p => p.Add(x => x.AllText, "Everything"));
		relabelled.FindAll(".pdcg-text")[0].TextContent.Should().Be("Everything");
	}

	/// <summary>Clicking a pill makes it active, applies the active CSS class and filters the table's columns.</summary>
	[Fact]
	public void Clicking_a_pill_activates_its_group_on_the_table()
	{
		var table = RenderTable();
		var grouper = RenderGrouper(table.Instance, p => p
			.Add(x => x.ActivePillCssClass, "chosen")
			.Add(x => x.PillCssClass, "pill")
			.Add(x => x.TextCssClass, "txt")
			.Add(x => x.IconCssClass, "ico"));

		grouper.FindAll("button.pdcg-pill")[1].Click();

		table.Instance.ActiveColumnGroup.Should().Be("Metrics");
		var pills = grouper.FindAll("button.pdcg-pill");
		pills[1].ClassList.Should().Contain("active").And.Contain("chosen").And.Contain("pill");
		pills[0].ClassList.Should().NotContain("active");
		pills[1].QuerySelector(".pdcg-text")!.ClassList.Should().Contain("txt");
		pills[1].QuerySelector(".pdcg-icon")!.ClassList.Should().Contain("ico");
		table.WaitForAssertion(() => table.Instance.ActualColumnsToDisplay.Select(c => c.Id)
			.Should().BeEquivalentTo(["col-name", "col-a", "col-b"]));

		grouper.FindAll("button.pdcg-pill")[0].Click();
		table.Instance.ActiveColumnGroup.Should().BeNull();
	}

	/// <summary>A disabled grouper renders its pills disabled.</summary>
	[Fact]
	public void Disabled_grouper_disables_its_pills()
	{
		var table = RenderTable();
		var grouper = RenderGrouper(table.Instance, p => p.Add(x => x.IsEnabled, false));

		grouper.FindAll("button.pdcg-pill").Should().AllSatisfy(b => b.HasAttribute("disabled").Should().BeTrue());
	}

	/// <summary>An invisible grouper renders nothing.</summary>
	[Fact]
	public void Invisible_grouper_renders_nothing()
	{
		var table = RenderTable();
		var grouper = RenderGrouper(table.Instance, p => p.Add(x => x.IsVisible, false));

		grouper.FindAll(".pd-column-grouper").Should().BeEmpty();
	}

	/// <summary>Without a table the container renders with no pills.</summary>
	[Fact]
	public void No_table_renders_no_pills()
	{
		var grouper = Render<PDColumnGrouper<Row>>();

		grouper.Find(".pd-column-grouper").Children.Should().BeEmpty();
	}

	/// <summary>A group registered on the table after the grouper rendered appears as a new pill.</summary>
	[Fact]
	public async Task A_group_registered_later_adds_a_pill()
	{
		var table = RenderTable();
		var grouper = RenderGrouper(table.Instance);

		await table.InvokeAsync(() => table.Instance.RegisterColumnGroup(new ColumnGroupContext { Name = "Late", Ordinal = 1 }));

		grouper.WaitForAssertion(() => grouper.FindAll(".pdcg-text").Select(e => e.TextContent)
			.Should().Equal("All", "Late", "Metrics", "Other"));
	}

	/// <summary>
	/// Switching the grouper to another table follows the new table's groups, and the old table's changes no
	/// longer re-render it; nor do any after the grouper is disposed.
	/// </summary>
	[Fact]
	public async Task Changing_or_disposing_unsubscribes_from_the_old_table()
	{
		var first = RenderTable();
		var second = RenderTable();
		var grouper = RenderGrouper(first.Instance);

		grouper.Render(p => p.Add(x => x.Table, second.Instance));
		var renders = grouper.RenderCount;
		await first.InvokeAsync(() => first.Instance.RegisterColumnGroup(new ColumnGroupContext { Name = "Ignored" }));
		grouper.RenderCount.Should().Be(renders);

		await second.InvokeAsync(() => second.Instance.RegisterColumnGroup(new ColumnGroupContext { Name = "Seen" }));
		grouper.WaitForAssertion(() => grouper.FindAll(".pdcg-text").Select(e => e.TextContent).Should().Contain("Seen"));

		grouper.Instance.Dispose();
		renders = grouper.RenderCount;
		await second.InvokeAsync(() => second.Instance.RegisterColumnGroup(new ColumnGroupContext { Name = "AfterDispose" }));
		grouper.RenderCount.Should().Be(renders);
	}

	private IRenderedComponent<PDColumnGrouper<Row>> RenderGrouper(
		PDTable<Row> table,
		Action<ComponentParameterCollectionBuilder<PDColumnGrouper<Row>>>? configure = null)
		=> Render<PDColumnGrouper<Row>>(parameters =>
		{
			parameters.Add(p => p.Table, table);
			configure?.Invoke(parameters);
		});

	private IRenderedComponent<PDTable<Row>> RenderTable()
	{
		var provider = new ListDataProviderService<Row>([new Row("x", 1, 2, 3)]);
		var table = Render<PDTable<Row>>(parameters => parameters
			.Add(p => p.DataProvider, provider)
			.Add(p => p.ShowPager, false)
			.Add(p => p.ChildContent, Columns()));
		table.WaitForAssertion(() => table.Instance.Columns.Should().HaveCount(4));
		return table;
	}

	private static RenderFragment Columns() => builder =>
	{
		AddColumn(builder, 0, "col-name", r => r.Name, null);
		builder.OpenComponent<PDColumnGroup>(10);
		builder.AddComponentParameter(11, nameof(PDColumnGroup.Name), "Metrics");
		builder.AddComponentParameter(12, nameof(PDColumnGroup.Icon), "fas fa-chart");
		builder.AddComponentParameter(13, nameof(PDColumnGroup.Description), "Numbers");
		builder.AddComponentParameter(14, nameof(PDColumnGroup.ChildContent), (RenderFragment)(b =>
		{
			AddColumn(b, 20, "col-a", r => r.A, null);
			AddColumn(b, 30, "col-b", r => r.B, null);
		}));
		builder.CloseComponent();
		AddColumn(builder, 40, "col-c", r => r.C, "Other");
	};

	private static void AddColumn(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, int sequence, string id, Expression<Func<Row, object>> field, string? group)
	{
		builder.OpenComponent<PDColumn<Row>>(sequence);
		builder.AddComponentParameter(sequence + 1, nameof(PDColumn<Row>.Id), id);
		builder.AddComponentParameter(sequence + 2, nameof(PDColumn<Row>.Field), field);
		if (group is not null)
		{
			builder.AddComponentParameter(sequence + 3, nameof(PDColumn<Row>.Group), group);
		}

		builder.CloseComponent();
	}

	/// <summary>A row in the table under test.</summary>
	/// <param name="Name">Display name.</param>
	/// <param name="A">First metric.</param>
	/// <param name="B">Second metric.</param>
	/// <param name="C">Other value.</param>
	public sealed record Row(string Name, int A, int B, int C);
}
