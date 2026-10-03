using System.Security.Cryptography;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Dummy data item for graph demonstration.
/// </summary>
public class GraphDataItem
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
}

public partial class PDGraphViewerPage : ComponentBase, IDisposable
{
	protected PDGraphViewer<GraphDataItem>? GraphViewer { get; set; }
	private readonly GraphDataProvider _dataProvider = new();
	private GraphVisualizationConfig _visualizationConfig = new();
	private GraphClusteringConfig _clusteringConfig = new();
	private double _damping = 0.95;
	private GraphData? _currentGraphData; // To store the current graph data

	// Demo configuration
	protected bool ShowInfo { get; set; } = true;
	protected bool ShowControls { get; set; } = true;
	protected bool ReadOnlyControls { get; set; }
	protected SplitDirection SplitDirection { get; set; } = SplitDirection.Horizontal;

	// ✅ FIXED: Simple convergence threshold handling
	private double _convergenceThreshold = 0.08;
	private bool _isUpdatingControls;

	// Selection state
	private GraphNode? _selectedNode;
	private GraphEdge? _selectedEdge;

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	protected override void OnInitialized()
	{
		// Configure visualization with meaningful dimension mappings
		_visualizationConfig.NodeVisualization.SizeDimension = "Influence";
		_visualizationConfig.NodeVisualization.ShapeDimension = "Category";
		_visualizationConfig.NodeVisualization.FillHueDimension = "Era";
		_visualizationConfig.NodeVisualization.FillSaturationDimension = "Fame";
		_visualizationConfig.NodeVisualization.FillLuminanceDimension = "Creativity";
		_visualizationConfig.EdgeVisualization.ThicknessDimension = "ConnectionStrength";
		_visualizationConfig.EdgeVisualization.HueDimension = "RelationshipType";
		_visualizationConfig.EdgeVisualization.AlphaDimension = "Certainty";

		// Enable clustering
		_clusteringConfig.IsEnabled = true;
		_clusteringConfig.MaxClusters = 4;
		_clusteringConfig.Algorithm = GraphClusteringAlgorithm.KMeans;
	}

	private async Task OnRefreshData()
	{
		if (_isUpdatingControls)
		{
			return;
		}

		_isUpdatingControls = true;
		try
		{
			// Fetch new data
			var newGraphDataResponse = await _dataProvider.GetDataAsync(new DataRequest<GraphData>(), CancellationToken.None).ConfigureAwait(false);
			var newGraphData = newGraphDataResponse.Items.FirstOrDefault();

			if (newGraphData == null)
			{
				return; // No data to refresh
			}

			// Check if the set of nodes and edges has changed (by ID)
			var structureChanged = HasStructureChanged(_currentGraphData, newGraphData);

			_currentGraphData = newGraphData; // Update current data reference

			if (GraphViewer != null)
			{
				await RefreshGraphViewerAsync(GraphViewer, structureChanged).ConfigureAwait(false);
			}

			EventManager?.Add(new Event("Graph data refreshed"));
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error refreshing graph data: {ex.Message}");
			EventManager?.Add(new Event($"Error refreshing graph data: {ex.Message}"));
		}
		finally
		{
			_isUpdatingControls = false;
		}
	}

	private static bool HasStructureChanged(GraphData? currentGraphData, GraphData newGraphData)
		=> currentGraphData == null
			|| !HaveSameIds(currentGraphData.Nodes.Select(n => n.Id), newGraphData.Nodes.Select(n => n.Id))
			|| !HaveSameIds(currentGraphData.Edges.Select(e => e.Id), newGraphData.Edges.Select(e => e.Id));

	private static bool HaveSameIds(IEnumerable<string> currentIds, IEnumerable<string> newIds)
		=> currentIds.OrderBy(id => id).SequenceEqual(newIds.OrderBy(id => id));

	private async Task RefreshGraphViewerAsync(PDGraphViewer<GraphDataItem> graphViewer, bool structureChanged)
	{
		if (structureChanged)
		{
			// If nodes or edges have changed, force a full refresh (regenerate layout)
			Console.WriteLine("PDGraphViewerPage: Nodes or edges changed, performing full refresh.");
			await graphViewer.RefreshAsync().ConfigureAwait(false);
		}
		else
		{
			// If only data within existing nodes/edges changed, update configuration (preserve layout)
			Console.WriteLine("PDGraphViewerPage: Only data changed, updating configuration.");
			await graphViewer.UpdateConfigurationAsync((_visualizationConfig, _clusteringConfig, _damping)).ConfigureAwait(false);
		}
	}

