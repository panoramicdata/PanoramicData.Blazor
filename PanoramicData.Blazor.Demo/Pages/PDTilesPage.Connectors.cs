using System.Security.Cryptography;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Connector generation for the PDTiles demo.
/// </summary>
public partial class PDTilesPage
{
	private List<TileConnector> _connectors = [];

	private void OnRandomizeConnectors()
	{
		_connectors = TilesComponent?.GenerateRandomConnectors() ?? GenerateDefaultConnectors();
		UpdateUrl();
		StateHasChanged();
	}

	private void OnClearConnectors()
	{
		_connectors = [];
		StateHasChanged();
	}

	private List<TileConnector> GenerateDefaultConnectors()
	{
		// Create a simple set of connectors for initial display
		var connectors = new List<TileConnector>();
		var colors = new[] { "#00FFFF", "#FF00FF", "#00FF00", "#FF6600", "#FFFF00" };

		for (var row = 0; row < _options.Rows; row++)
		{
			for (var col = 0; col < _options.Columns; col++)
			{
				// Connect to right neighbor
				if (col < _options.Columns - 1 && RandomNumberGenerator.GetInt32(100) < _connectorOptions.Population)
				{
					connectors.Add(new TileConnector
					{
						StartTile = new TileCoordinate { Column = col, Row = row },
						EndTile = new TileCoordinate { Column = col + 1, Row = row },
						Direction = "right",
						Color = colors[connectors.Count % colors.Length],
						Opacity = _connectorOptions.Opacity,
						FillPattern = ConnectorFillPattern.Solid
					});
				}

				// Connect to bottom neighbor
				if (row < _options.Rows - 1 && RandomNumberGenerator.GetInt32(100) < _connectorOptions.Population)
				{
					connectors.Add(new TileConnector
					{
						StartTile = new TileCoordinate { Column = col, Row = row },
						EndTile = new TileCoordinate { Column = col, Row = row + 1 },
						Direction = "down",
						Color = colors[connectors.Count % colors.Length],
						Opacity = _connectorOptions.Opacity,
						FillPattern = ConnectorFillPattern.Solid
					});
				}
			}
		}

		return connectors;
	}
}
