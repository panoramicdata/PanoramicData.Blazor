using PanoramicData.Blazor.Models.Tiles;
using System.Security.Cryptography;

namespace PanoramicData.Blazor;

/// <summary>
/// PDTiles: random tile population, per-tile overrides and random connector generation.
/// </summary>
public partial class PDTiles
{
	private List<string?> _tileLogos = [];
	private List<bool> _tileVisible = [];

	// The generated (random) logo and visibility of each tile, before per-tile overrides are applied
	private List<string?> _generatedLogos = [];
	private List<bool> _generatedVisible = [];

	// Track last known grid configuration to avoid re-randomizing on every render
	private int _lastColumns;
	private int _lastRows;
	private int _lastPopulation;
	private int _lastLogoCount;

	/// <summary>
	/// The neighbours of a tile in straight-line mode, in the order they are offered for connection.
	/// </summary>
	private static readonly (int ColumnOffset, int RowOffset, string Type)[] _neighbourOffsets =
	[
		(-1, 0, "left"),
		(1, 0, "right"),
		(0, -1, "up"),
		(0, 1, "down"),
		(1, 1, "diag-front"),
		(-1, -1, "diag-back"),
		(1, -1, "diag-right"),
		(-1, 1, "diag-left")
	];

	/// <summary>
	/// Returns a random integer in [0, <paramref name="maxExclusive"/>). The randomness only drives the visual
	/// layout (logo order, tile population, connectors), which has no reproducibility requirement.
	/// </summary>
	private static int NextRandom(int maxExclusive) => RandomNumberGenerator.GetInt32(maxExclusive);