	private async Task OnFitToView()
	{
		if (_isUpdatingControls)
		{
			return;
		}

		_isUpdatingControls = true;
		try
		{
			if (GraphViewer != null)
			{
				await GraphViewer.FitToViewAsync();
			}

			EventManager?.Add(new Event("Graph fitted to view"));
		}
		finally
		{
			_isUpdatingControls = false;
		}
	}

	// ✅ FIXED: Only update on input, don't call the update method
	private void OnConvergenceThresholdInput(ChangeEventArgs e)
	{
		if (_isUpdatingControls)
		{
			return;
		}

		if (double.TryParse(e.Value?.ToString(), out var value))
		{
			_convergenceThreshold = value;
			// Just update the local value for UI display
		}
	}

	// ✅ FIXED: Don't call the update method here - let parameter binding handle it
	private void OnConvergenceThresholdChanged()
	{
		if (_isUpdatingControls)
		{
			return;
		}

		Console.WriteLine($"Demo page: Convergence threshold changed to {_convergenceThreshold:F3}");
		EventManager?.Add(new Event($"Convergence threshold updated: {_convergenceThreshold:F3}"));
	}

	// ✅ FIXED: Use parameter binding to update the damping value
	private void OnDampingChanged()
	{
		if (_isUpdatingControls)
		{
			return;
		}

		Console.WriteLine($"Demo page: Damping changed to {_damping:F2}");
		EventManager?.Add(new Event($"Damping updated: {_damping:F2}"));
	}

	// ✅ ENHANCED: Better event logging
	private void OnNodeClick(GraphNode node)
	{
		_selectedNode = node;
		_selectedEdge = null;
		EventManager?.Add(new Event($"✅ Node clicked: {node.Label} (ID: {node.Id})"));
		Console.WriteLine($"Node click event fired: {node.Label}");
		StateHasChanged();
	}

	private void OnEdgeClick(GraphEdge edge)
	{
		_selectedNode = null;
		_selectedEdge = edge;
		EventManager?.Add(new Event($"✅ Edge clicked: {edge.Id} ({edge.FromNodeId} → {edge.ToNodeId})"));
		Console.WriteLine($"Edge click event fired: {edge.Id}");
		StateHasChanged();
	}

	private void OnSelectionChanged((GraphNode? Node, GraphEdge? Edge) selection)
	{
		_selectedNode = selection.Node;
		_selectedEdge = selection.Edge;

		var selectionInfo = selection.Node != null
			? $"Node: {selection.Node.Label}"
			: selection.Edge != null
				? $"Edge: {selection.Edge.Id}"
				: "None";

		EventManager?.Add(new Event($"✅ Selection changed: {selectionInfo}"));
		Console.WriteLine($"Selection changed: {selectionInfo}");
		StateHasChanged();
	}

	// ✅ FIXED: Don't trigger updates here - this should be read-only
	private void OnConfigurationChanged((GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping) config)
	{
		Console.WriteLine("Demo page: Configuration changed event received");
		_visualizationConfig = config.Visualization;
		_clusteringConfig = config.Clustering;
		_damping = config.Damping;
		EventManager?.Add(new Event("Graph configuration changed"));
		StateHasChanged();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}

/// <summary>
/// Sample data provider for a fun knowledge graph about innovation and creativity.
/// </summary>
public class GraphDataProvider : IDataProviderService<GraphData>
{
	private sealed record NodeSeed(string Id, string Label, double Era, double Fame, double Influence, double Creativity);

	private sealed record EdgeSeed(string From, string To, double Type, double Strength, double Certainty, string Label);

	// Categories for a more structured graph
	private const double _personCategory = 0.1;
	private const double _inventionCategory = 0.3;
	private const double _organizationCategory = 0.5;
	private const double _fieldCategory = 0.7;
	private const double _conceptCategory = 0.9;

	private static readonly NodeSeed[] _people =
	[
		new("einstein", "Albert Einstein", 0.3, 0.95, 0.9, 0.95),
		new("davinci", "Leonardo da Vinci", 0.1, 0.9, 0.85, 1.0),
		new("jobs", "Steve Jobs", 0.8, 0.9, 0.8, 0.85),
		new("tesla", "Nikola Tesla", 0.25, 0.7, 0.75, 0.9),
		new("curie", "Marie Curie", 0.3, 0.8, 0.7, 0.8),
		new("wozniak", "Steve Wozniak", 0.7, 0.6, 0.65, 0.8),
		new("turing", "Alan Turing", 0.4, 0.8, 0.85, 0.95),
		new("lovelace", "Ada Lovelace", 0.2, 0.6, 0.7, 0.9),
		new("newton", "Isaac Newton", 0.15, 0.9, 0.95, 0.9),
		new("galileo", "Galileo Galilei", 0.1, 0.8, 0.8, 0.85)
	];

