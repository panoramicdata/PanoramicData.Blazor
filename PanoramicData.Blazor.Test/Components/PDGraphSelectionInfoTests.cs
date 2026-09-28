using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDGraphSelectionInfo{TItem}"/> describes the selected node or edge, or prompts for a
/// selection when there is none.
/// </summary>
public class PDGraphSelectionInfoTests : BunitContext
{
	/// <summary>With nothing selected, the prompt is shown and no item details are.</summary>
	[Fact]
	public void No_selection_shows_the_prompt()
	{
		var component = Render<PDGraphSelectionInfo<object>>();

		component.Find(".no-selection-text").TextContent.Should().Contain("Click on a node or edge");
		component.FindAll(".selection-item").Should().BeEmpty();
	}

	/// <summary>A component given no id renders with the id it was assigned.</summary>
	/// <remarks>
	/// The generated id's prefix is not asserted: it depends on the static PDComponentBase.Sequence, which
	/// other components advance concurrently when tests run in parallel (issue #159).
	/// </remarks>
	[Fact]
	public void A_default_id_is_rendered_on_the_element()
	{
		var component = Render<PDGraphSelectionInfo<object>>();

		component.Instance.Id.Should().NotBeNullOrWhiteSpace();
		component.Find(".pd-graph-selection-info").Id.Should().Be(component.Instance.Id);
	}

	/// <summary>An explicit id is kept, extra CSS is applied, and an invisible component gets d-none.</summary>
	[Fact]
	public void An_explicit_id_css_class_and_visibility_are_honoured()
	{
		var component = Render<PDGraphSelectionInfo<object>>(parameters => parameters
			.Add(p => p.Id, "mine")
			.Add(p => p.CssClass, "extra")
			.Add(p => p.IsVisible, false));

		var root = component.Find(".pd-graph-selection-info");
		root.Id.Should().Be("mine");
		root.ClassList.Should().Contain("extra").And.Contain("d-none");
	}

	/// <summary>A selected node shows its label, id, position, sorted dimensions and fixed status.</summary>
	[Fact]
	public void A_selected_node_is_described_in_full()
	{
		var node = new GraphNode
		{
			Id = "n1",
			Label = "Ada",
			X = 1.25,
			Y = 2.5,
			IsFixed = true,
			Dimensions = new() { ["Zeta"] = 0.5, ["Alpha"] = 0.125 }
		};

		var component = Render<PDGraphSelectionInfo<object>>(parameters => parameters.Add(p => p.SelectedNode, node));

		component.Find(".item-header strong").TextContent.Should().Be("Node: Ada");
		var values = component.FindAll(".detail-row .detail-value").Select(e => e.TextContent).ToList();
		values[0].Should().Be("n1");
		values[1].Should().Be($"({1.25.ToString("F1")}, {2.5.ToString("F1")})");
		component.FindAll(".dimension-name").Select(e => e.TextContent).Should().Equal("Alpha", "Zeta");
		component.FindAll(".dimension-value")[0].TextContent.Should().Be(0.125.ToString("F3"));
		component.FindAll(".dimension-fill")[1].GetAttribute("style").Should().Contain("width: 50%");
		component.Find(".node-status").TextContent.Should().Contain("Fixed Position");
	}

	/// <summary>A node with no dimensions that is not fixed shows neither section.</summary>
	[Fact]
	public void A_plain_node_has_no_dimensions_or_fixed_sections()
	{
		var component = Render<PDGraphSelectionInfo<object>>(parameters => parameters
			.Add(p => p.SelectedNode, new GraphNode { Id = "n2", Label = "Plain" }));

		component.FindAll(".dimensions-section").Should().BeEmpty();
		component.FindAll(".node-status").Should().BeEmpty();
	}

	/// <summary>A selected edge shows its label, endpoints, strength and dimensions.</summary>
	[Fact]
	public void A_selected_edge_is_described_in_full()
	{
		var edge = new GraphEdge
		{
			Id = "e1",
			Label = "knows",
			FromNodeId = "a",
			ToNodeId = "b",
			Strength = 0.75,
			Dimensions = new() { ["Weight"] = 0.25 }
		};

		var component = Render<PDGraphSelectionInfo<object>>(parameters => parameters.Add(p => p.SelectedEdge, edge));

		component.Find(".item-header strong").TextContent.Should().Be("Edge: knows");
		component.FindAll(".detail-row .detail-value").Select(e => e.TextContent)
			.Should().Equal("e1", "a", "b", 0.75.ToString("F2"));
		component.Find(".dimension-name").TextContent.Should().Be("Weight");
	}

	/// <summary>An unlabelled edge is called a connection and shows no dimensions section.</summary>
	[Fact]
	public void An_unlabelled_edge_is_called_a_connection()
	{
		var component = Render<PDGraphSelectionInfo<object>>(parameters => parameters
			.Add(p => p.SelectedEdge, new GraphEdge { Id = "e2" }));

		component.Find(".item-header strong").TextContent.Should().Be("Edge: Connection");
		component.FindAll(".dimensions-section").Should().BeEmpty();
	}

	/// <summary>A node takes precedence over an edge when both are selected.</summary>
	[Fact]
	public void A_node_takes_precedence_over_an_edge()
	{
		var component = Render<PDGraphSelectionInfo<object>>(parameters => parameters
			.Add(p => p.SelectedNode, new GraphNode { Label = "N" })
			.Add(p => p.SelectedEdge, new GraphEdge { Label = "E" }));

		component.FindAll(".item-header strong").Should().ContainSingle()
			.Which.TextContent.Should().Be("Node: N");
	}

	/// <summary>UpdateSelection replaces the selection and re-renders.</summary>
	[Fact]
	public async Task UpdateSelection_rerenders_with_the_new_selection()
	{
		var component = Render<PDGraphSelectionInfo<object>>();

		await component.InvokeAsync(() => component.Instance.UpdateSelection(null, new GraphEdge { Label = "later" }));

		component.Find(".item-header strong").TextContent.Should().Be("Edge: later");
		component.Instance.SelectedEdge!.Label.Should().Be("later");
		component.Instance.SelectedNode.Should().BeNull();
	}
}
