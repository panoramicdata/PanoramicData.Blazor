using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

/// <summary>
/// Audio knob control that renders an SVG dial with configurable range, labels, and ticks.
/// </summary>
public partial class PDKnob : PDAudioControl
{
	/// <summary>
	/// Gets or sets the mode of the knob, which determines its behavior and appearance.
	/// </summary>
	[Parameter] public PDKnobMode Mode { get; set; } = PDKnobMode.Volume;

	/// <summary>
	/// Gets or sets the maximum display value for the knob.
	/// </summary>
	[Parameter] public int MaxDisplay { get; set; } = 11;

	/// <summary>
	/// Gets or sets the minimum display value for custom range labels.
	/// Only applies when MinLabel and MaxLabel are also set.
	/// </summary>
	[Parameter] public string? MinLabel { get; set; }

	/// <summary>
	/// Gets or sets the maximum display value for custom range labels.
	/// Only applies when MinLabel and MaxLabel are also set.
	/// </summary>
	[Parameter] public string? MaxLabel { get; set; }

	/// <summary>
	/// Gets or sets the size of the knob in pixels.
	/// </summary>
	[Parameter] public int SizePx { get; set; } = 60;

	/// <summary>
	/// Gets or sets the color of the knob's cap.
	/// </summary>
	[Parameter] public string CapColor { get; set; } = "#eee";

	/// <summary>
	/// Gets or sets the color of the active part of the knob.
	/// </summary>
	[Parameter] public string ActiveColor { get; set; } = "#2196f3";

	/// <summary>
	/// Gets or sets whether to show tick marks around the knob.
	/// </summary>
	[Parameter] public bool ShowTicks { get; set; } = true;

	/// <summary>
	/// Gets or sets the start angle of the knob's rotation in degrees.
	/// </summary>
	[Parameter] public double StartAngle { get; set; } = -160;

	/// <summary>
	/// Gets or sets the end angle of the knob's rotation in degrees.
	/// </summary>
	[Parameter] public double EndAngle { get; set; } = 160;

	private ElementReference _svgRef;

	/// <summary>
	/// Gets the center coordinate of the knob.
	/// </summary>
	protected double Center => SizePx / 2.0;
	/// <summary>
	/// Gets the dial radius.
	/// </summary>
	protected double Radius => SizePx * 0.25; // Reduced from 0.3 to 0.25 to leave more room for labels
	/// <summary>
	/// Gets the total sweep angle of the knob.
	/// </summary>
	protected double ArcAngle => EndAngle - StartAngle;

	/// <summary>
	/// Gets the computed text markings around the knob.
	/// </summary>
	protected List<Mark> Markings => GetMarkings();
	/// <summary>
	/// Represents a text mark rendered around the knob.
	/// </summary>
	protected record Mark(double X, double Y, string Label);
	/// <summary>
	/// Represents a tick segment rendered around the knob.
	/// </summary>
	protected record Tick(double X1, double Y1, double X2, double Y2);
	/// <summary>
	/// Gets the computed tick marks around the knob.
	/// </summary>
	protected List<Tick> Ticks => GetTicks();

	/// <summary>
	/// Gets the SVG path for the active arc.
	/// </summary>
	protected string ArcPath => DescribeArc(Center, Center, Radius, StartAngle, StartAngle + ArcAngle * Value);

	/// <summary>
	/// Gets the current indicator angle in degrees.
	/// </summary>
	protected double IndicatorAngle => StartAngle + ArcAngle * Value;
	/// <summary>
	/// Gets the indicator X coordinate.
	/// </summary>
	protected double IndicatorX => Center + Radius * Math.Sin(Deg2Rad(IndicatorAngle));
	/// <summary>
	/// Gets the indicator Y coordinate.
	/// </summary>
	protected double IndicatorY => Center - Radius * Math.Cos(Deg2Rad(IndicatorAngle));

