using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;
using System.Text.Json;

namespace PanoramicData.Blazor;

/// <summary>
/// SVG-based graph visualization component with force-directed layout and interactive features.
/// </summary>
/// <typeparam name="TItem">The type of data items used to generate graph data.</typeparam>
public partial class PDGraph<TItem> : JSModuleComponentBase where TItem : class
{
	private static int _idSequence;
	private ElementReference _svgElement;
	private GraphData? _graphData;
	private Dictionary<string, (double X, double Y)> _nodePositions = new();
	private string _transformMatrix = "translate(0,0) scale(1)";
	private bool _isLoading = true;
	private bool _hasError;

	/// <summary>
	/// Gets the JavaScript module path for this component.
	/// </summary>
	protected override string ModulePath => "./_content/PanoramicData.Blazor/PDGraph.razor.js";

	/// <summary>
	/// Gets or sets the unique identifier for this component.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-graph-{++_idSequence}";

	/// <summary>
	/// Gets or sets the CSS class for styling.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the component is visible.
	/// </summary>
	[Parameter]
	public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets the data provider for the graph data.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public IDataProviderService<GraphData>? DataProvider { get; set; }

	/// <summary>
	/// Gets or sets the visualization configuration.
	/// </summary>
	[Parameter]
	public GraphVisualizationConfig VisualizationConfig { get; set; } = new();

	/// <summary>
	/// Gets or sets the clustering configuration.
	/// </summary>
	[Parameter]
	public GraphClusteringConfig ClusteringConfig { get; set; } = new();

	/// <summary>
	/// Gets or sets a callback that is invoked when a node is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<GraphNode> NodeClick { get; set; }

	/// <summary>
	/// Gets or sets a callback that is invoked when an edge is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<GraphEdge> EdgeClick { get; set; }

	/// <summary>
	/// Gets or sets a callback that is invoked when the selection changes.
	/// </summary>
	[Parameter]
	public EventCallback<(GraphNode? Node, GraphEdge? Edge)> SelectionChanged { get; set; }

	/// <summary>
	/// Internal model classes for styling.
	/// </summary>
	private sealed class NodeStyle
	{
		public double Size { get; set; }
		public string FillColor { get; set; } = "#4a90e2";
		public double FillAlpha { get; set; } = 0.8;
		public string StrokeColor { get; set; } = "#333333";
		public double StrokeThickness { get; set; } = 1.0;
		public double StrokeAlpha { get; set; } = 1.0;
		public string StrokePattern { get; set; } = "none";
		public NodeShape Shape { get; set; } = NodeShape.Circle;
	}

	private sealed class EdgeStyle
	{
		public double Thickness { get; set; } = 1.0;
		public string Color { get; set; } = "#666666";
		public double Alpha { get; set; } = 0.6;
		public string Pattern { get; set; } = "none";
	}

