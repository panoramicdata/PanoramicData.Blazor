using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDColumnGroup"/> cascades its facet metadata to the columns it wraps, renders no
/// markup of its own, and keeps the cascaded context in step with its parameters.
/// </summary>
public class PDColumnGroupTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDColumnGroupTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Every parameter of the group reaches a wrapped component through the named "ColumnGroup" cascade.</summary>
	[Fact]
	public void Parameters_AreCascadedToChildren()
	{
		var cut = Render<PDColumnGroup>(p => p
			.Add(x => x.Name, "Stats")
			.Add(x => x.Icon, "fas fa-chart-bar")
			.Add(x => x.Ordinal, 20)
			.Add(x => x.Description, "Statistics")
			.AddChildContent<GroupProbe>());

		var context = cut.FindComponent<GroupProbe>().Instance.Context;
		context.Should().NotBeNull();
		context!.Name.Should().Be("Stats");
		context.Icon.Should().Be("fas fa-chart-bar");
		context.Ordinal.Should().Be(20);
		context.Description.Should().Be("Statistics");
	}

	/// <summary>When optional parameters are omitted the cascaded context carries the documented defaults.</summary>
	[Fact]
	public void OptionalParameters_Omitted_UseDefaults()
	{
		var cut = Render<PDColumnGroup>(p => p
			.Add(x => x.Name, "Identity")
			.AddChildContent<GroupProbe>());

		var context = cut.FindComponent<GroupProbe>().Instance.Context!;
		context.Icon.Should().BeNull();
		context.Ordinal.Should().Be(1000);
		context.Description.Should().BeNull();
	}

	/// <summary>The group renders only its children, with no wrapping element of its own.</summary>
	[Fact]
	public void Render_EmitsOnlyChildContent()
	{
		var cut = Render<PDColumnGroup>(p => p
			.Add(x => x.Name, "Stats")
			.AddChildContent("<span class=\"child\">x</span>"));

		cut.Markup.Trim().Should().Be("<span class=\"child\">x</span>");
	}

	/// <summary>Changing the parameters updates the same cascaded context instance, so children see the new values.</summary>
	[Fact]
	public void ParameterChange_UpdatesTheSameContextInstance()
	{
		var cut = Render<PDColumnGroup>(p => p
			.Add(x => x.Name, "Stats")
			.AddChildContent<GroupProbe>());
		var before = cut.FindComponent<GroupProbe>().Instance.Context;

		cut.Render(p => p.Add(x => x.Name, "Dates").Add(x => x.Ordinal, 5));

		var after = cut.FindComponent<GroupProbe>().Instance.Context;
		after.Should().BeSameAs(before);
		after!.Name.Should().Be("Dates");
		after.Ordinal.Should().Be(5);
	}

	/// <summary>
	/// Inside a <see cref="PDTable{TItem}"/>, a wrapped column takes the group's name as its effective group
	/// name and the table registers the group's metadata once.
	/// </summary>
	[Fact]
	public void WrappedColumnsInTable_InheritGroupName_AndRegisterGroupOnce()
	{
		Services.AddPanoramicDataBlazor();
		var table = Render<PDTable<Row>>(p => p
			.Add(x => x.DataProvider, new ListDataProviderService<Row>([new Row("1", "Alpha")]))
			.Add(x => x.KeyField, row => row.Id)
			.Add(x => x.ShowPager, false)
			.Add(x => x.ChildContent, builder =>
			{
				builder.OpenComponent<PDColumnGroup>(0);
				builder.AddAttribute(1, nameof(PDColumnGroup.Name), "Stats");
				builder.AddAttribute(2, nameof(PDColumnGroup.Icon), "fas fa-chart-bar");
				builder.AddAttribute(3, nameof(PDColumnGroup.ChildContent), (RenderFragment)(inner =>
				{
					AddColumn(inner, 0, nameof(Row.Id), row => row.Id);
					AddColumn(inner, 10, nameof(Row.Name), row => row.Name);
				}));
				builder.CloseComponent();
			}));

		table.WaitForAssertion(() => table.FindAll("tbody tr.pdtablerow").Should().ContainSingle());
		table.FindComponents<PDColumn<Row>>().Select(c => c.Instance.GroupName).Should().Equal("Stats", "Stats");
		table.Instance.ColumnGroups.Should().ContainSingle()
			.Which.Icon.Should().Be("fas fa-chart-bar");
	}

	private static void AddColumn(
		Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
		int sequence,
		string id,
		System.Linq.Expressions.Expression<Func<Row, object>> field)
	{
		builder.OpenComponent<PDColumn<Row>>(sequence);
		builder.AddAttribute(sequence + 1, nameof(PDColumn<Row>.Id), id);
		builder.AddAttribute(sequence + 2, nameof(PDColumn<Row>.Field), field);
		builder.CloseComponent();
	}

	/// <summary>A minimal table row.</summary>
	private sealed record Row(string Id, string Name);

	/// <summary>Captures the cascaded column group context for inspection.</summary>
	private sealed class GroupProbe : ComponentBase
	{
		[CascadingParameter(Name = "ColumnGroup")]
		public ColumnGroupContext? Context { get; set; }
	}
}
