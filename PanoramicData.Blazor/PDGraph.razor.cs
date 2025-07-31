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
	private Dictionary<string, (double X, double Y)> _nodePositions = [];
	private string _transformMatrix = "translate(0,0) scale(1)";
	private bool _isLoading = true;
	private bool _hasError;
	private DotNetObjectReference<PDGraph<TItem>>? _objRef;

	// Add a flag to prevent re-initialization during selection updates
	private bool _isUpdatingSelection = false;

	// Add these fields to track parameter changes
	private bool _isUpdatingParameters = false;
	private GraphVisualizationConfig? _previousVisualizationConfig;
	private GraphClusteringConfig? _previousClusteringConfig;
	private double _previousConvergenceThreshold = 0.02;
	private double _previousDamping = 0.95;

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
	/// Gets or sets the convergence threshold for the physics simulation. Lower values make physics run longer.
	/// </summary>
	[Parameter]
	public double ConvergenceThreshold { get; set; } = 0.02;

	/// <summary>
	/// Gets or sets the damping factor for the physics simulation. Higher values mean faster settling.
	/// </summary>
	[Parameter]
	public double Damping { get; set; } = 0.95;

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

		// ✅ FIXED: Early exit if we're updating to prevent cascading
		if (_isUpdatingSelection || _isUpdatingParameters)
		{
			Console.WriteLine("PDGraph: Skipping OnParametersSetAsync - already updating");
			return;
		}

		// ✅ FIXED: Track if this is the first load
		var isFirstLoad = _previousVisualizationConfig == null;

		if (isFirstLoad)
		{
			Console.WriteLine("PDGraph: First load - doing full refresh");
			_previousVisualizationConfig = VisualizationConfig;
			_previousClusteringConfig = ClusteringConfig;
			_previousConvergenceThreshold = ConvergenceThreshold;
			_previousDamping = Damping;

			await RefreshAsync().ConfigureAwait(false);
			return;
		}

		// ✅ FIXED: Check for actual changes with better logic
		var hasVisualizationChanged = !ReferenceEquals(_previousVisualizationConfig, VisualizationConfig);
		var hasClusteringChanged = !ReferenceEquals(_previousClusteringConfig, ClusteringConfig);
		var hasConvergenceChanged = Math.Abs(_previousConvergenceThreshold - ConvergenceThreshold) > 0.001;
		var hasDampingChanged = Math.Abs(_previousDamping - Damping) > 0.001;

		if (!hasVisualizationChanged && !hasClusteringChanged && !hasConvergenceChanged && !hasDampingChanged)
		{
			// No actual changes detected
			return;
		}

		Console.WriteLine($"PDGraph: Parameter changes - Viz: {hasVisualizationChanged}, Clustering: {hasClusteringChanged}, Convergence: {hasConvergenceChanged} ({_previousConvergenceThreshold:F3} -> {ConvergenceThreshold:F3}), Damping: {hasDampingChanged} ({_previousDamping:F3} -> {Damping:F3})");

		// ✅ FIXED: Set flag to prevent cascading and store new values
		_isUpdatingParameters = true;
		try
		{
			_previousVisualizationConfig = VisualizationConfig;
			_previousClusteringConfig = ClusteringConfig;
			_previousConvergenceThreshold = ConvergenceThreshold;
			_previousDamping = Damping;

			if (hasVisualizationChanged || hasClusteringChanged)
			{
				Console.WriteLine("PDGraph: Updating configuration via JavaScript");
				if (Module != null && _graphData != null)
				{
					await Module.InvokeVoidAsync("updateConfiguration", Id, _graphData, ClusteringConfig).ConfigureAwait(false);
				}
			}
			else if (hasConvergenceChanged || hasDampingChanged)
			{
				Console.WriteLine($"PDGraph: Updating physics parameters to Convergence: {ConvergenceThreshold:F3}, Damping: {Damping:F3}");
				if (Module != null)
				{
					await Module.InvokeVoidAsync("updatePhysicsParameters", Id, ConvergenceThreshold, Damping).ConfigureAwait(false);
				}
			}
		}
		finally
		{
			_isUpdatingParameters = false;
		}
	}

	protected override async Task OnModuleLoadedAsync(bool firstRender)
	{
		if (firstRender && Module != null)
		{
			// Create a DotNet object reference for JavaScript interop
			_objRef = DotNetObjectReference.Create(this);
			await Module.InvokeVoidAsync("initialize", Id, _objRef, ClusteringConfig).ConfigureAwait(false);

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
		if (DataProvider == null)
		{
			return;
		}

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
	/// Updates the physics simulation parameters.
	/// </summary>
	/// <param name="convergenceThreshold">The convergence threshold for physics simulation.</param>
	public async Task UpdatePhysicsParametersAsync(double convergenceThreshold)
	{
		// ✅ FIXED: Check if we're already updating to prevent cascading
		if (_isUpdatingParameters)
		{
			Console.WriteLine($"UpdatePhysicsParametersAsync: Already updating, skipping ({convergenceThreshold:F3})");
			return;
		}

		_isUpdatingParameters = true;
		try
		{
			Console.WriteLine($"UpdatePhysicsParametersAsync: Updating from {ConvergenceThreshold:F3} to {convergenceThreshold:F3}");
			ConvergenceThreshold = convergenceThreshold;
			if (Module != null)
			{
				await Module.InvokeVoidAsync("updatePhysicsParameters", Id, convergenceThreshold).ConfigureAwait(false);
			}
		}
		finally
		{
			_isUpdatingParameters = false;
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
		_isUpdatingParameters = true;
		try
		{
			VisualizationConfig = visualizationConfig;
			ClusteringConfig = clusteringConfig;

			// ✅ FIXED: Use updateConfiguration to preserve positions while updating styling
			if (Module != null && _graphData != null)
			{
				await Module.InvokeVoidAsync("updateConfiguration", Id, _graphData).ConfigureAwait(false);
			}

			StateHasChanged();
		}
		finally
		{
			_isUpdatingParameters = false;
		}
	}

	private async Task InitializeLayout()
	{
		if (_graphData?.Nodes == null)
		{
			return;
		}

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
			// Pass the convergence threshold parameter
			await Module.InvokeVoidAsync("regenerateLayout", Id, _graphData, ConvergenceThreshold, ClusteringConfig).ConfigureAwait(false);
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
		// ✅ FIXED: Set flag to prevent re-initialization
		_isUpdatingSelection = true;

		try
		{
			// Clear all selections first
			if (_graphData?.Nodes != null)
			{
				foreach (var n in _graphData.Nodes)
				{
					n.IsSelected = false;
				}
			}

			if (_graphData?.Edges != null)
			{
				foreach (var e in _graphData.Edges)
				{
					e.IsSelected = false;
				}
			}

			// Select the clicked node
			node.IsSelected = true;

			// ✅ FIXED: Update selection in JavaScript without regenerating layout
			if (Module != null)
			{
				await Module.InvokeVoidAsync("updateSelection", Id, node.Id, "node").ConfigureAwait(false);
				await Module.InvokeVoidAsync("setFocusNode", Id, node.Id).ConfigureAwait(false);
			}

			await NodeClick.InvokeAsync(node).ConfigureAwait(false);
			await SelectionChanged.InvokeAsync((node, null)).ConfigureAwait(false);

			// ✅ FIXED: Don't call StateHasChanged() here to avoid triggering refresh
		}
		finally
		{
			_isUpdatingSelection = false;
		}
	}

	private async Task OnEdgeClick(GraphEdge edge)
	{
		// ✅ FIXED: Set flag to prevent re-initialization
		_isUpdatingSelection = true;

		try
		{
			// Clear all selections first
			if (_graphData?.Nodes != null)
			{
				foreach (var n in _graphData.Nodes)
				{
					n.IsSelected = false;
				}
			}

			if (_graphData?.Edges != null)
			{
				foreach (var e in _graphData.Edges)
				{
					e.IsSelected = false;
				}
			}

			// Select the clicked edge
			edge.IsSelected = true;

			// ✅ FIXED: Update selection in JavaScript without regenerating layout
			if (Module != null)
			{
				await Module.InvokeVoidAsync("updateSelection", Id, edge.Id, "edge").ConfigureAwait(false);
			}

			await EdgeClick.InvokeAsync(edge).ConfigureAwait(false);
			await SelectionChanged.InvokeAsync((null, edge)).ConfigureAwait(false);

			// ✅ FIXED: Don't call StateHasChanged() here to avoid triggering refresh
		}
		finally
		{
			_isUpdatingSelection = false;
		}
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

	/// <summary>
	/// JavaScript interop method called when a node is clicked from the JavaScript side.
	/// </summary>
	/// <param name="nodeData">The node data from JavaScript.</param>
	[JSInvokable]
	public async Task OnNodeClickedFromJS(JsonElement nodeData)
	{
		try
		{
			Console.WriteLine($"Node clicked from JS: {nodeData}");

			// Find the corresponding node in our graph data
			if (_graphData?.Nodes != null)
			{
				var nodeId = nodeData.GetProperty("id").GetString();
				var node = _graphData.Nodes.FirstOrDefault(n => n.Id == nodeId);

				if (node != null)
				{
					Console.WriteLine($"Found node: {node.Label}, invoking click handler");
					await OnNodeClick(node).ConfigureAwait(false);
				}
				else
				{
					Console.WriteLine($"Node with ID {nodeId} not found in graph data");
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error handling node click from JS: {ex.Message}");
		}
	}

	public override async ValueTask DisposeAsync()
	{
		if (Module != null)
		{
			await Module.InvokeVoidAsync("destroy", Id).ConfigureAwait(false);
		}

		_objRef?.Dispose();
		await base.DisposeAsync();
	}
}