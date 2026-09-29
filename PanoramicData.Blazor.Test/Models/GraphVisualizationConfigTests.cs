using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="GraphVisualizationConfig"/> and the node, edge and default settings it holds.</summary>
public class GraphVisualizationConfigTests
{
	/// <summary>A new configuration holds non-null node, edge and default settings.</summary>
	[Fact]
	public void New_HasAllSections()
	{
		var config = new GraphVisualizationConfig();

		config.NodeVisualization.Should().NotBeNull();
		config.EdgeVisualization.Should().NotBeNull();
		config.Defaults.Should().NotBeNull();
	}

	/// <summary>Node visualisation maps no dimensions by default and sizes nodes between 5 and 20.</summary>
	[Fact]
	public void NodeVisualization_HasDocumentedDefaults()
	{
		var node = new GraphNodeVisualization();

		node.SizeDimension.Should().BeNull();
		node.ShapeDimension.Should().BeNull();
		node.FillHueDimension.Should().BeNull();
		node.StrokePatternDimension.Should().BeNull();
		node.MinSize.Should().Be(5.0);
		node.MaxSize.Should().Be(20.0);
		node.MinStrokeThickness.Should().Be(0.5);
		node.MaxStrokeThickness.Should().Be(3.0);
	}

	/// <summary>Edge visualisation maps no dimensions by default and draws edges between 0.5 and 5 thick.</summary>
	[Fact]
	public void EdgeVisualization_HasDocumentedDefaults()
	{
		var edge = new GraphEdgeVisualization();

		edge.ThicknessDimension.Should().BeNull();
		edge.PatternDimension.Should().BeNull();
		edge.MinThickness.Should().Be(0.5);
		edge.MaxThickness.Should().Be(5.0);
	}

	/// <summary>The defaults describe a semi-transparent blue circle with a solid black stroke, and grey edges.</summary>
	[Fact]
	public void Defaults_HaveDocumentedValues()
	{
		var defaults = new GraphVisualizationDefaults();

		defaults.NodeSize.Should().Be(0.5);
		defaults.NodeShape.Should().Be(0);
		defaults.NodeFillHue.Should().Be(0.6);
		defaults.NodeFillSaturation.Should().Be(0.7);
		defaults.NodeFillLuminance.Should().Be(0.5);
		defaults.NodeFillAlpha.Should().Be(0.8);
		defaults.NodeStrokeThickness.Should().Be(0.5);
		defaults.NodeStrokeHue.Should().Be(0);
		defaults.NodeStrokeAlpha.Should().Be(1.0);
		defaults.NodeStrokePattern.Should().Be(1.0);
		defaults.EdgeThickness.Should().Be(0.3);
		defaults.EdgeLuminance.Should().Be(0.4);
		defaults.EdgeAlpha.Should().Be(0.6);
		defaults.EdgePattern.Should().Be(1.0);
	}

	/// <summary>Every settable member of every section round-trips.</summary>
	[Fact]
	public void AllMembers_RoundTrip()
	{
		var config = new GraphVisualizationConfig
		{
			NodeVisualization = new GraphNodeVisualization(),
			EdgeVisualization = new GraphEdgeVisualization(),
			Defaults = new GraphVisualizationDefaults()
		};

		AssertRoundTrip(config.NodeVisualization);
		AssertRoundTrip(config.EdgeVisualization);
		AssertRoundTrip(config.Defaults);
	}

	private static void AssertRoundTrip(object instance)
	{
		foreach (var property in instance.GetType().GetProperties().Where(p => p.CanWrite))
		{
			object sample = property.PropertyType == typeof(double) ? 0.123 : "dimension";
			property.SetValue(instance, sample);
			property.GetValue(instance).Should().Be(sample, property.Name);
		}
	}
}
