using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor;

/// <summary>
/// PDTiles: grid layout, alignment, styles and tile geometry.
/// </summary>
public partial class PDTiles
{
	/// <summary>
	/// Gets the SVG viewBox attribute value.
	/// </summary>
	private string ViewBox
	{
		get
		{
			var layout = CalculateLayout();
			return $"0 0 {F(layout.ViewBoxWidth)} {F(layout.ViewBoxHeight)}";
		}
	}

	/// <summary>
	/// Gets the SVG preserveAspectRatio value based on alignment.
	/// </summary>
	private string PreserveAspectRatio
	{
		get
		{
			var xAlign = Options.Alignment switch
			{
				GridAlignment.TopLeft or GridAlignment.MiddleLeft or GridAlignment.BottomLeft => "xMin",
				GridAlignment.TopRight or GridAlignment.MiddleRight or GridAlignment.BottomRight => "xMax",
				_ => "xMid"
			};

			var yAlign = Options.Alignment switch
			{
				GridAlignment.TopLeft or GridAlignment.TopCenter or GridAlignment.TopRight => "YMin",
				GridAlignment.BottomLeft or GridAlignment.BottomCenter or GridAlignment.BottomRight => "YMax",
				_ => "YMid"
			};

			return $"{xAlign}{yAlign} meet";
		}
	}

	/// <summary>
	/// Gets the style for the SVG container div.
	/// </summary>
	private string GetSvgContainerStyle()
	{
		if (ChildContent != null)
		{
			// Position absolutely so the SVG fills the entire container as a background layer.
			// In wrapping mode, grid lines/glow/background show through behind the content text.
			return "position: absolute; top: 0; left: 0; width: 100%; height: 100%;";
		}

		return "width: 100%; height: 100%;";
	}

	/// <summary>
	/// Gets the inline style for the SVG element itself.
	/// In wrapping mode the height is omitted so the SVG auto-sizes from its viewBox aspect ratio.
	/// </summary>
	private string GetSvgElementStyle()
	{
		var style = "width: 100%; height: 100%;";

		if (Options.ShowBackground)
		{
			style += $" background-color: {Options.BackgroundColor};";
		}

		return style;
	}

	/// <summary>
	/// Gets the style for the child content container.
	/// </summary>
	private static string GetChildContentStyle() => "position: absolute; top: 0; left: 0; width: 100%; height: 100%; overflow: auto; z-index: 1; pointer-events: none;";

	/// <summary>
	/// Gets the style for the wrapping child content container (newspaper-style).
	/// </summary>
	private static string GetChildContentWrapStyle() => "position: absolute; top: 0; left: 0; width: 100%; height: 100%; overflow: auto; z-index: 1; pointer-events: none;";

	/// <summary>
	/// Gets the style for the invisible float spacer that forces content to wrap around the grid.
	/// The spacer fills the full container height to ensure content never overlaps the tile area.
	/// </summary>
	private string GetFloatSpacerStyle()
	{
		var floatSide = Options.Alignment is GridAlignment.TopLeft or GridAlignment.MiddleLeft or GridAlignment.BottomLeft
			? "left"
			: "right";

		// Use MaxGridWidthPercent if set, otherwise default to 50%
		var widthPercent = Options.MaxGridWidthPercent ?? 50;

		return $"float: {floatSide}; width: {widthPercent}%; height: 100%;";
	}

