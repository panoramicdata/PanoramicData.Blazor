using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Dummy data item for graph demonstration.
/// </summary>
public class GraphDataItem
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
}

public partial class PDGraphViewerPage : ComponentBase
{
	private PDGraphViewer<GraphDataItem>? _graphViewer;
	private readonly GraphDataProvider _dataProvider = new();
	private GraphVisualizationConfig _visualizationConfig = new();
	private GraphClusteringConfig _clusteringConfig = new();

	// Demo configuration
	private bool _showInfo = true;
	private bool _showControls = true;
	private bool _readOnlyControls = false;
	private SplitDirection _splitDirection = SplitDirection.Horizontal;

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
		if (_graphViewer != null)
		{
			await _graphViewer.RefreshAsync();
		}
		EventManager?.Add(new Event("Graph data refreshed"));
	}

	private async Task OnFitToView()
	{
		if (_graphViewer != null)
		{
			await _graphViewer.FitToViewAsync();
		}
		EventManager?.Add(new Event("Graph fitted to view"));
	}

	private void OnNodeClick(GraphNode node)
	{
		_selectedNode = node;
		_selectedEdge = null;
		EventManager?.Add(new Event($"Node clicked: {node.Label} (ID: {node.Id})"));
		StateHasChanged();
	}

	private void OnEdgeClick(GraphEdge edge)
	{
		_selectedNode = null;
		_selectedEdge = edge;
		EventManager?.Add(new Event($"Edge clicked: {edge.Id} ({edge.FromNodeId} ? {edge.ToNodeId})"));
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
		
		EventManager?.Add(new Event($"Selection changed: {selectionInfo}"));
		StateHasChanged();
	}

	private void OnConfigurationChanged((GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering) config)
	{
		_visualizationConfig = config.Visualization;
		_clusteringConfig = config.Clustering;
		EventManager?.Add(new Event("Graph configuration changed"));
		StateHasChanged();
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

		// Create a fun knowledge graph about innovation, creativity, and historical figures
		var nodeConfigs = new[]
		{
			// People
			new { Id = "einstein", Label = "Albert Einstein", Category = 0.1, Era = 0.3, Fame = 0.95, Influence = 0.9, Creativity = 0.95 },
			new { Id = "davinci", Label = "Leonardo da Vinci", Category = 0.1, Era = 0.1, Fame = 0.9, Influence = 0.85, Creativity = 1.0 },
			new { Id = "jobs", Label = "Steve Jobs", Category = 0.1, Era = 0.8, Fame = 0.9, Influence = 0.8, Creativity = 0.85 },
			new { Id = "tesla", Label = "Nikola Tesla", Category = 0.1, Era = 0.25, Fame = 0.7, Influence = 0.75, Creativity = 0.9 },
			new { Id = "curie", Label = "Marie Curie", Category = 0.1, Era = 0.3, Fame = 0.8, Influence = 0.7, Creativity = 0.8 },
			new { Id = "wozniak", Label = "Steve Wozniak", Category = 0.1, Era = 0.7, Fame = 0.6, Influence = 0.65, Creativity = 0.8 },
			
			// Inventions/Concepts
			new { Id = "relativity", Label = "Theory of Relativity", Category = 0.3, Era = 0.3, Fame = 0.8, Influence = 0.9, Creativity = 0.95 },
			new { Id = "iphone", Label = "iPhone", Category = 0.3, Era = 0.9, Fame = 0.95, Influence = 0.85, Creativity = 0.8 },
			new { Id = "electricity", Label = "Electricity", Category = 0.3, Era = 0.25, Fame = 0.9, Influence = 1.0, Creativity = 0.85 },
			new { Id = "flight", Label = "Powered Flight", Category = 0.3, Era = 0.3, Fame = 0.85, Influence = 0.9, Creativity = 0.9 },
			new { Id = "internet", Label = "Internet", Category = 0.3, Era = 0.7, Fame = 0.9, Influence = 0.95, Creativity = 0.8 },
			new { Id = "radioactivity", Label = "Radioactivity", Category = 0.3, Era = 0.3, Fame = 0.7, Influence = 0.8, Creativity = 0.85 },
			
			// Organizations/Places
			new { Id = "apple", Label = "Apple Inc.", Category = 0.5, Era = 0.8, Fame = 0.9, Influence = 0.8, Creativity = 0.75 },
			new { Id = "princeton", Label = "Princeton University", Category = 0.5, Era = 0.4, Fame = 0.7, Influence = 0.75, Creativity = 0.7 },
			new { Id = "mit", Label = "MIT", Category = 0.5, Era = 0.6, Fame = 0.8, Influence = 0.8, Creativity = 0.85 },
			new { Id = "bell_labs", Label = "Bell Labs", Category = 0.5, Era = 0.5, Fame = 0.6, Influence = 0.7, Creativity = 0.8 },
			
			// Fields/Disciplines
			new { Id = "physics", Label = "Physics", Category = 0.7, Era = 0.5, Fame = 0.7, Influence = 0.9, Creativity = 0.8 },
			new { Id = "art", Label = "Renaissance Art", Category = 0.7, Era = 0.1, Fame = 0.8, Influence = 0.7, Creativity = 0.95 },
			new { Id = "engineering", Label = "Engineering", Category = 0.7, Era = 0.5, Fame = 0.6, Influence = 0.85, Creativity = 0.75 },
			new { Id = "computer_science", Label = "Computer Science", Category = 0.7, Era = 0.7, Fame = 0.7, Influence = 0.9, Creativity = 0.8 },
			
			// Concepts/Ideas
			new { Id = "innovation", Label = "Innovation", Category = 0.9, Era = 0.5, Fame = 0.6, Influence = 0.85, Creativity = 0.9 },
			new { Id = "creativity", Label = "Creativity", Category = 0.9, Era = 0.5, Fame = 0.5, Influence = 0.7, Creativity = 1.0 },
		};

		foreach (var config in nodeConfigs)
		{
			var node = new GraphNode
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
			};
			nodes.Add(node);
		}

		// Create meaningful relationships with semantic edge types
		var edgeConfigs = new[]
		{
			// Einstein relationships
			new { From = "einstein", To = "relativity", Type = 0.1, Strength = 0.95, Certainty = 1.0, Label = "discovered" },
			new { From = "einstein", To = "princeton", Type = 0.3, Strength = 0.8, Certainty = 0.9, Label = "worked at" },
			new { From = "einstein", To = "physics", Type = 0.2, Strength = 0.9, Certainty = 1.0, Label = "advanced" },
			
			// Da Vinci relationships
			new { From = "davinci", To = "art", Type = 0.1, Strength = 0.95, Certainty = 1.0, Label = "mastered" },
			new { From = "davinci", To = "engineering", Type = 0.2, Strength = 0.8, Certainty = 0.8, Label = "pioneered" },
			new { From = "davinci", To = "creativity", Type = 0.4, Strength = 0.9, Certainty = 0.9, Label = "embodied" },
			
			// Steve Jobs relationships
			new { From = "jobs", To = "apple", Type = 0.5, Strength = 0.95, Certainty = 1.0, Label = "co-founded" },
			new { From = "jobs", To = "iphone", Type = 0.1, Strength = 0.9, Certainty = 1.0, Label = "conceived" },
			new { From = "jobs", To = "wozniak", Type = 0.6, Strength = 0.8, Certainty = 1.0, Label = "partnered with" },
			new { From = "jobs", To = "innovation", Type = 0.4, Strength = 0.85, Certainty = 0.9, Label = "championed" },
			
			// Tesla relationships
			new { From = "tesla", To = "electricity", Type = 0.1, Strength = 0.9, Certainty = 1.0, Label = "revolutionized" },
			new { From = "tesla", To = "engineering", Type = 0.2, Strength = 0.85, Certainty = 0.9, Label = "advanced" },
			
			// Marie Curie relationships
			new { From = "curie", To = "radioactivity", Type = 0.1, Strength = 0.95, Certainty = 1.0, Label = "discovered" },
			new { From = "curie", To = "physics", Type = 0.2, Strength = 0.8, Certainty = 1.0, Label = "contributed to" },
			
			// Wozniak relationships
			new { From = "wozniak", To = "apple", Type = 0.5, Strength = 0.9, Certainty = 1.0, Label = "co-founded" },
			new { From = "wozniak", To = "computer_science", Type = 0.2, Strength = 0.8, Certainty = 0.9, Label = "advanced" },
			
			// Organizational relationships
			new { From = "apple", To = "innovation", Type = 0.4, Strength = 0.8, Certainty = 0.8, Label = "drives" },
			new { From = "mit", To = "computer_science", Type = 0.3, Strength = 0.85, Certainty = 0.9, Label = "teaches" },
			new { From = "princeton", To = "physics", Type = 0.3, Strength = 0.8, Certainty = 0.9, Label = "researches" },
			new { From = "bell_labs", To = "internet", Type = 0.1, Strength = 0.7, Certainty = 0.8, Label = "contributed to" },
			
			// Conceptual relationships
			new { From = "creativity", To = "innovation", Type = 0.7, Strength = 0.9, Certainty = 0.8, Label = "enables" },
			new { From = "art", To = "creativity", Type = 0.7, Strength = 0.8, Certainty = 0.9, Label = "expresses" },
			new { From = "physics", To = "relativity", Type = 0.8, Strength = 0.9, Certainty = 1.0, Label = "includes" },
			new { From = "computer_science", To = "internet", Type = 0.8, Strength = 0.85, Certainty = 0.9, Label = "enabled" },
			new { From = "engineering", To = "flight", Type = 0.8, Strength = 0.8, Certainty = 1.0, Label = "achieved" },
			
			// Cross-disciplinary influences
			new { From = "art", To = "engineering", Type = 0.7, Strength = 0.6, Certainty = 0.7, Label = "influences" },
			new { From = "physics", To = "engineering", Type = 0.7, Strength = 0.8, Certainty = 0.9, Label = "informs" },
			new { From = "innovation", To = "iphone", Type = 0.4, Strength = 0.8, Certainty = 0.8, Label = "produced" },
		};

		for (int i = 0; i < edgeConfigs.Length; i++)
		{
			var config = edgeConfigs[i];
			var edge = new GraphEdge
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
			};
			edges.Add(edge);
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