	/// <summary>
	/// Applies mode-specific parameter behavior before rendering.
	/// </summary>
	protected override void OnParametersSet()
	{
		// Only auto-set SnapPoints for Volume mode if custom labels aren't being used
		if (Mode == PDKnobMode.Volume && string.IsNullOrEmpty(MinLabel) && string.IsNullOrEmpty(MaxLabel))
		{
			SnapPoints = MaxDisplay + 1;
		}

		base.OnParametersSet();
	}

	/// <summary>
	/// Converts pointer coordinates to an angle clamped to the knob range.
	/// </summary>
	/// <param name="e">The pointer event arguments.</param>
	/// <returns>The clamped angle in degrees.</returns>
	protected double GetAngleFromPointer(PointerEventArgs e)
	{
		var x = e.OffsetX - Center;
		var y = Center - e.OffsetY;
		var angle = Math.Atan2(x, y) * 180 / Math.PI;
		// Clamp to knob range
		if (angle < StartAngle)
		{
			angle = StartAngle;
		}

		if (angle > EndAngle)
		{
			angle = EndAngle;
		}

		return angle;
	}

	/// <summary>
	/// Converts degrees to radians.
	/// </summary>
	/// <param name="deg">The angle in degrees.</param>
	/// <returns>The angle in radians.</returns>
	protected static double Deg2Rad(double deg) => deg * Math.PI / 180.0;

	/// <summary>
	/// Builds an SVG arc path between two angles.
	/// </summary>
	/// <param name="cx">Center X coordinate.</param>
	/// <param name="cy">Center Y coordinate.</param>
	/// <param name="r">Arc radius.</param>
	/// <param name="startAngle">Start angle in degrees.</param>
	/// <param name="endAngle">End angle in degrees.</param>
	/// <returns>An SVG path string.</returns>
	protected static string DescribeArc(double cx, double cy, double r, double startAngle, double endAngle)
	{
		var (startX, startY) = PolarToCartesian(cx, cy, r, endAngle);
		var (endX, endY) = PolarToCartesian(cx, cy, r, startAngle);
		var largeArcFlag = endAngle - startAngle <= 180 ? "0" : "1";
		return $"M {startX.ToString(CultureInfo.InvariantCulture)} {startY.ToString(CultureInfo.InvariantCulture)} " +
			   $"A {r.ToString(CultureInfo.InvariantCulture)} {r.ToString(CultureInfo.InvariantCulture)} 0 {largeArcFlag} 0 {endX.ToString(CultureInfo.InvariantCulture)} {endY.ToString(CultureInfo.InvariantCulture)}";
	}

	/// <summary>
	/// Converts polar coordinates to cartesian coordinates.
	/// </summary>
	/// <param name="cx">Center X coordinate.</param>
	/// <param name="cy">Center Y coordinate.</param>
	/// <param name="r">Radius.</param>
	/// <param name="angleDeg">Angle in degrees.</param>
	/// <returns>The corresponding cartesian coordinate.</returns>
	protected static (double X, double Y) PolarToCartesian(double cx, double cy, double r, double angleDeg)
	{
		var angleRad = Deg2Rad(angleDeg);
		return (cx + r * Math.Sin(angleRad), cy - r * Math.Cos(angleRad));
	}