	private LayoutInfo CalculateLayout()
	{
		var depthPixels = (int)Math.Round(_tileWidth * (Options.Depth / 100.0));
		// Gap is percentage of tile size to add as spacing between tiles
		// 0% = tiles tessellate (touch), 100% = one tile-width gap between tiles
		var gapFactor = 1 + (Options.Gap / 100.0);
		var isoSpacingX = (_tileWidth / 2.0) * gapFactor;
		var isoSpacingY = (_tileHeight / 2.0) * gapFactor;

		var diagonalSteps = (Options.Columns - 1) + (Options.Rows - 1);
		var gridPixelWidth = diagonalSteps * isoSpacingX + _tileWidth;
		var gridPixelHeight = diagonalSteps * isoSpacingY + _tileHeight + depthPixels;

		// Apply scale (100% = grid edges touch viewbox, <100% = smaller, >100% = larger/cropped)
		var scaleFactor = Options.Scale / 100.0;

		// Apply padding as percentage of the grid size
		var paddingFactor = 1 + (Options.Padding / 100.0 * 2);

		// This is the "constrained area" where the grid should be positioned,
		// with the base viewBox dimensions accounting for scale and padding
		var constrainedWidth = gridPixelWidth / scaleFactor * paddingFactor;
		var constrainedHeight = gridPixelHeight / scaleFactor * paddingFactor;

		return new LayoutInfo
		{
			DepthPixels = depthPixels,
			IsoSpacingX = isoSpacingX,
			IsoSpacingY = isoSpacingY,
			GridWidth = gridPixelWidth,
			GridHeight = gridPixelHeight,
			ConstrainedWidth = constrainedWidth,
			ConstrainedHeight = constrainedHeight,
			ViewBoxWidth = ExpandToMaxPercent(constrainedWidth, GetEffectiveMaxGridWidthPercent()),
			ViewBoxHeight = ExpandToMaxPercent(constrainedHeight, Options.MaxGridHeightPercent)
		};
	}

	/// <summary>
	/// Gets the share of the container width the grid may occupy. When ContentWrapping is active the grid is
	/// auto-constrained to 50% so the tiles stay on one side.
	/// </summary>
	private int? GetEffectiveMaxGridWidthPercent()
		=> Options.MaxGridWidthPercent is null && Options.ContentWrapping && ChildContent != null
			? 50
			: Options.MaxGridWidthPercent;

	/// <summary>
	/// Expands a viewBox dimension so the grid occupies only <paramref name="maxPercent"/> of it (positioned by
	/// alignment). For example, at 50% the viewBox is twice as large. Values outside (0, 100) leave it unchanged.
	/// </summary>
	private static double ExpandToMaxPercent(double constrained, int? maxPercent)
		=> maxPercent is > 0 and < 100
			? constrained * (100.0 / maxPercent.Value)
			: constrained;

	/// <summary>
	/// Gets the anchor point for positioning the grid based on alignment.
	/// </summary>
	private (double X, double Y) GetGridAnchorPoint(LayoutInfo layout)
	{
		// Calculate the offset for the constrained area within the expanded viewBox
		// When MaxGridWidthPercent/MaxGridHeightPercent are set, the constrained area
		// needs to be positioned within the larger viewBox based on alignment
		var constrainedOffsetX = Options.Alignment switch
		{
			GridAlignment.TopLeft or GridAlignment.MiddleLeft or GridAlignment.BottomLeft
				=> 0,
			GridAlignment.TopRight or GridAlignment.MiddleRight or GridAlignment.BottomRight
				=> layout.ViewBoxWidth - layout.ConstrainedWidth,
			_ => (layout.ViewBoxWidth - layout.ConstrainedWidth) / 2
		};

		var constrainedOffsetY = Options.Alignment switch
		{
			GridAlignment.TopLeft or GridAlignment.TopCenter or GridAlignment.TopRight
				=> 0,
			GridAlignment.BottomLeft or GridAlignment.BottomCenter or GridAlignment.BottomRight
				=> layout.ViewBoxHeight - layout.ConstrainedHeight,
			_ => (layout.ViewBoxHeight - layout.ConstrainedHeight) / 2
		};

		// Calculate padding within the constrained area
		var paddingX = (layout.ConstrainedWidth - layout.GridWidth) / 2;
		var paddingY = (layout.ConstrainedHeight - layout.GridHeight) / 2;

		// Position within the constrained area, then offset by the constrained area position
		var x = Options.Alignment switch
		{
			GridAlignment.TopLeft or GridAlignment.MiddleLeft or GridAlignment.BottomLeft
				=> constrainedOffsetX + paddingX + layout.GridWidth / 2,
			GridAlignment.TopRight or GridAlignment.MiddleRight or GridAlignment.BottomRight
				=> constrainedOffsetX + layout.ConstrainedWidth - paddingX - layout.GridWidth / 2,
			_ => constrainedOffsetX + layout.ConstrainedWidth / 2
		};

		var y = Options.Alignment switch
		{
			GridAlignment.TopLeft or GridAlignment.TopCenter or GridAlignment.TopRight
				=> constrainedOffsetY + paddingY + layout.GridHeight / 2 - layout.DepthPixels / 2,
			GridAlignment.BottomLeft or GridAlignment.BottomCenter or GridAlignment.BottomRight
				=> constrainedOffsetY + layout.ConstrainedHeight - paddingY - layout.GridHeight / 2 - layout.DepthPixels / 2,
			_ => constrainedOffsetY + layout.ConstrainedHeight / 2 - layout.DepthPixels / 2
		};

		return (x, y);
	}

