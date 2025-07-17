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
	private PDGraphViewer<GraphDataItem>? _graphViewer;
	private readonly GraphDataProvider _dataProvider = new();
	private GraphVisualizationConfig _visualizationConfig = new();
	private GraphClusteringConfig _clusteringConfig = new();
	private double _damping = 0.95;
	private GraphData? _currentGraphData; // To store the current graph data

	// Demo configuration
	private bool _showInfo = true;
	private bool _showControls = true;
	private bool _readOnlyControls = false;
	private SplitDirection _splitDirection = SplitDirection.Horizontal;

	// ✅ FIXED: Simple convergence threshold handling
	private double _convergenceThreshold = 0.08;
	private bool _isUpdatingControls = false;

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
			var nodesChanged = _currentGraphData == null ||
				!_currentGraphData.Nodes.Select(n => n.Id).OrderBy(id => id).SequenceEqual(newGraphData.Nodes.Select(n => n.Id).OrderBy(id => id));
			var edgesChanged = _currentGraphData == null ||
				!_currentGraphData.Edges.Select(e => e.Id).OrderBy(id => id).SequenceEqual(newGraphData.Edges.Select(e => e.Id).OrderBy(id => id));

			_currentGraphData = newGraphData; // Update current data reference

			if (_graphViewer != null)
			{
				if (nodesChanged || edgesChanged)
				{
					// If nodes or edges have changed, force a full refresh (regenerate layout)
					Console.WriteLine("PDGraphViewerPage: Nodes or edges changed, performing full refresh.");
					await _graphViewer.RefreshAsync().ConfigureAwait(false);
				}
				else
				{
					// If only data within existing nodes/edges changed, update configuration (preserve layout)
					Console.WriteLine("PDGraphViewerPage: Only data changed, updating configuration.");
					await _graphViewer.UpdateConfigurationAsync((_visualizationConfig, _clusteringConfig, _damping)).ConfigureAwait(false);
				}
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

	private async Task OnFitToView()
	{
		if (_isUpdatingControls)
		{
			return;
		}

		_isUpdatingControls = true;
		try
		{
			if (_graphViewer != null)
			{
				await _graphViewer.FitToViewAsync();
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
		// No cleanup needed
	}
}

/// <summary>
/// Sample data provider for a fun knowledge graph about innovation and creativity.
/// </summary>
public class GraphDataProvider : IDataProviderService<GraphData>
{
	private readonly Random _random = new();

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

	private GraphData GenerateInnovationKnowledgeGraph()
	{
		var nodes = new List<GraphNode>();
		var edges = new List<GraphEdge>();
		var nodeConfigs = new List<dynamic>();

		// Define categories for a more structured graph
		var categories = new Dictionary<string, double>
		{
			{ "Person", 0.1 },
			{ "Invention", 0.3 },
			{ "Organization", 0.5 },
			{ "Field", 0.7 },
			{ "Concept", 0.9 }
		};

		// --- People ---
		var people = new[]
		{
			new { Id = "einstein", Label = "Albert Einstein", Era = 0.3, Fame = 0.95, Influence = 0.9, Creativity = 0.95 },
			new { Id = "davinci", Label = "Leonardo da Vinci", Era = 0.1, Fame = 0.9, Influence = 0.85, Creativity = 1.0 },
			new { Id = "jobs", Label = "Steve Jobs", Era = 0.8, Fame = 0.9, Influence = 0.8, Creativity = 0.85 },
			new { Id = "tesla", Label = "Nikola Tesla", Era = 0.25, Fame = 0.7, Influence = 0.75, Creativity = 0.9 },
			new { Id = "curie", Label = "Marie Curie", Era = 0.3, Fame = 0.8, Influence = 0.7, Creativity = 0.8 },
			new { Id = "wozniak", Label = "Steve Wozniak", Era = 0.7, Fame = 0.6, Influence = 0.65, Creativity = 0.8 },
			new { Id = "turing", Label = "Alan Turing", Era = 0.4, Fame = 0.8, Influence = 0.85, Creativity = 0.95 },
			new { Id = "lovelace", Label = "Ada Lovelace", Era = 0.2, Fame = 0.6, Influence = 0.7, Creativity = 0.9 },
			new { Id = "newton", Label = "Isaac Newton", Era = 0.15, Fame = 0.9, Influence = 0.95, Creativity = 0.9 },
			new { Id = "galileo", Label = "Galileo Galilei", Era = 0.1, Fame = 0.8, Influence = 0.8, Creativity = 0.85 }
		};
		foreach (var p in people)
		{
			nodeConfigs.Add(new { p.Id, p.Label, Category = categories["Person"], p.Era, p.Fame, p.Influence, p.Creativity });
		}

		// --- Inventions/Concepts ---
		var inventions = new[]
		{
			new { Id = "relativity", Label = "Theory of Relativity", Era = 0.3, Fame = 0.8, Influence = 0.9, Creativity = 0.95 },
			new { Id = "iphone", Label = "iPhone", Era = 0.9, Fame = 0.95, Influence = 0.85, Creativity = 0.8 },
			new { Id = "electricity", Label = "AC Electricity", Era = 0.25, Fame = 0.9, Influence = 1.0, Creativity = 0.85 },
			new { Id = "flight", Label = "Powered Flight", Era = 0.3, Fame = 0.85, Influence = 0.9, Creativity = 0.9 },
			new { Id = "internet", Label = "Internet", Era = 0.7, Fame = 0.9, Influence = 0.95, Creativity = 0.8 },
			new { Id = "radioactivity", Label = "Radioactivity", Era = 0.3, Fame = 0.7, Influence = 0.8, Creativity = 0.85 },
			new { Id = "turing_machine", Label = "Turing Machine", Era = 0.4, Fame = 0.7, Influence = 0.9, Creativity = 0.95 },
			new { Id = "analytical_engine", Label = "Analytical Engine", Era = 0.2, Fame = 0.5, Influence = 0.8, Creativity = 0.9 },
			new { Id = "calculus", Label = "Calculus", Era = 0.15, Fame = 0.8, Influence = 0.95, Creativity = 0.9 },
			new { Id = "telescope", Label = "Telescope", Era = 0.1, Fame = 0.7, Influence = 0.8, Creativity = 0.8 }
		};
		foreach (var i in inventions)
		{
			nodeConfigs.Add(new { i.Id, i.Label, Category = categories["Invention"], i.Era, i.Fame, i.Influence, i.Creativity });
		}

		// --- Organizations/Places ---
		var organizations = new[]
		{
			new { Id = "apple", Label = "Apple Inc.", Era = 0.8, Fame = 0.9, Influence = 0.8, Creativity = 0.75 },
			new { Id = "princeton", Label = "Princeton University", Era = 0.4, Fame = 0.7, Influence = 0.75, Creativity = 0.7 },
			new { Id = "mit", Label = "MIT", Era = 0.6, Fame = 0.8, Influence = 0.8, Creativity = 0.85 },
			new { Id = "bell_labs", Label = "Bell Labs", Era = 0.5, Fame = 0.6, Influence = 0.7, Creativity = 0.8 },
			new { Id = "bletchley_park", Label = "Bletchley Park", Era = 0.4, Fame = 0.7, Influence = 0.8, Creativity = 0.9 },
			new { Id = "cambridge", Label = "University of Cambridge", Era = 0.2, Fame = 0.8, Influence = 0.85, Creativity = 0.8 },
			new { Id = "xerox_parc", Label = "Xerox PARC", Era = 0.7, Fame = 0.7, Influence = 0.8, Creativity = 0.85 }
		};
		foreach (var o in organizations)
		{
			nodeConfigs.Add(new { o.Id, o.Label, Category = categories["Organization"], o.Era, o.Fame, o.Influence, o.Creativity });
		}

		// --- Fields/Disciplines ---
		var fields = new[]
		{
			new { Id = "physics", Label = "Physics", Era = 0.5, Fame = 0.7, Influence = 0.9, Creativity = 0.8 },
			new { Id = "art", Label = "Renaissance Art", Era = 0.1, Fame = 0.8, Influence = 0.7, Creativity = 0.95 },
			new { Id = "engineering", Label = "Engineering", Era = 0.5, Fame = 0.6, Influence = 0.85, Creativity = 0.75 },
			new { Id = "computer_science", Label = "Computer Science", Era = 0.7, Fame = 0.7, Influence = 0.9, Creativity = 0.8 },
			new { Id = "mathematics", Label = "Mathematics", Era = 0.4, Fame = 0.8, Influence = 0.9, Creativity = 0.85 },
			new { Id = "astronomy", Label = "Astronomy", Era = 0.3, Fame = 0.7, Influence = 0.8, Creativity = 0.75 },
			new { Id = "cryptography", Label = "Cryptography", Era = 0.5, Fame = 0.6, Influence = 0.7, Creativity = 0.8 }
		};
		foreach (var f in fields)
		{
			nodeConfigs.Add(new { f.Id, f.Label, Category = categories["Field"], f.Era, f.Fame, f.Influence, f.Creativity });
		}

		// --- Concepts/Ideas ---
		var concepts = new[]
		{
			new { Id = "innovation", Label = "Innovation", Era = 0.5, Fame = 0.6, Influence = 0.85, Creativity = 0.9 },
			new { Id = "creativity", Label = "Creativity", Era = 0.5, Fame = 0.5, Influence = 0.7, Creativity = 1.0 },
			new { Id = "computation", Label = "Computation", Era = 0.6, Fame = 0.7, Influence = 0.85, Creativity = 0.8 },
			new { Id = "relativity_principle", Label = "Principle of Relativity", Era = 0.3, Fame = 0.8, Influence = 0.9, Creativity = 0.9 },
			new { Id = "user_interface", Label = "User Interface", Era = 0.7, Fame = 0.6, Influence = 0.7, Creativity = 0.8 }
		};
		foreach (var c in concepts)
		{
			nodeConfigs.Add(new { c.Id, c.Label, Category = categories["Concept"], c.Era, c.Fame, c.Influence, c.Creativity });
		}

		//// --- Generate a large number of additional "follower" nodes to test performance ---
		//int followerCount = 50;
		//for (int i = 0; i < followerCount; i++)
		//{
		//	var followerId = $"follower_{i}";
		//	var connectedTo = nodeConfigs[_random.Next(nodeConfigs.Count)];
		//	nodeConfigs.Add(new
		//	{
		//		Id = followerId,
		//		Label = $"Follower {i + 1}",
		//		Category = categories["Person"],
		//		Era = AddNoise(connectedTo.Era, 0.1),
		//		Fame = AddNoise(0.2, 0.1),
		//		Influence = AddNoise(0.1, 0.1),
		//		Creativity = AddNoise(0.3, 0.2)
		//	});
		//}

		// --- Create all nodes ---
		foreach (var config in nodeConfigs)
		{
			nodes.Add(new GraphNode
			{
				Id = config.Id,
				Label = config.Label,
				Dimensions = new Dictionary<string, double>
				{
					["Category"] = config.Category,
					["Era"] = config.Era,
					["Fame"] = AddNoise(config.Fame, 0.1),
					["Influence"] = AddNoise(config.Influence, 0.1),
					["Creativity"] = AddNoise(config.Creativity, 0.1)
				}
			});
		}

		// --- Create a rich set of edges with semantic meaning ---
		var edgeConfigs = new List<dynamic>
		{
			// Foundational relationships
			new { From = "newton", To = "calculus", Type = 0.1, Strength = 0.95, Certainty = 1.0, Label = "developed" },
			new { From = "galileo", To = "telescope", Type = 0.1, Strength = 0.9, Certainty = 1.0, Label = "improved" },
			new { From = "davinci", To = "art", Type = 0.1, Strength = 0.95, Certainty = 1.0, Label = "mastered" },
			new { From = "davinci", To = "engineering", Type = 0.2, Strength = 0.8, Certainty = 0.8, Label = "pioneered" },

			// Physics and Math
			new { From = "einstein", To = "relativity", Type = 0.1, Strength = 0.95, Certainty = 1.0, Label = "discovered" },
			new { From = "einstein", To = "physics", Type = 0.2, Strength = 0.9, Certainty = 1.0, Label = "advanced" },
			new { From = "einstein", To = "newton", Type = 0.7, Strength = 0.8, Certainty = 0.9, Label = "built upon work of" },
			new { From = "curie", To = "radioactivity", Type = 0.1, Strength = 0.95, Certainty = 1.0, Label = "discovered" },
			new { From = "curie", To = "physics", Type = 0.2, Strength = 0.8, Certainty = 1.0, Label = "contributed to" },
			new { From = "calculus", To = "physics", Type = 0.8, Strength = 0.9, Certainty = 1.0, Label = "is fundamental to" },
			new { From = "relativity", To = "relativity_principle", Type = 0.8, Strength = 0.9, Certainty = 1.0, Label = "is based on" },

			// Computer Science
			new { From = "lovelace", To = "analytical_engine", Type = 0.1, Strength = 0.8, Certainty = 0.9, Label = "wrote algorithm for" },
			new { From = "turing", To = "turing_machine", Type = 0.1, Strength = 0.95, Certainty = 1.0, Label = "formalized" },
			new { From = "turing", To = "computer_science", Type = 0.2, Strength = 0.9, Certainty = 1.0, Label = "is father of" },
			new { From = "turing", To = "cryptography", Type = 0.2, Strength = 0.85, Certainty = 0.9, Label = "applied" },
			new { From = "turing_machine", To = "computation", Type = 0.8, Strength = 0.9, Certainty = 1.0, Label = "defines" },
			new { From = "jobs", To = "apple", Type = 0.5, Strength = 0.95, Certainty = 1.0, Label = "co-founded" },
			new { From = "wozniak", To = "apple", Type = 0.5, Strength = 0.9, Certainty = 1.0, Label = "co-founded" },
			new { From = "jobs", To = "wozniak", Type = 0.6, Strength = 0.8, Certainty = 1.0, Label = "partnered with" },
			new { From = "apple", To = "iphone", Type = 0.1, Strength = 0.9, Certainty = 1.0, Label = "developed" },
			new { From = "xerox_parc", To = "user_interface", Type = 0.1, Strength = 0.8, Certainty = 0.9, Label = "pioneered" },
			new { From = "apple", To = "xerox_parc", Type = 0.7, Strength = 0.7, Certainty = 0.8, Label = "was influenced by" },

			// Institutional connections
			new { From = "einstein", To = "princeton", Type = 0.3, Strength = 0.8, Certainty = 0.9, Label = "worked at" },
			new { From = "turing", To = "cambridge", Type = 0.3, Strength = 0.8, Certainty = 0.9, Label = "studied at" },
			new { From = "turing", To = "bletchley_park", Type = 0.3, Strength = 0.9, Certainty = 1.0, Label = "worked at" },
			new { From = "newton", To = "cambridge", Type = 0.3, Strength = 0.85, Certainty = 1.0, Label = "was a fellow of" },
			new { From = "bell_labs", To = "internet", Type = 0.1, Strength = 0.7, Certainty = 0.8, Label = "contributed to" },
			new { From = "mit", To = "computer_science", Type = 0.3, Strength = 0.85, Certainty = 0.9, Label = "is a leader in" },

			// Conceptual links
			new { From = "creativity", To = "innovation", Type = 0.7, Strength = 0.9, Certainty = 0.8, Label = "enables" },
			new { From = "art", To = "creativity", Type = 0.7, Strength = 0.8, Certainty = 0.9, Label = "expresses" },
			new { From = "innovation", To = "iphone", Type = 0.4, Strength = 0.8, Certainty = 0.8, Label = "produced" },
			new { From = "engineering", To = "flight", Type = 0.8, Strength = 0.8, Certainty = 1.0, Label = "achieved" },
			new { From = "physics", To = "engineering", Type = 0.7, Strength = 0.8, Certainty = 0.9, Label = "informs" },
			new { From = "mathematics", To = "computer_science", Type = 0.7, Strength = 0.9, Certainty = 1.0, Label = "is the foundation of" }
		};

		//// --- Add edges for "follower" nodes ---
		//for (int i = 0; i < followerCount; i++)
		//{
		//	var followerId = $"follower_{i}";
		//	// Connect each follower to 1-3 random core nodes
		//	int connections = _random.Next(1, 4);
		//	for (int j = 0; j < connections; j++)
		//	{
		//		var connectedTo = nodeConfigs[_random.Next(people.Length + inventions.Length)]; // Connect to people or inventions
		//		edgeConfigs.Add(new
		//		{
		//			From = followerId,
		//			To = connectedTo.Id,
		//			Type = 0.6, // "follower of"
		//			Strength = AddNoise(0.4, 0.2),
		//			Certainty = AddNoise(0.7, 0.2),
		//			Label = "is influenced by"
		//		});
		//	}
		//}

		// --- Create all edges ---
		foreach (var config in edgeConfigs)
		{
			// Ensure edge doesn't already exist before adding
			if (!edges.Any(e => e.FromNodeId == config.From && e.ToNodeId == config.To))
			{
				edges.Add(new GraphEdge
				{
					Id = $"edge_{config.From}_{config.To}",
					FromNodeId = config.From,
					ToNodeId = config.To,
					Strength = config.Strength,
					Label = config.Label,
					Dimensions = new Dictionary<string, double>
					{
						["ConnectionStrength"] = AddNoise(config.Strength, 0.1),
						["RelationshipType"] = config.Type,
						["Certainty"] = AddNoise(config.Certainty, 0.05)
					}
				});
			}
		}

		return new GraphData
		{
			Nodes = nodes,
			Edges = edges
		};
	}

	private double AddNoise(double value, double maxNoise)
	{
		return Math.Clamp(value + (_random.NextDouble() - 0.5) * maxNoise * 2, 0.0, 1.0);
	}
}