	/// <summary>
	/// Computes all label markings for the current knob mode.
	/// </summary>
	/// <returns>A list of label markings.</returns>
	protected List<Mark> GetMarkings()
	{
		var marks = new List<Mark>();
		// Adjusted label distance - reduced from Radius + 10 to Radius + 6 to fit within SVG bounds
		var labelDistance = Radius + 6;

		// Check if custom labels are provided
		if (!string.IsNullOrEmpty(MinLabel) && !string.IsNullOrEmpty(MaxLabel))
		{
			// Custom labels mode (similar to Gain mode but with custom text)
			var (xMin, yMin) = PolarToCartesian(Center, Center, labelDistance, StartAngle);
			var (xMax, yMax) = PolarToCartesian(Center, Center, labelDistance, EndAngle);
			marks.Add(new Mark(xMin, yMin, MinLabel));
			marks.Add(new Mark(xMax, yMax, MaxLabel));
			return marks;
		}

		switch (Mode)
		{
			case PDKnobMode.Volume:
				{
					int step = CalculateMarkingStep(MaxDisplay);
					for (int i = 0; i <= MaxDisplay; i += step)
					{
						double frac = (double)i / MaxDisplay;
						double angle = StartAngle + ArcAngle * frac;
						var (x, y) = PolarToCartesian(Center, Center, labelDistance, angle);
						marks.Add(new Mark(x, y, i.ToString(CultureInfo.InvariantCulture)));
					}

					// Ensure the last mark (MaxVolume) is always included if not already
					if (MaxDisplay % step != 0 && MaxDisplay > 0)
					{
						double frac = (double)MaxDisplay / MaxDisplay;
						double angle = StartAngle + ArcAngle * frac;
						var (x, y) = PolarToCartesian(Center, Center, labelDistance, angle);
						if (!marks.Any(m => m.Label == MaxDisplay.ToString(CultureInfo.InvariantCulture)))
						{
							marks.Add(new Mark(x, y, MaxDisplay.ToString(CultureInfo.InvariantCulture)));
						}
					}

					break;
				}

			case PDKnobMode.Balance:
				{
					// L and R
					var (xL, yL) = PolarToCartesian(Center, Center, labelDistance, StartAngle);
					var (xR, yR) = PolarToCartesian(Center, Center, labelDistance, EndAngle);
					marks.Add(new Mark(xL, yL, "L"));
					marks.Add(new Mark(xR, yR, "R"));
					break;
				}

			case PDKnobMode.Gain:
				{
					// -∞ and +∞
					var (xMin, yMin) = PolarToCartesian(Center, Center, labelDistance, StartAngle);
					var (xMax, yMax) = PolarToCartesian(Center, Center, labelDistance, EndAngle);
					marks.Add(new Mark(xMin, yMin, "-∞"));
					marks.Add(new Mark(xMax, yMax, "+∞"));
					break;
				}
		}

		return marks;
	}

	/// <summary>
	/// Computes tick marks for the current knob mode.
	/// </summary>
	/// <returns>A list of tick marks.</returns>
	protected List<Tick> GetTicks()
	{
		var ticks = new List<Tick>();
		if (ShowTicks && Mode == PDKnobMode.Volume)
		{
			int step = CalculateMarkingStep(MaxDisplay);
			for (int i = 0; i <= MaxDisplay; i += step)
			{
				double frac = (double)i / MaxDisplay;
				double angle = StartAngle + ArcAngle * frac;
				var (x1, y1) = PolarToCartesian(Center, Center, Radius + 2, angle);
				var (x2, y2) = PolarToCartesian(Center, Center, Radius + 6, angle);
				ticks.Add(new Tick(x1, y1, x2, y2));
			}

			// Ensure the last tick (MaxVolume) is always included if not already
			if (MaxDisplay % step != 0 && MaxDisplay > 0)
			{
				double frac = (double)MaxDisplay / MaxDisplay;
				double angle = StartAngle + ArcAngle * frac;
				var (x1, y1) = PolarToCartesian(Center, Center, Radius + 2, angle);
				var (x2, y2) = PolarToCartesian(Center, Center, Radius + 6, angle);
				if (!ticks.Any(t => Math.Abs(t.X1 - x1) < 0.01 && Math.Abs(t.Y1 - y1) < 0.01))
				{
					ticks.Add(new Tick(x1, y1, x2, y2));
				}
			}
		}

		return ticks;
	}

	/// <summary>
	/// Gets the JavaScript module path used by this control.
	/// </summary>
	protected override string JsFileName => "./_content/PanoramicData.Blazor/PDKnob.razor.js";
}