using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor;

/// <summary>
/// PDTiles: per-tile property overrides, colours, gradients and names.
/// </summary>
public partial class PDTiles
{
	private T GetTileProperty<T>(int col, int row, Func<TileDefinition, T?> selector, T defaultValue) where T : struct
	{
		var key = $"{col},{row}";
		if (_tileOverrides.TryGetValue(key, out var tileDef))
		{
			var value = selector(tileDef);
			if (value.HasValue)
			{
				return value.Value;
			}
		}

		return defaultValue;
	}

	private string GetTileProperty(int col, int row, Func<TileDefinition, string?> selector, string defaultValue)
	{
		var key = $"{col},{row}";
		if (_tileOverrides.TryGetValue(key, out var tileDef))
		{
			var value = selector(tileDef);
			if (!string.IsNullOrEmpty(value))
			{
				return value;
			}
		}

		return defaultValue;
	}

	/// <summary>
	/// Gets the glow filter for a tile's top face. Without a <see cref="TileDefinition.Glow"/> override the
	/// shared filter is used; an override of zero or less removes the glow; any other override gets the tile
	/// its own filter at that intensity (percentage, capped at 100).
	/// </summary>
	private TopFaceFilter GetTopFaceFilter(TileRenderInfo tile)
	{
		if (!_tileOverrides.TryGetValue($"{tile.Column},{tile.Row}", out var tileDef) || tileDef.Glow is not { } glow)
		{
			return new TopFaceFilter($"{Id}-glow", null);
		}

		return glow <= 0
			? new TopFaceFilter(null, null)
			: new TopFaceFilter($"{Id}-glow-{tile.Id}", Math.Min(glow, 100) / 100.0);
	}

	private string? GetTileLogo(int col, int row)
	{
		var id = row * Options.Columns + col;
		return _tileLogos.Count > id ? _tileLogos[id] : null;
	}

	private TileGradientInfo EnsureGradients(string color)
	{
		if (!_tileColorGradients.TryGetValue(color, out var gradInfo))
		{
			var colors = GenerateTileColors(color);
			gradInfo = new TileGradientInfo
			{
				TopGradId = $"{Id}-topGrad-{color.Replace("#", "")}",
				FrontGradId = $"{Id}-frontGrad-{color.Replace("#", "")}",
				Colors = colors
			};
			_tileColorGradients[color] = gradInfo;
		}

		return gradInfo;
	}

	private static TileColors GenerateTileColors(string baseColor)
	{
		var (r, g, b) = ParseHexColor(baseColor);
		return new TileColors
		{
			Dark = ToHex((int)(r * 0.25), (int)(g * 0.25), (int)(b * 0.25)),
			Mid = ToHex((int)(r * 0.5), (int)(g * 0.5), (int)(b * 0.5)),
			Base = baseColor,
			Light = ToHex(Math.Min(255, (int)(r * 1.4)), Math.Min(255, (int)(g * 1.4)), Math.Min(255, (int)(b * 1.4)))
		};
	}

	/// <summary>
	/// Parses a hex colour in the #RRGGBB or short #RGB form (the leading # is optional).
	/// </summary>
	/// <exception cref="ArgumentException">The value is not a hex colour in either form.</exception>
	private static (int r, int g, int b) ParseHexColor(string hex)
	{
		var digits = (hex ?? string.Empty).TrimStart('#');
		if (digits.Length == 3)
		{
			digits = string.Concat(digits.Select(c => new string(c, 2)));
		}

		if (digits.Length != 6 || !digits.All(Uri.IsHexDigit))
		{
			throw new ArgumentException($"'{hex}' is not a valid colour: PDTiles accepts hex colours in the form #RGB or #RRGGBB.", nameof(hex));
		}

		return (
			Convert.ToInt32(digits[..2], 16),
			Convert.ToInt32(digits.Substring(2, 2), 16),
			Convert.ToInt32(digits.Substring(4, 2), 16)
		);
	}

	private static string ToHex(int r, int g, int b) => $"#{Math.Clamp(r, 0, 255):X2}{Math.Clamp(g, 0, 255):X2}{Math.Clamp(b, 0, 255):X2}";

	private static string HexToRgba(string hex, double alpha)
	{
		var (r, g, b) = ParseHexColor(hex);
		return $"rgba({r}, {g}, {b}, {F(alpha)})";
	}

	private static string GetTileName(string? logoPath)
	{
		if (string.IsNullOrEmpty(logoPath))
		{
			return "Unknown";
		}

		return logoPath
			.Replace("tiles/", "")
			.Replace(" Logo.svg", "")
			.Replace("Logo.svg", "")
			.Replace(".svg", "")
			.Trim();
	}
}
