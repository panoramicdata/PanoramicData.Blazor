namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration for dimensional visualization mapping.
/// </summary>
public class GraphVisualizationConfig
{
	/// <summary>
	/// Gets or sets the dimension assignments for node visualization.
	/// </summary>
	public GraphNodeVisualization NodeVisualization { get; set; } = new();

	/// <summary>
	/// Gets or sets the dimension assignments for edge visualization.
	/// </summary>
	public GraphEdgeVisualization EdgeVisualization { get; set; } = new();

	/// <summary>
	/// Gets or sets the default values for unassigned dimensions.
	/// </summary>
	public GraphVisualizationDefaults Defaults { get; set; } = new();
}

/// <summary>
/// Node visualization dimension assignments.
/// </summary>
public class GraphNodeVisualization
{
	/// <summary>
	/// Dimension name for node size control.
	/// </summary>
	public string? SizeDimension { get; set; }

	/// <summary>
	/// Dimension name for node shape control (0=Circle, 0.2=Oval, 0.4=Diamond, 0.6=Octagon, 0.8=Square, 1.0=Rectangle).
	/// </summary>
	public string? ShapeDimension { get; set; }

	/// <summary>
	/// Dimension name for fill hue control.
	/// </summary>
	public string? FillHueDimension { get; set; }

	/// <summary>
	/// Dimension name for fill saturation control.
	/// </summary>
	public string? FillSaturationDimension { get; set; }

	/// <summary>
	/// Dimension name for fill luminance control.
	/// </summary>
	public string? FillLuminanceDimension { get; set; }

	/// <summary>
	/// Dimension name for fill alpha control.
	/// </summary>
	public string? FillAlphaDimension { get; set; }

	/// <summary>
	/// Dimension name for stroke thickness control.
	/// </summary>
	public string? StrokeThicknessDimension { get; set; }

	/// <summary>
	/// Dimension name for stroke hue control.
	/// </summary>
	public string? StrokeHueDimension { get; set; }

	/// <summary>
	/// Dimension name for stroke saturation control.
	/// </summary>
	public string? StrokeSaturationDimension { get; set; }

	/// <summary>
	/// Dimension name for stroke luminance control.
	/// </summary>
	public string? StrokeLuminanceDimension { get; set; }

	/// <summary>
	/// Dimension name for stroke alpha control.
	/// </summary>
	public string? StrokeAlphaDimension { get; set; }

	/// <summary>
	/// Dimension name for stroke pattern control.
	/// </summary>
	public string? StrokePatternDimension { get; set; }

	/// <summary>
	/// Minimum node size in pixels.
	/// </summary>
	public double MinSize { get; set; } = 5.0;

	/// <summary>
	/// Maximum node size in pixels.
	/// </summary>
	public double MaxSize { get; set; } = 20.0;

	/// <summary>
	/// Minimum stroke thickness in pixels.
	/// </summary>
	public double MinStrokeThickness { get; set; } = 0.5;

	/// <summary>
	/// Maximum stroke thickness in pixels.
	/// </summary>
	public double MaxStrokeThickness { get; set; } = 3.0;
}

/// <summary>
/// Edge visualization dimension assignments.
/// </summary>
public class GraphEdgeVisualization
{
	/// <summary>
	/// Dimension name for edge thickness control.
	/// </summary>
	public string? ThicknessDimension { get; set; }

	/// <summary>
	/// Dimension name for edge hue control.
	/// </summary>
	public string? HueDimension { get; set; }

	/// <summary>
	/// Dimension name for edge saturation control.
	/// </summary>
	public string? SaturationDimension { get; set; }

	/// <summary>
	/// Dimension name for edge luminance control.
	/// </summary>
	public string? LuminanceDimension { get; set; }

	/// <summary>
	/// Dimension name for edge alpha control.
	/// </summary>
	public string? AlphaDimension { get; set; }

	/// <summary>
	/// Dimension name for edge pattern control.
	/// </summary>
	public string? PatternDimension { get; set; }

	/// <summary>
	/// Minimum edge thickness in pixels.
	/// </summary>
	public double MinThickness { get; set; } = 0.5;

	/// <summary>
	/// Maximum edge thickness in pixels.
	/// </summary>
	public double MaxThickness { get; set; } = 5.0;
}

/// <summary>
/// Default values for unassigned visualization dimensions.
/// </summary>
public class GraphVisualizationDefaults
{
	// Node defaults
	public double NodeSize { get; set; } = 0.5;
	public double NodeShape { get; set; } = 0.0; // Circle by default
	public double NodeFillHue { get; set; } = 0.6; // Blue
	public double NodeFillSaturation { get; set; } = 0.7;
	public double NodeFillLuminance { get; set; } = 0.5;
	public double NodeFillAlpha { get; set; } = 0.8;
	public double NodeStrokeThickness { get; set; } = 0.5;
	public double NodeStrokeHue { get; set; } = 0.0; // Black
	public double NodeStrokeSaturation { get; set; } = 0.0;
	public double NodeStrokeLuminance { get; set; } = 0.0;
	public double NodeStrokeAlpha { get; set; } = 1.0;
	public double NodeStrokePattern { get; set; } = 1.0; // Solid

	// Edge defaults
	public double EdgeThickness { get; set; } = 0.3;
	public double EdgeHue { get; set; } = 0.0; // Black
	public double EdgeSaturation { get; set; } = 0.0;
	public double EdgeLuminance { get; set; } = 0.4;
	public double EdgeAlpha { get; set; } = 0.6;
	public double EdgePattern { get; set; } = 1.0; // Solid
}