	private enum NodeShape
	{
		Circle = 0,
		Oval = 1,
		Diamond = 2,
		Octagon = 3,
		Square = 4,
		Rectangle = 5
	}

	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();
		await RefreshAsync().ConfigureAwait(false);
	}

	protected override async Task OnModuleLoadedAsync(bool firstRender)
	{
		if (firstRender && Module != null)
		{
			await Module.InvokeVoidAsync("initialize", Id).ConfigureAwait(false);
			
			// If we have data already, initialize the layout after module loads
			if (_graphData?.Nodes != null)
			{
				await InitializeLayout().ConfigureAwait(false);
			}
		}
	}

	/// <summary>
	/// Refreshes the graph data from the data provider.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token for the async operation.</param>
	/// <returns>A task representing the async operation.</returns>
	public async Task RefreshAsync(CancellationToken cancellationToken = default)
	{
		if (DataProvider == null) return;

		try
		{
			_isLoading = true;
			_hasError = false;
			StateHasChanged();

			var request = new DataRequest<GraphData>();
			var response = await DataProvider.GetDataAsync(request, cancellationToken).ConfigureAwait(false);
			
			if (response.Items.Any())
			{
				_graphData = response.Items.First();
				
				// Only initialize layout if module is loaded
				if (Module != null)
				{
					await InitializeLayout().ConfigureAwait(false);
				}
			}
			else
			{
				_graphData = new GraphData();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading graph data: {ex.Message}");
			_hasError = true;
			_graphData = null;
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	/// <summary>
	/// Centers the graph on the specified node.
	/// </summary>
	/// <param name="nodeId">The ID of the node to center on.</param>
	/// <returns>A task representing the async operation.</returns>
	public async Task CenterOnNodeAsync(string nodeId)
	{
		if (Module != null && _nodePositions.TryGetValue(nodeId, out var position))
		{
			await Module.InvokeVoidAsync("centerOnNode", Id, position.X, position.Y).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Fits the entire graph into the viewport.
	/// </summary>
	/// <returns>A task representing the async operation.</returns>
	public async Task FitToViewAsync()
	{
		if (Module != null)
		{
			await Module.InvokeVoidAsync("fitToView", Id).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Updates the visualization configuration.
	/// </summary>
	/// <param name="visualizationConfig">The new visualization configuration.</param>
	/// <param name="clusteringConfig">The new clustering configuration.</param>
	public void UpdateConfiguration(GraphVisualizationConfig visualizationConfig, GraphClusteringConfig clusteringConfig)
	{
		VisualizationConfig = visualizationConfig;
		ClusteringConfig = clusteringConfig;
		StateHasChanged();
	}

	/// <summary>
	/// Updates the configuration and refreshes the styling without regenerating positions.
	/// </summary>
	/// <param name="visualizationConfig">The new visualization configuration.</param>
	/// <param name="clusteringConfig">The new clustering configuration.</param>
	public async Task UpdateConfigurationAsync(GraphVisualizationConfig visualizationConfig, GraphClusteringConfig clusteringConfig)
	{
		VisualizationConfig = visualizationConfig;
		ClusteringConfig = clusteringConfig;

		// ? FIXED: Use updateConfiguration to preserve positions while updating styling
		if (Module != null && _graphData != null)
		{
			await Module.InvokeVoidAsync("updateConfiguration", Id, _graphData).ConfigureAwait(false);
		}
		
		StateHasChanged();
	}

	private async Task InitializeLayout()
	{
		if (_graphData?.Nodes == null) return;

		// Clear existing positions
		_nodePositions.Clear();
		
		// Initialize position tracking dictionary
		foreach (var node in _graphData.Nodes)
		{
			_nodePositions[node.Id] = (0, 0); // Temporary placeholder
		}

		// Start the force simulation - use regenerateLayout to force fresh positioning
		if (Module != null)
		{
			await Module.InvokeVoidAsync("regenerateLayout", Id, _graphData).ConfigureAwait(false);
		}
	}

	private NodeStyle GetNodeStyle(GraphNode node)
	{
		var config = VisualizationConfig.NodeVisualization;
		var defaults = VisualizationConfig.Defaults;
		
		var style = new NodeStyle();

		// Size
		var sizeValue = GetDimensionValue(node, config.SizeDimension, defaults.NodeSize);
		style.Size = config.MinSize + (config.MaxSize - config.MinSize) * sizeValue;

		// Shape (new feature - based on rounded proximity to specific values)
		var shapeValue = GetDimensionValue(node, config.ShapeDimension, defaults.NodeShape);
		style.Shape = GetNodeShapeFromValue(shapeValue);

		// Fill color (HSL)
		var hue = GetDimensionValue(node, config.FillHueDimension, defaults.NodeFillHue) * 360;
		var saturation = GetDimensionValue(node, config.FillSaturationDimension, defaults.NodeFillSaturation) * 100;
		var luminance = GetDimensionValue(node, config.FillLuminanceDimension, defaults.NodeFillLuminance) * 100;
		style.FillColor = $"hsl({hue:F0}, {saturation:F0}%, {luminance:F0}%)";
		style.FillAlpha = GetDimensionValue(node, config.FillAlphaDimension, defaults.NodeFillAlpha);

		// Stroke
		var strokeThickValue = GetDimensionValue(node, config.StrokeThicknessDimension, defaults.NodeStrokeThickness);
		style.StrokeThickness = config.MinStrokeThickness + (config.MaxStrokeThickness - config.MinStrokeThickness) * strokeThickValue;
		
		var strokeHue = GetDimensionValue(node, config.StrokeHueDimension, defaults.NodeStrokeHue) * 360;
		var strokeSaturation = GetDimensionValue(node, config.StrokeSaturationDimension, defaults.NodeStrokeSaturation) * 100;
		var strokeLuminance = GetDimensionValue(node, config.StrokeLuminanceDimension, defaults.NodeStrokeLuminance) * 100;
		style.StrokeColor = $"hsl({strokeHue:F0}, {strokeSaturation:F0}%, {strokeLuminance:F0}%)";
		style.StrokeAlpha = GetDimensionValue(node, config.StrokeAlphaDimension, defaults.NodeStrokeAlpha);

		// Pattern
		var patternValue = GetDimensionValue(node, config.StrokePatternDimension, defaults.NodeStrokePattern);
		style.StrokePattern = GetStrokePattern(patternValue);

		return style;
	}

	private static NodeShape GetNodeShapeFromValue(double value)
	{
		// Round to nearest 0.2 increment: 0, 0.2, 0.4, 0.6, 0.8, 1.0
		var rounded = Math.Round(value * 5) / 5;
		return rounded switch
		{
			<= 0.0 => NodeShape.Circle,
			<= 0.2 => NodeShape.Oval,
			<= 0.4 => NodeShape.Diamond,
			<= 0.6 => NodeShape.Octagon,
			<= 0.8 => NodeShape.Square,
			_ => NodeShape.Rectangle
		};
	}

	private static RenderFragment RenderNodeShape(NodeStyle style, GraphNode node) => builder =>
	{
		switch (style.Shape)
		{
			case NodeShape.Circle:
				builder.OpenElement(0, "circle");
				builder.AddAttribute(1, "class", "node-shape");
				builder.AddAttribute(2, "r", style.Size);
				builder.AddAttribute(3, "fill", style.FillColor);
				builder.AddAttribute(4, "fill-opacity", style.FillAlpha);
				builder.AddAttribute(5, "stroke", style.StrokeColor);
				builder.AddAttribute(6, "stroke-width", style.StrokeThickness);
				builder.AddAttribute(7, "stroke-opacity", style.StrokeAlpha);
				builder.AddAttribute(8, "stroke-dasharray", style.StrokePattern);
				builder.CloseElement();
				break;

			case NodeShape.Oval:
				builder.OpenElement(0, "ellipse");
				builder.AddAttribute(1, "class", "node-shape");
				builder.AddAttribute(2, "rx", style.Size * 1.4);
				builder.AddAttribute(3, "ry", style.Size * 0.8);
				builder.AddAttribute(4, "fill", style.FillColor);
				builder.AddAttribute(5, "fill-opacity", style.FillAlpha);
				builder.AddAttribute(6, "stroke", style.StrokeColor);
				builder.AddAttribute(7, "stroke-width", style.StrokeThickness);
				builder.AddAttribute(8, "stroke-opacity", style.StrokeAlpha);
				builder.AddAttribute(9, "stroke-dasharray", style.StrokePattern);
				builder.CloseElement();
				break;

			case NodeShape.Diamond:
				var diamondPoints = $"0,{-style.Size} {style.Size},0 0,{style.Size} {-style.Size},0";
				builder.OpenElement(0, "polygon");
				builder.AddAttribute(1, "class", "node-shape");
				builder.AddAttribute(2, "points", diamondPoints);
				builder.AddAttribute(3, "fill", style.FillColor);
				builder.AddAttribute(4, "fill-opacity", style.FillAlpha);
				builder.AddAttribute(5, "stroke", style.StrokeColor);
				builder.AddAttribute(6, "stroke-width", style.StrokeThickness);
				builder.AddAttribute(7, "stroke-opacity", style.StrokeAlpha);
				builder.AddAttribute(8, "stroke-dasharray", style.StrokePattern);
				builder.CloseElement();
				break;

			case NodeShape.Octagon:
				var octSize = style.Size;
				var octInner = octSize * 0.7;
				var octagonPoints = $"{-octInner},{-octSize} {octInner},{-octSize} {octSize},{-octInner} {octSize},{octInner} {octSize},{octSize} {-octInner},{octSize} {-octSize},{octInner} {-octSize},{-octInner}";
				builder.OpenElement(0, "polygon");
				builder.AddAttribute(1, "class", "node-shape");
				builder.AddAttribute(2, "points", octagonPoints);
				builder.AddAttribute(3, "fill", style.FillColor);
				builder.AddAttribute(4, "fill-opacity", style.FillAlpha);
				builder.AddAttribute(5, "stroke", style.StrokeColor);
				builder.AddAttribute(6, "stroke-width", style.StrokeThickness);
				builder.AddAttribute(7, "stroke-opacity", style.StrokeAlpha);
				builder.AddAttribute(8, "stroke-dasharray", style.StrokePattern);
				builder.CloseElement();
				break;

			case NodeShape.Square:
				builder.OpenElement(0, "rect");
				builder.AddAttribute(1, "class", "node-shape");
				builder.AddAttribute(2, "x", -style.Size);
				builder.AddAttribute(3, "y", -style.Size);
				builder.AddAttribute(4, "width", style.Size * 2);
				builder.AddAttribute(5, "height", style.Size * 2);
				builder.AddAttribute(6, "fill", style.FillColor);
				builder.AddAttribute(7, "fill-opacity", style.FillAlpha);
				builder.AddAttribute(8, "stroke", style.StrokeColor);
				builder.AddAttribute(9, "stroke-width", style.StrokeThickness);
				builder.AddAttribute(10, "stroke-opacity", style.StrokeAlpha);
				builder.AddAttribute(11, "stroke-dasharray", style.StrokePattern);
				builder.CloseElement();
				break;

			case NodeShape.Rectangle:
				builder.OpenElement(0, "rect");
				builder.AddAttribute(1, "class", "node-shape");
				builder.AddAttribute(2, "x", -style.Size * 1.5);
				builder.AddAttribute(3, "y", -style.Size);
				builder.AddAttribute(4, "width", style.Size * 3);
				builder.AddAttribute(5, "height", style.Size * 2);
				builder.AddAttribute(6, "fill", style.FillColor);
				builder.AddAttribute(7, "fill-opacity", style.FillAlpha);
				builder.AddAttribute(8, "stroke", style.StrokeColor);
				builder.AddAttribute(9, "stroke-width", style.StrokeThickness);
				builder.AddAttribute(10, "stroke-opacity", style.StrokeAlpha);
				builder.AddAttribute(11, "stroke-dasharray", style.StrokePattern);
				builder.CloseElement();
				break;
		}
	};

	private static RenderFragment RenderNodeLabel(GraphNode node, NodeStyle style) => builder =>
	{
		if (!string.IsNullOrEmpty(node.Label))
		{
			var fontSize = Math.Max(8, style.Size * 0.6);
			var textColor = GetContrastingTextColor(style.FillColor);
			var truncatedLabel = GetTruncatedLabel(node.Label, style.Size);
			
			builder.OpenElement(0, "text");
			builder.AddAttribute(1, "class", "node-label");
			builder.AddAttribute(2, "text-anchor", "middle");
			builder.AddAttribute(3, "dy", ".35em");
			builder.AddAttribute(4, "font-size", fontSize);
			builder.AddAttribute(5, "fill", textColor);
			builder.AddAttribute(6, "pointer-events", "none");
			builder.AddContent(7, truncatedLabel);
			builder.CloseElement();
		}
	};

	private EdgeStyle GetEdgeStyle(GraphEdge edge)
	{
		var config = VisualizationConfig.EdgeVisualization;
		var defaults = VisualizationConfig.Defaults;
		
		var style = new EdgeStyle();

		// Thickness
		var thicknessValue = GetDimensionValue(edge, config.ThicknessDimension, defaults.EdgeThickness);
		style.Thickness = config.MinThickness + (config.MaxThickness - config.MinThickness) * thicknessValue;

		// Color (HSL)
		var hue = GetDimensionValue(edge, config.HueDimension, defaults.EdgeHue) * 360;
		var saturation = GetDimensionValue(edge, config.SaturationDimension, defaults.EdgeSaturation) * 100;
		var luminance = GetDimensionValue(edge, config.LuminanceDimension, defaults.EdgeLuminance) * 100;
		style.Color = $"hsl({hue:F0}, {saturation:F0}%, {luminance:F0}%)";
		style.Alpha = GetDimensionValue(edge, config.AlphaDimension, defaults.EdgeAlpha);

		// Pattern
		var patternValue = GetDimensionValue(edge, config.PatternDimension, defaults.EdgePattern);
		style.Pattern = GetStrokePattern(patternValue);

		return style;
	}

	private static double GetDimensionValue(GraphNode node, string? dimensionName, double defaultValue)
	{
		if (string.IsNullOrEmpty(dimensionName) || !node.Dimensions.TryGetValue(dimensionName, out var value))
		{
			return defaultValue;
		}
		return Math.Clamp(value, 0.0, 1.0);
	}

	private static double GetDimensionValue(GraphEdge edge, string? dimensionName, double defaultValue)
	{
		if (string.IsNullOrEmpty(dimensionName) || !edge.Dimensions.TryGetValue(dimensionName, out var value))
		{
			return defaultValue;
		}
		return Math.Clamp(value, 0.0, 1.0);
	}

	private static string GetStrokePattern(double patternValue)
	{
		return patternValue switch
		{
			<= 0.1 => "none",
			<= 0.5 => "2,2", // Dotted
			<= 0.9 => "5,5", // Dashed
			_ => "none" // Solid
		};
	}

	private static string GetTruncatedLabel(string label, double nodeSize)
	{
		var maxLength = Math.Max(1, (int)(nodeSize / 4)); // Adjusted for better fit
		return label.Length > maxLength ? label[..maxLength] + "..." : label;
	}

	/// <summary>
	/// Gets a contrasting text color based on the background color.
	/// </summary>
	/// <param name="backgroundColor">The background color in HSL format.</param>
	/// <returns>A contrasting text color.</returns>
	private static string GetContrastingTextColor(string backgroundColor)
	{
		// For HSL colors, we can parse the luminance value
		if (backgroundColor.StartsWith("hsl(", StringComparison.OrdinalIgnoreCase))
		{
			var values = backgroundColor.Replace("hsl(", "").Replace(")", "").Split(',');
			if (values.Length >= 3 && double.TryParse(values[2].Replace("%", "").Trim(), out var luminance))
			{
				// If luminance is > 50%, use dark text, otherwise use light text
				return luminance > 50 ? "#212529" : "#ffffff";
			}
		}
		
		// Default to white text for unknown formats
		return "#ffffff";
	}

	private async Task OnNodeClick(GraphNode node)
	{
		// Clear all selections first
		if (_graphData?.Nodes != null)
		{
			foreach (var n in _graphData.Nodes)
				n.IsSelected = false;
		}
		if (_graphData?.Edges != null)
		{
			foreach (var e in _graphData.Edges)
				e.IsSelected = false;
		}

		// Select the clicked node
		node.IsSelected = true;
		
		// Set this node as the focus of the multi-dimensional space
		if (Module != null)
		{
			await Module.InvokeVoidAsync("setFocusNode", Id, node.Id).ConfigureAwait(false);
		}
		
		await NodeClick.InvokeAsync(node).ConfigureAwait(false);
		await SelectionChanged.InvokeAsync((node, null)). ConfigureAwait(false);
		StateHasChanged();
	}

	private async Task OnEdgeClick(GraphEdge edge)
	{
		// Clear all selections first
		if (_graphData?.Nodes != null)
		{
			foreach (var n in _graphData.Nodes)
				n.IsSelected = false;
		}
		if (_graphData?.Edges != null)
		{
			foreach (var e in _graphData.Edges)
				e.IsSelected = false;
		}

		// Select the clicked edge
		edge.IsSelected = true;
		
		// DO NOT call setFocusNode for edge clicks - only select the edge
		// No layout changes should happen when clicking edges
		
		await EdgeClick.InvokeAsync(edge). ConfigureAwait(false);
		await SelectionChanged.InvokeAsync((null, edge)).ConfigureAwait(false);
		StateHasChanged();
	}

	[JSInvokable]
	public void UpdateNodePositions(Dictionary<string, object> positions)
	{
		_nodePositions.Clear();
		foreach (var kvp in positions)
		{
			if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Object)
			{
				var x = element.GetProperty("x").GetDouble();
				var y = element.GetProperty("y").GetDouble();
				_nodePositions[kvp.Key] = (x, y);
			}
		}
		// Remove StateHasChanged() here to prevent interference with hover
		// The positions will be updated when the next render cycle occurs naturally
	}

	[JSInvokable]
	public void UpdateTransform(string transform)
	{
		_transformMatrix = transform;
		// Remove StateHasChanged() here to prevent unnecessary re-renders during smooth animations
	}
}