	/// <summary>
	/// Gets the render depth for a tile based on connection mode.
	/// </summary>
	private int GetTileDepth(int row, int col)
	{
		return ConnectorOptions.ConnectionMode switch
		{
			ConnectionMode.RowCurves => row,        // Group by row only
			ConnectionMode.ColumnCurves => col,     // Group by column only
			_ => row + col                          // Standard isometric depth
		};
	}

	private List<TileRenderInfo> GetSortedTiles()
	{
		var tiles = new List<TileRenderInfo>();
		var layout = CalculateLayout();
		var tileId = 0;

		// Calculate grid anchor point based on alignment
		var (anchorX, anchorY) = GetGridAnchorPoint(layout);

		var gridCenterCol = (Options.Columns - 1) / 2.0;
		var gridCenterRow = (Options.Rows - 1) / 2.0;

		for (var row = 0; row < Options.Rows; row++)
		{
			for (var col = 0; col < Options.Columns; col++)
			{
				var relCol = col - gridCenterCol;
				var relRow = row - gridCenterRow;
				var x = anchorX + (relCol - relRow) * layout.IsoSpacingX - _tileCenterX;
				var y = anchorY + (relCol + relRow) * layout.IsoSpacingY - _tileCenterY;

				tiles.Add(new TileRenderInfo
				{
					Id = tileId,
					Column = col,
					Row = row,
					X = x,
					Y = y,
					Depth = GetTileDepth(row, col),
					Logo = _tileLogos.Count > tileId ? _tileLogos[tileId] : null,
					Visible = _tileVisible.Count > tileId && _tileVisible[tileId]
				});
				tileId++;
			}
		}

		return [.. tiles.OrderBy(t => t.Depth)];
	}

	private static List<int> GetAllDepths(List<TileRenderInfo> tiles, Dictionary<int, List<ConnectorRenderInfo>> connectorsByDepth)
	{
		var allDepths = new HashSet<int>(tiles.Select(t => t.Depth));
		foreach (var depth in connectorsByDepth.Keys)
		{
			allDepths.Add(depth);
		}

		return [.. allDepths.OrderBy(d => d)];
	}