	private static readonly NodeSeed[] _inventions =
	[
		new("relativity", "Theory of Relativity", 0.3, 0.8, 0.9, 0.95),
		new("iphone", "iPhone", 0.9, 0.95, 0.85, 0.8),
		new("electricity", "AC Electricity", 0.25, 0.9, 1.0, 0.85),
		new("flight", "Powered Flight", 0.3, 0.85, 0.9, 0.9),
		new("internet", "Internet", 0.7, 0.9, 0.95, 0.8),
		new("radioactivity", "Radioactivity", 0.3, 0.7, 0.8, 0.85),
		new("turing_machine", "Turing Machine", 0.4, 0.7, 0.9, 0.95),
		new("analytical_engine", "Analytical Engine", 0.2, 0.5, 0.8, 0.9),
		new("calculus", "Calculus", 0.15, 0.8, 0.95, 0.9),
		new("telescope", "Telescope", 0.1, 0.7, 0.8, 0.8)
	];

	private static readonly NodeSeed[] _organizations =
	[
		new("apple", "Apple Inc.", 0.8, 0.9, 0.8, 0.75),
		new("princeton", "Princeton University", 0.4, 0.7, 0.75, 0.7),
		new("mit", "MIT", 0.6, 0.8, 0.8, 0.85),
		new("bell_labs", "Bell Labs", 0.5, 0.6, 0.7, 0.8),
		new("bletchley_park", "Bletchley Park", 0.4, 0.7, 0.8, 0.9),
		new("cambridge", "University of Cambridge", 0.2, 0.8, 0.85, 0.8),
		new("xerox_parc", "Xerox PARC", 0.7, 0.7, 0.8, 0.85)
	];

	private static readonly NodeSeed[] _fields =
	[
		new("physics", "Physics", 0.5, 0.7, 0.9, 0.8),
		new("art", "Renaissance Art", 0.1, 0.8, 0.7, 0.95),
		new("engineering", "Engineering", 0.5, 0.6, 0.85, 0.75),
		new("computer_science", "Computer Science", 0.7, 0.7, 0.9, 0.8),
		new("mathematics", "Mathematics", 0.4, 0.8, 0.9, 0.85),
		new("astronomy", "Astronomy", 0.3, 0.7, 0.8, 0.75),
		new("cryptography", "Cryptography", 0.5, 0.6, 0.7, 0.8)
	];

	private static readonly NodeSeed[] _concepts =
	[
		new("innovation", "Innovation", 0.5, 0.6, 0.85, 0.9),
		new("creativity", "Creativity", 0.5, 0.5, 0.7, 1.0),
		new("computation", "Computation", 0.6, 0.7, 0.85, 0.8),
		new("relativity_principle", "Principle of Relativity", 0.3, 0.8, 0.9, 0.9),
		new("user_interface", "User Interface", 0.7, 0.6, 0.7, 0.8)
	];

	// A rich set of edges with semantic meaning
	private static readonly EdgeSeed[] _edgeSeeds =
	[
		// Foundational relationships
		new("newton", "calculus", 0.1, 0.95, 1.0, "developed"),
		new("galileo", "telescope", 0.1, 0.9, 1.0, "improved"),
		new("davinci", "art", 0.1, 0.95, 1.0, "mastered"),
		new("davinci", "engineering", 0.2, 0.8, 0.8, "pioneered"),

		// Physics and Math
		new("einstein", "relativity", 0.1, 0.95, 1.0, "discovered"),
		new("einstein", "physics", 0.2, 0.9, 1.0, "advanced"),
		new("einstein", "newton", 0.7, 0.8, 0.9, "built upon work of"),
		new("curie", "radioactivity", 0.1, 0.95, 1.0, "discovered"),
		new("curie", "physics", 0.2, 0.8, 1.0, "contributed to"),
		new("calculus", "physics", 0.8, 0.9, 1.0, "is fundamental to"),
		new("relativity", "relativity_principle", 0.8, 0.9, 1.0, "is based on"),

		// Computer Science
		new("lovelace", "analytical_engine", 0.1, 0.8, 0.9, "wrote algorithm for"),
		new("turing", "turing_machine", 0.1, 0.95, 1.0, "formalized"),
		new("turing", "computer_science", 0.2, 0.9, 1.0, "is father of"),
		new("turing", "cryptography", 0.2, 0.85, 0.9, "applied"),
		new("turing_machine", "computation", 0.8, 0.9, 1.0, "defines"),
		new("jobs", "apple", 0.5, 0.95, 1.0, "co-founded"),
		new("wozniak", "apple", 0.5, 0.9, 1.0, "co-founded"),
		new("jobs", "wozniak", 0.6, 0.8, 1.0, "partnered with"),
		new("apple", "iphone", 0.1, 0.9, 1.0, "developed"),
		new("xerox_parc", "user_interface", 0.1, 0.8, 0.9, "pioneered"),
		new("apple", "xerox_parc", 0.7, 0.7, 0.8, "was influenced by"),

		// Institutional connections
		new("einstein", "princeton", 0.3, 0.8, 0.9, "worked at"),
		new("turing", "cambridge", 0.3, 0.8, 0.9, "studied at"),
		new("turing", "bletchley_park", 0.3, 0.9, 1.0, "worked at"),
		new("newton", "cambridge", 0.3, 0.85, 1.0, "was a fellow of"),
		new("bell_labs", "internet", 0.1, 0.7, 0.8, "contributed to"),
		new("mit", "computer_science", 0.3, 0.85, 0.9, "is a leader in"),

		// Conceptual links
		new("creativity", "innovation", 0.7, 0.9, 0.8, "enables"),
		new("art", "creativity", 0.7, 0.8, 0.9, "expresses"),
		new("innovation", "iphone", 0.4, 0.8, 0.8, "produced"),
		new("engineering", "flight", 0.8, 0.8, 1.0, "achieved"),
		new("physics", "engineering", 0.7, 0.8, 0.9, "informs"),
		new("mathematics", "computer_science", 0.7, 0.9, 1.0, "is the foundation of")
	];