	/// <summary>
	/// Returns a random integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
	/// </summary>
	private static int NextRandom(int minInclusive, int maxExclusive) => RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);

	/// <summary>
	/// Returns the items in a random order.
	/// </summary>
	private static List<T> InRandomOrder<T>(IEnumerable<T> items) => [.. items.OrderBy(_ => NextRandom(int.MaxValue))];

	private void InitializeTiles()
	{
		var totalTiles = Options.Columns * Options.Rows;
		var logoCount = Logos?.Count ?? 0;

		// Only re-randomize the generated state when the grid configuration changes
		if (HasGridConfigurationChanged(logoCount) || _generatedVisible.Count != totalTiles || _generatedLogos.Count != totalTiles)
		{
			_lastColumns = Options.Columns;
			_lastRows = Options.Rows;
			_lastPopulation = Options.Population;
			_lastLogoCount = logoCount;
			GenerateTiles(totalTiles);
		}

		// Overrides are re-applied on every parameter set, so tile definitions supplied, removed or changed
		// after the first render take effect without re-randomizing the other tiles
		ApplyTileOverrides(totalTiles);
	}

	private bool HasGridConfigurationChanged(int logoCount)
		=> _lastColumns != Options.Columns
			|| _lastRows != Options.Rows
			|| _lastPopulation != Options.Population
			|| _lastLogoCount != logoCount;

	private void GenerateTiles(int totalTiles)
	{
		// Logos: shuffle and assign. With no logos the tiles are drawn without one.
		var shuffledLogos = InRandomOrder(Logos ?? []);
		_generatedLogos = [];
		for (var i = 0; i < totalTiles; i++)
		{
			_generatedLogos.Add(shuffledLogos.Count == 0 ? null : shuffledLogos[i % shuffledLogos.Count]);
		}

		// Visibility based on population
		_generatedVisible = [];
		var visibleCount = (int)Math.Ceiling(totalTiles * (Options.Population / 100.0));
		var indices = InRandomOrder(Enumerable.Range(0, totalTiles));
		for (var i = 0; i < totalTiles; i++)
		{
			_generatedVisible.Add(indices.IndexOf(i) < visibleCount);
		}
	}

	private void ApplyTileOverrides(int totalTiles)
	{
		_tileOverrides.Clear();
		foreach (var tile in Tiles ?? [])
		{
			_tileOverrides[$"{tile.Column},{tile.Row}"] = tile;
		}

		_tileLogos = [];
		_tileVisible = [];
		for (var i = 0; i < totalTiles; i++)
		{
			var col = i % Options.Columns;
			var row = i / Options.Columns;
			_tileOverrides.TryGetValue($"{col},{row}", out var tileDef);
			_tileLogos.Add(string.IsNullOrEmpty(tileDef?.Logo) ? _generatedLogos[i] : tileDef.Logo);
			_tileVisible.Add(tileDef?.Visible ?? _generatedVisible[i]);
		}

		// Pre-populate per-tile color gradients so <defs> are available on first render.
		// Without this, the gradient <linearGradient> elements would be missing on the first
		// render pass because EnsureGradients() is called during tile rendering which occurs
		// after the <defs> section in the markup.
		_tileColorGradients.Clear();
		EnsureGradients(Options.TileColor);
		foreach (var tileDef in _tileOverrides.Values)
		{
			if (!string.IsNullOrEmpty(tileDef.Color))
			{
				EnsureGradients(tileDef.Color);
			}
		}
	}

	/// <summary>
	/// Shuffles the tile logos randomly.
	/// </summary>
	public void Shuffle()
	{
		_tileLogos = InRandomOrder(_tileLogos);
		// Keep the shuffled order when the overrides are next re-applied
		_generatedLogos = [.. _tileLogos];
		StateHasChanged();
	}

	/// <summary>
	/// Generates random connectors based on current options.
	/// </summary>
	public List<TileConnector> GenerateRandomConnectors()
	{
		var result = new List<TileConnector>();
		var added = new HashSet<string>();

		for (var row = 0; row < Options.Rows; row++)
		{
			for (var col = 0; col < Options.Columns; col++)
			{
				// Skip invisible tiles
				if (!IsTileVisible(row * Options.Columns + col))
				{
					continue;
				}

				// Get connectable tiles based on connection mode
				foreach (var adj in GetConnectableTiles(col, row))
				{
					AddRandomConnectors(result, added, col, row, adj);
				}
			}
		}

		return result;
	}

	private bool IsTileVisible(int tileId) => tileId < _tileVisible.Count && _tileVisible[tileId];

	/// <summary>
	/// Adds the random connectors (if any) between a visible tile and one of its connectable tiles.
	/// Each pair of tiles is considered once, whichever end it is reached from.
	/// </summary>
	private void AddRandomConnectors(List<TileConnector> result, HashSet<string> added, int col, int row, AdjacentTile adj)
	{
		var startId = row * Options.Columns + col;
		var endId = adj.Row * Options.Columns + adj.Column;

		// Skip invisible tiles and, for StraightLine mode, directions excluded by the direction filter
		if (!IsTileVisible(endId) || !IsDirectionAllowed(adj.Type))
		{
			return;
		}

		if (!added.Add($"{Math.Min(startId, endId)}-{Math.Max(startId, endId)}"))
		{
			return;
		}

		var numConn = GetEdgeConnectorCount(adj.Type);

		if (!IsPairPopulated())
		{
			return;
		}

		for (var ci = 0; ci < numConn; ci++)
		{
			result.Add(CreateRandomConnector(col, row, adj, ci, numConn, result.Count));
		}
	}

	/// <summary>
	/// Diagonal neighbours get a single connector; edge neighbours get <see cref="TileConnectorOptions.PerEdge"/>
	/// connectors, or a random 0 to 4 when that is not set.
	/// </summary>
	private int GetEdgeConnectorCount(string type)
		=> type.StartsWith("diag-", StringComparison.Ordinal) ? 1 : (ConnectorOptions.PerEdge ?? NextRandom(0, 5));

	/// <summary>
	/// Randomly decides whether a pair of tiles is connected, according to the connector population percentage.
	/// </summary>
	private bool IsPairPopulated() => ConnectorOptions.Population >= 100 || NextRandom(100) < ConnectorOptions.Population;

	private bool IsDirectionAllowed(string type)
		=> ConnectorOptions.ConnectionMode != ConnectionMode.StraightLine || MatchesDirection(type, ConnectorOptions.Direction);

	private TileConnector CreateRandomConnector(int col, int row, AdjacentTile adj, int edgeIndex, int edgeTotal, int connectorIndex)
	{
		var chosenPattern = ConnectorOptions.FillPattern == ConnectorFillPattern.Random
			? (ConnectorFillPattern)NextRandom(1, 4) // Skip Random (0)
			: ConnectorOptions.FillPattern;

		return new TileConnector
		{
			StartTile = new TileCoordinate { Column = col, Row = row },
			EndTile = new TileCoordinate { Column = adj.Column, Row = adj.Row },
			Direction = adj.Type,
			Reversed = NextRandom(2) == 0,
			Color = _connectorColors[connectorIndex % _connectorColors.Length],
			Opacity = ConnectorOptions.Opacity,
			AnimationSpeed = ConnectorOptions.AnimationSpeed,
			FillPattern = chosenPattern,
			EdgeIndex = edgeIndex,
			EdgeTotal = edgeTotal,
			Height = ConnectorOptions.Height,
			VerticalAlign = ConnectorOptions.VerticalAlign
		};
	}

	private List<AdjacentTile> GetAdjacentTiles(int col, int row)
	{
		var adj = new List<AdjacentTile>();
		foreach (var (columnOffset, rowOffset, type) in _neighbourOffsets)
		{
			var adjCol = col + columnOffset;
			var adjRow = row + rowOffset;
			if (adjCol >= 0 && adjCol < Options.Columns && adjRow >= 0 && adjRow < Options.Rows)
			{
				adj.Add(new AdjacentTile(adjCol, adjRow, type));
			}
		}

		return adj;
	}

	private static bool MatchesDirection(string type, ConnectorDirection dirFilter) => dirFilter switch
	{
		ConnectorDirection.Orthogonal => !type.StartsWith("diag-", StringComparison.Ordinal),
		ConnectorDirection.Diagonal => type.StartsWith("diag-", StringComparison.Ordinal),
		ConnectorDirection.DiagonalLeftRight => type is "diag-right" or "diag-left",
		ConnectorDirection.DiagonalFrontBack => type is "diag-front" or "diag-back",
		_ => true // All (and any unrecognised filter) matches every direction
	};

	/// <summary>
	/// Gets the tiles that can be connected to in the current connection mode.
	/// </summary>
	private List<AdjacentTile> GetConnectableTiles(int col, int row)
	{
		return ConnectorOptions.ConnectionMode switch
		{
			ConnectionMode.RowCurves => GetRowConnectableTiles(row),
			ConnectionMode.ColumnCurves => GetColumnConnectableTiles(col),
			_ => GetAdjacentTiles(col, row) // StraightLine uses existing adjacent tile logic
		};
	}

	private List<AdjacentTile> GetRowConnectableTiles(int row)
	{
		var tiles = new List<AdjacentTile>();

		// RowCurves: Connect to tiles in adjacent rows (row +/- 1), any column
		// Use standard "up"/"down" directions so attachment points work the same as straight lines
		foreach (var targetRow in new[] { row - 1, row + 1 }.Where(r => r >= 0 && r < Options.Rows))
		{
			var direction = targetRow < row ? "up" : "down";
			for (var c = 0; c < Options.Columns; c++)
			{
				tiles.Add(new AdjacentTile(c, targetRow, direction));
			}
		}

		return tiles;
	}

	private List<AdjacentTile> GetColumnConnectableTiles(int col)
	{
		var tiles = new List<AdjacentTile>();

		// ColumnCurves: Connect to tiles in adjacent columns (col +/- 1), any row
		// Use standard "left"/"right" directions so attachment points work the same as straight lines
		foreach (var targetCol in new[] { col - 1, col + 1 }.Where(c => c >= 0 && c < Options.Columns))
		{
			var direction = targetCol < col ? "left" : "right";
			for (var r = 0; r < Options.Rows; r++)
			{
				tiles.Add(new AdjacentTile(targetCol, r, direction));
			}
		}

		return tiles;
	}
}