	private List<LineInfo> GetBackgroundLines()
	{
		var lines = new List<LineInfo>();
		var layout = CalculateLayout();
		var (anchorX, anchorY) = GetGridAnchorPoint(layout);
		// Adjust for grid lines being at base of tiles (add depth)
		var lineAnchorY = anchorY + layout.DepthPixels;
		var gridCenterCol = (Options.Columns - 1) / 2.0;
		var gridCenterRow = (Options.Rows - 1) / 2.0;

		var ext = GetBackgroundLineExtension(layout);

		// Vertical lines (column direction)
		for (var i = -ext; i <= Options.Columns + ext; i++)
		{
			var relCol = i - gridCenterCol;
			var sR = -ext - gridCenterRow;
			var eR = Options.Rows + ext - gridCenterRow;

			lines.Add(new LineInfo
			{
				X1 = anchorX + (relCol - sR) * layout.IsoSpacingX,
				Y1 = lineAnchorY + (relCol + sR) * layout.IsoSpacingY,
				X2 = anchorX + (relCol - eR) * layout.IsoSpacingX,
				Y2 = lineAnchorY + (relCol + eR) * layout.IsoSpacingY
			});
		}

		// Horizontal lines (row direction)
		for (var i = -ext; i <= Options.Rows + ext; i++)
		{
			var relRow = i - gridCenterRow;
			var sC = -ext - gridCenterCol;
			var eC = Options.Columns + ext - gridCenterCol;

			lines.Add(new LineInfo
			{
				X1 = anchorX + (sC - relRow) * layout.IsoSpacingX,
				Y1 = lineAnchorY + (sC + relRow) * layout.IsoSpacingY,
				X2 = anchorX + (eC - relRow) * layout.IsoSpacingX,
				Y2 = lineAnchorY + (eC + relRow) * layout.IsoSpacingY
			});
		}

		return lines;
	}

	/// <summary>
	/// Gets how many tiles beyond the grid the background lines extend, so they reach the container edges.
	/// We need lines to extend beyond the viewBox to ensure full coverage.
	/// </summary>
	private int GetBackgroundLineExtension(LayoutInfo layout)
	{
		var viewBoxExtent = Math.Max(layout.ViewBoxWidth, layout.ViewBoxHeight);
		var gridExtent = Math.Max(layout.GridWidth, layout.GridHeight);
		var extRatio = gridExtent > 0 ? viewBoxExtent / gridExtent : 2;
		return Math.Max(5, (int)Math.Ceiling(extRatio * Math.Max(Options.Columns, Options.Rows)));
	}

	private string GetPerspectiveStyle()
	{
		if (Options.Perspective <= 0)
		{
			return string.Empty;
		}

		var tiltDegrees = Options.Perspective * 0.2;
		return $"transform: perspective(1000px) rotateX({F(tiltDegrees)}deg);";
	}

	private static string GetFrontFacePath(int depthPercent)
	{
		var d = (int)Math.Round(_tileWidth * (depthPercent / 100.0));
		return $"M 88,150 C 82,153 82,156 88,158 L 192,214 C 198,217 202,217 208,214 L 312,158 C 318,156 318,153 312,150 L 316.5,154 L 316.5,{155 + d} Q 316.5,{157 + d} 312,{158 + d} L 208,{214 + d} C 202,{217 + d} 198,{217 + d} 192,{214 + d} L 88,{158 + d} Q 83.5,{157 + d} 83.5,{155 + d} L 83.5,154 Z";
	}

	private static string GetReflectionPath(int depthPercent, int reflectionDepthPercent)
	{
		var d = (int)Math.Round(_tileWidth * (depthPercent / 100.0));
		var reflectionHeight = (int)Math.Round(d * (reflectionDepthPercent / 100.0));
		var bottomY = 155 + d;
		var reflectEndY = bottomY + reflectionHeight;
		return $"M 83.5,{bottomY} L 83.5,{reflectEndY} Q 83.5,{reflectEndY + 2} 88,{reflectEndY + 3} L 192,{214 + d + reflectionHeight + 3} C 198,{217 + d + reflectionHeight + 3} 202,{217 + d + reflectionHeight + 3} 208,{214 + d + reflectionHeight + 3} L 312,{reflectEndY + 3} Q 316.5,{reflectEndY + 2} 316.5,{reflectEndY} L 316.5,{bottomY} L 312,{158 + d} L 208,{214 + d} C 202,{217 + d} 198,{217 + d} 192,{214 + d} L 88,{158 + d} Z";
	}
}