	public Task<DataResponse<GraphData>> GetDataAsync(DataRequest<GraphData> request, CancellationToken cancellationToken)
	{
		var graphData = GenerateInnovationKnowledgeGraph();
		var response = new DataResponse<GraphData>([graphData], 1);
		return Task.FromResult(response);
	}

	public Task<OperationResponse> CreateAsync(GraphData item, CancellationToken cancellationToken)
	{
		return Task.FromResult(new OperationResponse { Success = false, ErrorMessage = "Create not supported" });
	}

	public Task<OperationResponse> DeleteAsync(GraphData item, CancellationToken cancellationToken)
	{
		return Task.FromResult(new OperationResponse { Success = false, ErrorMessage = "Delete not supported" });
	}

	public Task<OperationResponse> UpdateAsync(GraphData item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
	{
		return Task.FromResult(new OperationResponse { Success = false, ErrorMessage = "Update not supported" });
	}

	private static GraphData GenerateInnovationKnowledgeGraph()
	{
		var nodes = new List<GraphNode>();
		AddNodes(nodes, _people, _personCategory);
		AddNodes(nodes, _inventions, _inventionCategory);
		AddNodes(nodes, _organizations, _organizationCategory);
		AddNodes(nodes, _fields, _fieldCategory);
		AddNodes(nodes, _concepts, _conceptCategory);

		var edges = new List<GraphEdge>();
		foreach (var seed in _edgeSeeds)
		{
			// Ensure edge doesn't already exist before adding
			if (!edges.Any(e => e.FromNodeId == seed.From && e.ToNodeId == seed.To))
			{
				edges.Add(CreateEdge(seed));
			}
		}

		return new GraphData
		{
			Nodes = nodes,
			Edges = edges
		};
	}

	private static void AddNodes(List<GraphNode> nodes, NodeSeed[] seeds, double category)
	{
		foreach (var seed in seeds)
		{
			nodes.Add(new GraphNode
			{
				Id = seed.Id,
				Label = seed.Label,
				Dimensions = new Dictionary<string, double>
				{
					["Category"] = category,
					["Era"] = seed.Era,
					["Fame"] = AddNoise(seed.Fame, 0.1),
					["Influence"] = AddNoise(seed.Influence, 0.1),
					["Creativity"] = AddNoise(seed.Creativity, 0.1)
				}
			});
		}
	}

	private static GraphEdge CreateEdge(EdgeSeed seed)
		=> new()
		{
			Id = $"edge_{seed.From}_{seed.To}",
			FromNodeId = seed.From,
			ToNodeId = seed.To,
			Strength = seed.Strength,
			Label = seed.Label,
			Dimensions = new Dictionary<string, double>
			{
				["ConnectionStrength"] = AddNoise(seed.Strength, 0.1),
				["RelationshipType"] = seed.Type,
				["Certainty"] = AddNoise(seed.Certainty, 0.05)
			}
		};

	/// <summary>
	/// Returns a random value in the range [0, 1).
	/// </summary>
	private static double NextDouble() => RandomNumberGenerator.GetInt32(int.MaxValue) / (double)int.MaxValue;

	private static double AddNoise(double value, double maxNoise)
	{
		return Math.Clamp(value + (NextDouble() - 0.5) * maxNoise * 2, 0.0, 1.0);
	}
}