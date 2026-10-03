using Microsoft.AspNetCore.WebUtilities;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTilesPage
{
	private void ParseQueryParameters()
	{
		var uri = new Uri(NavigationManager.Uri);
		var query = QueryHelpers.ParseQuery(uri.Query);

		// Grid options
		TryParseInt(query, "cols", v => _options.Columns = v);
		TryParseInt(query, "rows", v => _options.Rows = v);
		TryParseInt(query, "depth", v => _options.Depth = v);
		TryParseInt(query, "gap", v => _options.Gap = v);
		TryParseInt(query, "pop", v => _options.Population = v);
		TryParseInt(query, "logoSize", v => _options.LogoSize = v);
		TryParseInt(query, "logoRot", v => _options.LogoRotation = v);
		TryParseHexColor(query, "tile", v => _options.TileColor = v);
		TryParseHexColor(query, "bg", v => _options.BackgroundColor = v);
		TryParseHexColor(query, "lineColor", v => _options.LineColor = v);
		TryParseInt(query, "lineOp", v => _options.LineOpacity = v);
		TryParseInt(query, "glow", v => _options.Glow = v);
		TryParseInt(query, "glowFO", v => _options.GlowFalloff = v);
		TryParseInt(query, "persp", v => _options.Perspective = v);
		TryParseInt(query, "refl", v => _options.Reflection = v);
		TryParseInt(query, "reflD", v => _options.ReflectionDepth = v);
		TryParseInt(query, "scale", v => _options.Scale = v);
		TryParseInt(query, "pad", v => _options.Padding = v);
		TryParseEnum<GridAlignment>(query, "align", v => _options.Alignment = v);
		TryParseNullableInt(query, "maxW", v => _options.MaxGridWidthPercent = v);
		TryParseNullableInt(query, "maxH", v => _options.MaxGridHeightPercent = v);
		TryParseBool(query, "content", v => ShowChildContent = v);
		TryParseBool(query, "wrap", v => _options.ContentWrapping = v);

		// Connector options
		TryParseEnum<ConnectorFillPattern>(query, "cPat", v => _connectorOptions.FillPattern = v);
		TryParseEnum<ConnectorDirection>(query, "cDir", v => _connectorOptions.Direction = v);
		TryParseNullableInt(query, "cN", v => _connectorOptions.PerEdge = v);
		TryParseInt(query, "cPop", v => _connectorOptions.Population = v);
		TryParseInt(query, "cH", v => _connectorOptions.Height = v);
		TryParseEnum<ConnectorVerticalAlign>(query, "cV", v => _connectorOptions.VerticalAlign = v);
		TryParseInt(query, "cOp", v => _connectorOptions.Opacity = v);
		TryParseInt(query, "cAnim", v => _connectorOptions.AnimationSpeed = v);
	}

	private static void TryParseInt(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<int> setter)
	{
		if (query.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
		{
			setter(parsed);
		}
	}

	private static void TryParseBool(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<bool> setter)
	{
		if (query.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed))
		{
			setter(parsed);
		}
	}

	private static void TryParseEnum<TEnum>(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<TEnum> setter) where TEnum : struct, Enum
	{
		if (query.TryGetValue(key, out var value) && Enum.TryParse<TEnum>(value, out var parsed))
		{
			setter(parsed);
		}
	}

	private static void TryParseHexColor(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<string> setter)
	{
		if (query.TryGetValue(key, out var value))
		{
			setter("#" + value.ToString());
		}
	}

	private static void TryParseNullableInt(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<int?> setter)
	{
		if (query.TryGetValue(key, out var value))
		{
			setter(string.IsNullOrEmpty(value) ? null : int.TryParse(value, out var parsed) ? parsed : null);
		}
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

	private static string FormatNullable(int? value) => value?.ToString() ?? "";

	private static string FormatBool(bool value) => value ? "true" : "false";

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
