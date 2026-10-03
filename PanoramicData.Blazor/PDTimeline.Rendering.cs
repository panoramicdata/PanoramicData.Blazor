namespace PanoramicData.Blazor;

/// <summary>
/// Helpers used by the timeline markup to draw the plot.
/// </summary>
public partial class PDTimeline
{
	private double GetMaxValue(DataPoint[] points)
	{
		double max = 0;

		DataPoint[] tempArray = [.. points.Where(x => x != null && x.SeriesValues.Length > 0)];
		if (tempArray.Length != 0)
		{
			max = tempArray.Max(x => x.SeriesValues.Sum(y => YValueTransform(y)));
		}

		return max;
	}

	private DataPoint[] GetViewPortDataPoints()
	{
		var points = new DataPoint[_viewportColumns];
		for (var i = 0; i < _viewportColumns; i++)
		{
			var key = _columnOffset + i;
			if (_dataPoints.TryGetValue(key, out DataPoint? value))
			{
				points[i] = value;
			}
			else if (!_loading)
			{
				points[i] = new DataPoint
				{
					PeriodIndex = key,
					StartTime = Scale.AddPeriods(RoundedMinDateTime, key)
				};
			}
		}

		return [.. points];
	}

	private bool IsPointEnabled(DataPoint point)
	{
		if (point is null
			|| !IsEnabled
			|| (DisableAfter != DateTime.MinValue && Scale.PeriodEnd(point.StartTime) > DisableAfter)
			|| (DisableBefore != DateTime.MinValue && point.StartTime < DisableBefore))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Utility methods used by timeline rendering helpers.
	/// </summary>
	public static class Utilities
	{
		/// <summary>
		/// Builds an SVG arc path between two angles.
		/// </summary>
		/// <param name="x">Center X.</param>
		/// <param name="y">Center Y.</param>
		/// <param name="radius">Arc radius.</param>
		/// <param name="startAngle">Start angle in degrees.</param>
		/// <param name="endAngle">End angle in degrees.</param>
		/// <returns>SVG path data.</returns>
		public static string DescribeArc(double x, double y, double radius, double startAngle, double endAngle)
		{
			var sp = PolarToCartesian(x, y, radius, endAngle);
			var ep = PolarToCartesian(x, y, radius, startAngle);
			var arcSweep = endAngle - startAngle <= 180 ? "0" : "1";
			return string.Create(
				CultureInfo.InvariantCulture,
				$"M {sp.x:0.00} {sp.y:0.00} A {radius} {radius} 0 {arcSweep} 0 {ep.x:0.00} {ep.y:0.00}");
		}

		/// <summary>
		/// Converts polar coordinates to cartesian coordinates.
		/// </summary>
		/// <param name="centerX">Center X.</param>
		/// <param name="centerY">Center Y.</param>
		/// <param name="radius">Radius.</param>
		/// <param name="angleInDegrees">Angle in degrees.</param>
		/// <returns>Cartesian coordinates.</returns>
		public static (double x, double y) PolarToCartesian(double centerX, double centerY, double radius, double angleInDegrees)
		{
			var angleInRadians = angleInDegrees * Math.PI / 180.0;
			var x = centerX + radius * Math.Cos(angleInRadians);
			var y = centerY + radius * Math.Sin(angleInRadians);
			return (x, y);
		}

		/// <summary>
		/// Generates an SVG arrow path.
		/// </summary>
		/// <param name="x">X origin.</param>
		/// <param name="boundsHeight">Bounding height.</param>
		/// <param name="boundsWidth">Bounding width.</param>
		/// <param name="padding">Inner padding.</param>
		/// <param name="faceLeft">True to draw left-facing arrow; otherwise right-facing.</param>
		/// <returns>SVG path data.</returns>
		public static string ArrowPath(double x, double boundsHeight, double boundsWidth, double padding, bool faceLeft)
		{
			var cy = boundsHeight / 2;
			var w = boundsWidth - (2 * padding);
			var sb = new StringBuilder();
			if (faceLeft)
			{
				sb.Append("M ").Append(x + padding).Append(' ').Append(Math.Round(cy));
				sb.Append("l ").Append(w).Append(" -").Append(w);
				sb.Append("l 0 ").Append(2 * w);
			}
			else
			{
				sb.Append("M ").Append(x + (boundsWidth - padding)).Append(' ').Append(Math.Round(cy));
				sb.Append("l -").Append(w).Append(" -").Append(w);
				sb.Append("l 0 ").Append(2 * w);
			}

			sb.Append('Z');
			return sb.ToString();
		}
	}

	/// <summary>
	/// Label positioning metadata used when rendering timeline text.
	/// </summary>
	public class TextInfo
	{
		/// <summary>
		/// Horizontal text offset.
		/// </summary>
		public int OffsetX { get; set; } = 3;
		/// <summary>
		/// Vertical text offset.
		/// </summary>
		public int OffsetY { get; set; } = 14;
		/// <summary>
		/// Number of items to skip before rendering next label.
		/// </summary>
		public int Skip { get; set; }
		/// <summary>
		/// Text value.
		/// </summary>
		public string Text { get; set; } = string.Empty;
	}
}
