using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTilesPage
{
	private void ParseQueryParameters()
	{
		var uri = new Uri(NavigationManager.Uri);
		var query = QueryHelpers.ParseQuery(uri.Query);

		// Grid options
		Apply<int>(query, int.TryParse, "cols", v => _options.Columns = v);
		Apply<int>(query, int.TryParse, "rows", v => _options.Rows = v);
		Apply<int>(query, int.TryParse, "depth", v => _options.Depth = v);
		Apply<int>(query, int.TryParse, "gap", v => _options.Gap = v);
		Apply<int>(query, int.TryParse, "pop", v => _options.Population = v);
		Apply<int>(query, int.TryParse, "logoSize", v => _options.LogoSize = v);
		Apply<int>(query, int.TryParse, "logoRot", v => _options.LogoRotation = v);
		Apply<string>(query, TryParseHexColor, "tile", v => _options.TileColor = v);
		Apply<string>(query, TryParseHexColor, "bg", v => _options.BackgroundColor = v);
		Apply<string>(query, TryParseHexColor, "lineColor", v => _options.LineColor = v);
		Apply<int>(query, int.TryParse, "lineOp", v => _options.LineOpacity = v);
		Apply<int>(query, int.TryParse, "glow", v => _options.Glow = v);
		Apply<int>(query, int.TryParse, "glowFO", v => _options.GlowFalloff = v);
		Apply<int>(query, int.TryParse, "persp", v => _options.Perspective = v);
		Apply<int>(query, int.TryParse, "refl", v => _options.Reflection = v);
		Apply<int>(query, int.TryParse, "reflD", v => _options.ReflectionDepth = v);
		Apply<int>(query, int.TryParse, "scale", v => _options.Scale = v);
		Apply<int>(query, int.TryParse, "pad", v => _options.Padding = v);
		Apply<GridAlignment>(query, Enum.TryParse, "align", v => _options.Alignment = v);
		Apply<int?>(query, TryParseNullableInt, "maxW", v => _options.MaxGridWidthPercent = v);
		Apply<int?>(query, TryParseNullableInt, "maxH", v => _options.MaxGridHeightPercent = v);
		Apply<bool>(query, bool.TryParse, "content", v => ShowChildContent = v);
		Apply<bool>(query, bool.TryParse, "wrap", v => _options.ContentWrapping = v);

		// Connector options
		Apply<ConnectorFillPattern>(query, Enum.TryParse, "cPat", v => _connectorOptions.FillPattern = v);
		Apply<ConnectorDirection>(query, Enum.TryParse, "cDir", v => _connectorOptions.Direction = v);
		Apply<int?>(query, TryParseNullableInt, "cN", v => _connectorOptions.PerEdge = v);
		Apply<int>(query, int.TryParse, "cPop", v => _connectorOptions.Population = v);
		Apply<int>(query, int.TryParse, "cH", v => _connectorOptions.Height = v);
		Apply<ConnectorVerticalAlign>(query, Enum.TryParse, "cV", v => _connectorOptions.VerticalAlign = v);
		Apply<int>(query, int.TryParse, "cOp", v => _connectorOptions.Opacity = v);
		Apply<int>(query, int.TryParse, "cAnim", v => _connectorOptions.AnimationSpeed = v);
	}

	private delegate bool TryParser<T>(string text, out T result);

	private static void Apply<T>(Dictionary<string, StringValues> query, TryParser<T> tryParse, string key, Action<T> setter)
	{
		if (query.TryGetValue(key, out var value) && tryParse(value.ToString(), out var parsed))
		{
			setter(parsed);
		}
	}

	private static bool TryParseHexColor(string text, out string result)
	{
		result = "#" + text;
		return true;
	}

	// An empty or non-numeric value clears the setting
	private static bool TryParseNullableInt(string text, out int? result)
	{
		result = int.TryParse(text, out var parsed) ? parsed : null;
		return true;
	}

	private void UpdateUrl()
	{
		if (!_isInitialized)
		{
			return;
		}

		// Always include all parameters so users have a complete starting point
		var queryParams = GetGridQueryParameters();
		foreach (var (key, value) in GetConnectorQueryParameters())
		{
			queryParams[key] = value;
		}

		var newUrl = QueryHelpers.AddQueryString(NavigationManager.Uri.Split('?')[0], queryParams!);
		NavigationManager.NavigateTo(newUrl, replace: true);
	}

	private static string FormatNullable(int? value) => $"{value}";

	private static string FormatBool(bool value) => value.ToString().ToLowerInvariant();

	private Dictionary<string, string?> GetGridQueryParameters()
		=> new()
		{
			// Grid options
			["cols"] = _options.Columns.ToString(),
			["rows"] = _options.Rows.ToString(),
			["depth"] = _options.Depth.ToString(),
			["gap"] = _options.Gap.ToString(),
			["pop"] = _options.Population.ToString(),
			["logoSize"] = _options.LogoSize.ToString(),
			["logoRot"] = _options.LogoRotation.ToString(),
			["tile"] = _options.TileColor.TrimStart('#'),
			["bg"] = _options.BackgroundColor.TrimStart('#'),
			["lineColor"] = _options.LineColor.TrimStart('#'),
			["lineOp"] = _options.LineOpacity.ToString(),
			["glow"] = _options.Glow.ToString(),
			["glowFO"] = _options.GlowFalloff.ToString(),
			["persp"] = _options.Perspective.ToString(),
			["refl"] = _options.Reflection.ToString(),
			["reflD"] = _options.ReflectionDepth.ToString(),
			["scale"] = _options.Scale.ToString(),
			["pad"] = _options.Padding.ToString(),
			["align"] = _options.Alignment.ToString(),
			["maxW"] = FormatNullable(_options.MaxGridWidthPercent),
			["maxH"] = FormatNullable(_options.MaxGridHeightPercent),
			["content"] = FormatBool(ShowChildContent),
			["wrap"] = FormatBool(_options.ContentWrapping)
		};

	private Dictionary<string, string?> GetConnectorQueryParameters()
		=> new()
		{
			["cPat"] = _connectorOptions.FillPattern.ToString(),
			["cDir"] = _connectorOptions.Direction.ToString(),
			["cN"] = FormatNullable(_connectorOptions.PerEdge),
			["cPop"] = _connectorOptions.Population.ToString(),
			["cH"] = _connectorOptions.Height.ToString(),
			["cV"] = _connectorOptions.VerticalAlign.ToString(),
			["cOp"] = _connectorOptions.Opacity.ToString(),
			["cAnim"] = _connectorOptions.AnimationSpeed.ToString()
		};
}
