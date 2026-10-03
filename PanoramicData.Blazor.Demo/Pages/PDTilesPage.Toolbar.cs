using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTilesPage
{
	// Static menu items for dropdowns
	private readonly List<MenuItem> _alignmentItems = CreateMenuItems(
		("TopLeft", "TL"), ("TopCenter", "TC"), ("TopRight", "TR"),
		("MiddleLeft", "ML"), ("MiddleCenter", "MC"), ("MiddleRight", "MR"),
		("BottomLeft", "BL"), ("BottomCenter", "BC"), ("BottomRight", "BR"));

	private readonly List<MenuItem> _patternItems = CreateMenuItems(
		("Random", "Random"), ("Solid", "Solid"), ("Bars", "Bars"), ("Chevrons", "Chevrons"));

	private readonly List<MenuItem> _directionItems = CreateMenuItems(
		("All", "All"), ("Orthogonal", "Ortho"), ("Diagonal", "Diag"), ("DiagonalLeftRight", "D-LR"), ("DiagonalFrontBack", "D-FB"));

	private readonly List<MenuItem> _connectionModeItems = CreateMenuItems(
		("StraightLine", "Straight"), ("RowCurves", "Row Curves"), ("ColumnCurves", "Col Curves"));

	private readonly List<MenuItem> _perEdgeItems = CreateMenuItems(
		("", "Rnd"), ("1", "1"), ("2", "2"), ("3", "3"), ("4", "4"));

	private readonly List<MenuItem> _verticalAlignItems = CreateMenuItems(
		("Bottom", "Bot"), ("Center", "Mid"), ("Top", "Top"));

	private readonly List<MenuItem> _animSpeedItems = CreateMenuItems(
		("0", "Off"), ("15", "Slow"), ("35", "Medium"), ("60", "Fast"), ("100", "V.Fast"));

	private readonly List<MenuItem> _maxSizeItems = CreateMenuItems(
		("", "None"), ("25", "25%"), ("33", "33%"), ("50", "50%"), ("66", "66%"), ("75", "75%"), ("100", "100%"));

	// Helper methods for short text display
	private string GetAlignmentShortText() => _options.Alignment switch
	{
		GridAlignment.TopLeft => "TL",
		GridAlignment.TopCenter => "TC",
		GridAlignment.TopRight => "TR",
		GridAlignment.MiddleLeft => "ML",
		GridAlignment.MiddleCenter => "MC",
		GridAlignment.MiddleRight => "MR",
		GridAlignment.BottomLeft => "BL",
		GridAlignment.BottomCenter => "BC",
		GridAlignment.BottomRight => "BR",
		_ => "MC"
	};

	private string GetDirectionShortText() => _connectorOptions.Direction switch
	{
		ConnectorDirection.All => "All",
		ConnectorDirection.Orthogonal => "Ortho",
		ConnectorDirection.Diagonal => "Diag",
		ConnectorDirection.DiagonalLeftRight => "D-LR",
		ConnectorDirection.DiagonalFrontBack => "D-FB",
		_ => "All"
	};

	private string GetConnectionModeShortText() => _connectorOptions.ConnectionMode switch
	{
		ConnectionMode.StraightLine => "Line",
		ConnectionMode.RowCurves => "Row",
		ConnectionMode.ColumnCurves => "Col",
		_ => "Line"
	};

	private string GetVerticalAlignShortText() => _connectorOptions.VerticalAlign switch
	{
		ConnectorVerticalAlign.Bottom => "Bot",
		ConnectorVerticalAlign.Center => "Mid",
		ConnectorVerticalAlign.Top => "Top",
		_ => "Mid"
	};

	private string GetAnimSpeedText() => _connectorOptions.AnimationSpeed switch
	{
		0 => "Off",
		15 => "Slow",
		35 => "Medium",
		60 => "Fast",
		100 => "V.Fast",
		_ => _connectorOptions.AnimationSpeed.ToString()
	};

	// Dropdown selection handlers for PDToolbarDropdown
	private async Task OnColumnsSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Columns = v; await OnGridSizeChangedAsync().ConfigureAwait(true); }
	}

	private async Task OnRowsSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Rows = v; await OnGridSizeChangedAsync().ConfigureAwait(true); }
	}

	private async Task OnGridSizeChangedAsync()
	{
		OnOptionsChanged();
		// Wait for the component to re-render and update its tile visibility
		await Task.Yield();
		StateHasChanged();
		await Task.Yield();
		OnRandomizeConnectors(); // Re-randomize connectors when grid size changes
	}

	private void OnDepthSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Depth = v; OnOptionsChanged(); }
	}

	private void OnGapSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Gap = v; OnOptionsChanged(); }
	}

	private async Task OnPopulationSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Population = v; await OnGridSizeChangedAsync().ConfigureAwait(true); }
	}

	private void OnLogoSizeSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.LogoSize = v; OnOptionsChanged(); }
	}

	private void OnLineOpacitySelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.LineOpacity = v; OnOptionsChanged(); }
	}

	private void OnGlowSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Glow = v; OnOptionsChanged(); }
	}

	private void OnGlowFalloffSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.GlowFalloff = v; OnOptionsChanged(); }
	}

	private void OnPerspectiveSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Perspective = v; OnOptionsChanged(); }
	}

	private void OnReflectionSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Reflection = v; OnOptionsChanged(); }
	}

	private void OnReflectionDepthSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.ReflectionDepth = v; OnOptionsChanged(); }
	}

	private void OnScaleSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Scale = v; OnOptionsChanged(); }
	}

	private void OnPaddingSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Padding = v; OnOptionsChanged(); }
	}

	private void OnAlignmentSelected(string key)
	{
		if (Enum.TryParse<GridAlignment>(key, out var v)) { _options.Alignment = v; OnOptionsChanged(); }
	}

	private void OnPatternSelected(string key)
	{
		if (Enum.TryParse<ConnectorFillPattern>(key, out var v)) { _connectorOptions.FillPattern = v; OnRandomizeConnectors(); }
	}

	private void OnDirectionSelected(string key)
	{
		if (Enum.TryParse<ConnectorDirection>(key, out var v)) { _connectorOptions.Direction = v; OnRandomizeConnectors(); }
	}

	private void OnConnectionModeSelected(string key)
	{
		if (Enum.TryParse<ConnectionMode>(key, out var v)) { _connectorOptions.ConnectionMode = v; OnRandomizeConnectors(); }
	}

	private void OnPerEdgeSelected(string key)
	{
		_connectorOptions.PerEdge = string.IsNullOrEmpty(key) ? null : int.TryParse(key, out var v) ? v : null;
		OnRandomizeConnectors();
	}

	private void OnConnectorPopSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _connectorOptions.Population = v; OnRandomizeConnectors(); }
	}

	private void OnConnectorHeightSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _connectorOptions.Height = v; OnOptionsChanged(); }
	}

	private void OnVerticalAlignSelected(string key)
	{
		if (Enum.TryParse<ConnectorVerticalAlign>(key, out var v)) { _connectorOptions.VerticalAlign = v; OnOptionsChanged(); }
	}

	private void OnConnectorOpacitySelected(string key)
	{
		if (int.TryParse(key, out var v)) { _connectorOptions.Opacity = v; OnOptionsChanged(); }
	}

	private void OnAnimSpeedSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _connectorOptions.AnimationSpeed = v; OnOptionsChanged(); }
	}

	private void OnCurveTensionSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _connectorOptions.CurveTension = v; OnOptionsChanged(); }
	}

	private void OnMaxWidthSelected(string key)
	{
		_options.MaxGridWidthPercent = string.IsNullOrEmpty(key) ? null : int.TryParse(key, out var v) ? v : null;
		OnOptionsChanged();
	}

	private void OnMaxHeightSelected(string key)
	{
		_options.MaxGridHeightPercent = string.IsNullOrEmpty(key) ? null : int.TryParse(key, out var v) ? v : null;
		OnOptionsChanged();
	}

	// Helper to create menu items from values
	private static List<MenuItem> CreateMenuItems(int[] values) =>
		[.. values.Select(v => new MenuItem { Key = v.ToString(), Text = v.ToString() })];

	private static List<MenuItem> CreateMenuItems(params (string Key, string Text)[] items) =>
		[.. items.Select(i => new MenuItem { Key = i.Key, Text = i.Text })];
}
