using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Data;

/// <summary>
/// Data provider for graph demonstration with innovation knowledge graph data.
/// </summary>
public class GraphDataProvider : DataProviderBase<GraphData>
{
	public override Task<DataResponse<GraphData>> GetDataAsync(DataRequest<GraphData> request, CancellationToken cancellationToken = default)
	{
		var graphData = CreateInnovationKnowledgeGraph();
		
		// Use the correct constructor: new DataResponse<T>(items, totalCount)
		var response = new DataResponse<GraphData>([graphData], 1);

		return Task.FromResult(response);
	}

	public override Task<OperationResponse> CreateAsync(GraphData item, CancellationToken cancellationToken = default)
	{
		// For demo purposes, just return success
		return Task.FromResult(new OperationResponse { Success = true });
	}

	public override Task<OperationResponse> UpdateAsync(GraphData item, IDictionary<string, object?> delta, CancellationToken cancellationToken = default)
	{
		// For demo purposes, just return success
		return Task.FromResult(new OperationResponse { Success = true });
	}

	public override Task<OperationResponse> DeleteAsync(GraphData item, CancellationToken cancellationToken = default)
	{
		// For demo purposes, just return success
		return Task.FromResult(new OperationResponse { Success = true });
	}

	private static GraphData CreateInnovationKnowledgeGraph()
	{
		var random = new Random(42); // Fixed seed for consistent demo data
		var graphData = new GraphData();

		// Create innovation knowledge graph nodes
		var nodes = new List<GraphNode>
		{
			new() {
				Id = "einstein",
				Label = "Einstein",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.95 },
					{ "Category", 0.8 },
					{ "Era", 0.3 },
					{ "Fame", 0.98 },
					{ "Creativity", 0.92 }
				}
			},
			new() {
				Id = "davinci",
				Label = "da Vinci",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.88 },
					{ "Category", 0.2 },
					{ "Era", 0.1 },
					{ "Fame", 0.85 },
					{ "Creativity", 0.98 }
				}
			},
			new() {
				Id = "tesla",
				Label = "Tesla",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.82 },
					{ "Category", 0.9 },
					{ "Era", 0.4 },
					{ "Fame", 0.75 },
					{ "Creativity", 0.88 }
				}
			},
			new() {
				Id = "jobs",
				Label = "Steve Jobs",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.78 },
					{ "Category", 0.95 },
					{ "Era", 0.9 },
					{ "Fame", 0.92 },
					{ "Creativity", 0.85 }
				}
			},
			new() {
				Id = "curie",
				Label = "Marie Curie",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.85 },
					{ "Category", 0.75 },
					{ "Era", 0.35 },
					{ "Fame", 0.88 },
					{ "Creativity", 0.82 }
				}
			},
			new() {
				Id = "edison",
				Label = "Thomas Edison",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.72 },
					{ "Category", 0.85 },
					{ "Era", 0.25 },
					{ "Fame", 0.78 },
					{ "Creativity", 0.75 }
				}
			},
			new() {
				Id = "gates",
				Label = "Bill Gates",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.68 },
					{ "Category", 0.98 },
					{ "Era", 0.85 },
					{ "Fame", 0.88 },
					{ "Creativity", 0.72 }
				}
			},
			new() {
				Id = "hawking",
				Label = "Stephen Hawking",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.75 },
					{ "Category", 0.82 },
					{ "Era", 0.7 },
					{ "Fame", 0.85 },
					{ "Creativity", 0.88 }
				}
			},
			new() {
				Id = "newton",
				Label = "Isaac Newton",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.98 },
					{ "Category", 0.78 },
					{ "Era", 0.05 },
					{ "Fame", 0.92 },
					{ "Creativity", 0.95 }
				}
			},
			new() {
				Id = "wozniak",
				Label = "Steve Wozniak",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.58 },
					{ "Category", 0.92 },
					{ "Era", 0.88 },
					{ "Fame", 0.62 },
					{ "Creativity", 0.88 }
				}
			},
			new() {
				Id = "berners-lee",
				Label = "Tim Berners-Lee",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.85 },
					{ "Category", 0.88 },
					{ "Era", 0.78 },
					{ "Fame", 0.68 },
					{ "Creativity", 0.82 }
				}
			},
			new() {
				Id = "turing",
				Label = "Alan Turing",
				Dimensions = new Dictionary<string, double> {
					{ "Influence", 0.88 },
					{ "Category", 0.85 },
					{ "Era", 0.55 },
					{ "Fame", 0.72 },
					{ "Creativity", 0.92 }
				}
			}
		};

		// Create edges representing relationships/influences
		var edges = new List<GraphEdge>
		{
			new() {
				Id = "newton-einstein",
				FromNodeId = "newton",
				ToNodeId = "einstein",
				Label = "influences",
				Strength = 0.9,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.9 },
					{ "RelationshipType", 0.8 },
					{ "Certainty", 0.95 }
				}
			},
			new() {
				Id = "einstein-hawking",
				FromNodeId = "einstein",
				ToNodeId = "hawking",
				Label = "influences",
				Strength = 0.8,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.8 },
					{ "RelationshipType", 0.8 },
					{ "Certainty", 0.9 }
				}
			},
			new() {
				Id = "tesla-edison",
				FromNodeId = "tesla",
				ToNodeId = "edison",
				Label = "collaborates",
				Strength = 0.6,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.6 },
					{ "RelationshipType", 0.4 },
					{ "Certainty", 0.85 }
				}
			},
			new() {
				Id = "jobs-wozniak",
				FromNodeId = "jobs",
				ToNodeId = "wozniak",
				Label = "co-founded",
				Strength = 0.95,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.95 },
					{ "RelationshipType", 0.9 },
					{ "Certainty", 1.0 }
				}
			},
			new() {
				Id = "turing-berners-lee",
				FromNodeId = "turing",
				ToNodeId = "berners-lee",
				Label = "influences",
				Strength = 0.7,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.7 },
					{ "RelationshipType", 0.8 },
					{ "Certainty", 0.8 }
				}
			},
			new() {
				Id = "davinci-tesla",
				FromNodeId = "davinci",
				ToNodeId = "tesla",
				Label = "inspires",
				Strength = 0.5,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.5 },
					{ "RelationshipType", 0.6 },
					{ "Certainty", 0.7 }
				}
			},
			new() {
				Id = "curie-hawking",
				FromNodeId = "curie",
				ToNodeId = "hawking",
				Label = "scientific legacy",
				Strength = 0.6,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.6 },
					{ "RelationshipType", 0.8 },
					{ "Certainty", 0.75 }
				}
			},
			new() {
				Id = "gates-jobs",
				FromNodeId = "gates",
				ToNodeId = "jobs",
				Label = "rivals",
				Strength = 0.8,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.8 },
					{ "RelationshipType", 0.2 },
					{ "Certainty", 0.95 }
				}
			},
			new() {
				Id = "edison-tesla-conflict",
				FromNodeId = "edison",
				ToNodeId = "tesla",
				Label = "AC/DC war",
				Strength = 0.7,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.7 },
					{ "RelationshipType", 0.1 },
					{ "Certainty", 0.9 }
				}
			},
			new() {
				Id = "newton-davinci",
				FromNodeId = "newton",
				ToNodeId = "davinci",
				Label = "scientific method",
				Strength = 0.4,
				Dimensions = new Dictionary<string, double> {
					{ "ConnectionStrength", 0.4 },
					{ "RelationshipType", 0.7 },
					{ "Certainty", 0.6 }
				}
			}
		};

		graphData.Nodes.AddRange(nodes);
		graphData.Edges.AddRange(edges);

		return graphData;
	}
}