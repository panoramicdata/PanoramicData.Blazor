using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Node and edge styling tests for <see cref="PDGraph{TItem}"/>.
/// </summary>
public partial class PDGraphTests
{
	/// <summary>
	/// Verifies the default node style: a circle of middle size, filled and stroked from the defaults,
	/// with a truncated label in contrasting text.
	/// </summary>
	[Fact]
	public async Task A_node_without_dimensions_uses_the_default_style()
	{
		var component = RenderGraph();

		await ReportPositionsAsync(component, ("n1", 0, 0));

		var shape = component.Find("g.graph-node .node-shape");
		shape.LocalName.Should().Be("circle");
		shape.GetAttribute("fill").Should().Be("hsl(216, 70%, 50%)");
		shape.GetAttribute("stroke").Should().Be("hsl(0, 0%, 0%)");
		shape.GetAttribute("stroke-dasharray").Should().Be("none");
		var label = component.Find("g.graph-node text.node-label");
		label.TextContent.Should().Be("Alp...");
		label.GetAttribute("fill").Should().Be("#ffffff");
		component.Find("g.graph-node title").TextContent.Should().StartWith("Alpha");
	}

	/// <summary>
	/// Verifies that the shape dimension selects the node shape.
	/// </summary>
	[Theory]
	[InlineData(0.0, "circle", "r")]
	[InlineData(0.2, "ellipse", "rx")]
	[InlineData(0.4, "polygon", "points")]
	[InlineData(0.6, "polygon", "points")]
	[InlineData(0.8, "rect", "width")]
	[InlineData(1.0, "rect", "width")]
	public async Task The_shape_dimension_selects_the_shape(double shapeValue, string tag, string sizeAttribute)
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.ShapeDimension = "shape";
		var data = CreateData();
		data.Nodes[0].Dimensions["shape"] = shapeValue;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0));

		var shape = component.Find("g.graph-node .node-shape");
		shape.LocalName.Should().Be(tag);
		shape.HasAttribute(sizeAttribute).Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the octagon and diamond differ in their number of points, and square and rectangle in
	/// their proportions.
	/// </summary>
	[Theory]
	[InlineData(0.4, 4)]
	[InlineData(0.6, 8)]
	public async Task Polygon_shapes_have_their_number_of_points(double shapeValue, int points)
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.ShapeDimension = "shape";
		var data = CreateData();
		data.Nodes[0].Dimensions["shape"] = shapeValue;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0));

		component.Find("g.graph-node polygon").GetAttribute("points")!.Split(' ').Should().HaveCount(points);
	}

	/// <summary>
	/// Verifies that a rectangle is wider than it is tall, while a square is not.
	/// </summary>
	[Theory]
	[InlineData(0.8, false)]
	[InlineData(1.0, true)]
	public async Task A_rectangle_is_wider_than_a_square(double shapeValue, bool wider)
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.ShapeDimension = "shape";
		config.NodeVisualization.SizeDimension = "size";
		var data = CreateData();
		data.Nodes[0].Dimensions["shape"] = shapeValue;
		data.Nodes[0].Dimensions["size"] = 1;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0));

		var rect = component.Find("g.graph-node rect");
		rect.GetAttribute("height").Should().Be("40");
		(rect.GetAttribute("width") == "60").Should().Be(wider);
	}

	/// <summary>
	/// Verifies that dimension values are clamped, and that a light fill gets dark label text.
	/// </summary>
	[Fact]
	public async Task Dimensions_are_clamped_and_light_fills_get_dark_text()
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.SizeDimension = "size";
		config.NodeVisualization.FillLuminanceDimension = "light";
		var data = CreateData();
		data.Nodes[0].Dimensions["size"] = 5;
		data.Nodes[0].Dimensions["light"] = 0.8;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0));

		component.Find("g.graph-node circle").GetAttribute("r").Should().Be("20");
		component.Find("g.graph-node circle").GetAttribute("fill").Should().Be("hsl(216, 70%, 80%)");
		component.Find("text.node-label").GetAttribute("fill").Should().Be("#212529");
		component.Find("text.node-label").TextContent.Should().Be("Alpha");
	}

	/// <summary>
	/// Verifies that the pattern dimension selects solid, dotted or dashed strokes.
	/// </summary>
	[Theory]
	[InlineData(0.05, "none")]
	[InlineData(0.3, "2,2")]
	[InlineData(0.7, "5,5")]
	[InlineData(1.0, "none")]
	public async Task The_pattern_dimension_selects_the_stroke_pattern(double pattern, string expected)
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.StrokePatternDimension = "pattern";
		config.EdgeVisualization.PatternDimension = "pattern";
		var data = CreateData();
		data.Nodes[0].Dimensions["pattern"] = pattern;
		data.Edges[0].Dimensions["pattern"] = pattern;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0), ("n2", 1, 1));

		component.Find("g.graph-node .node-shape").GetAttribute("stroke-dasharray").Should().Be(expected);
		component.Find("line.graph-edge").GetAttribute("stroke-dasharray").Should().Be(expected);
	}

	/// <summary>
	/// Verifies the default edge style and that edge dimensions change it.
	/// </summary>
	[Fact]
	public async Task Edges_are_styled_from_their_dimensions()
	{
		var config = new GraphVisualizationConfig();
		config.EdgeVisualization.HueDimension = "hue";
		config.EdgeVisualization.ThicknessDimension = "weight";
		var data = CreateData();
		data.Edges[0].Dimensions["hue"] = 0.5;
		data.Edges[0].Dimensions["weight"] = 1;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0), ("n2", 1, 1));

		var edge = component.Find("line.graph-edge");
		edge.GetAttribute("stroke").Should().Be("hsl(180, 0%, 40%)");
		edge.GetAttribute("stroke-width").Should().Be("5");
	}
}
