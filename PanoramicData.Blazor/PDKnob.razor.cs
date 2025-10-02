using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

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

	// Geometry
	protected double Center => SizePx / 2.0;
	protected double Radius => SizePx * 0.3;
	protected double ArcAngle => EndAngle - StartAngle;

	// Markings
	protected List<Mark> Markings => GetMarkings();
	protected record Mark(double X, double Y, string Label);
	protected record Tick(double X1, double Y1, double X2, double Y2);
	protected List<Tick> Ticks => GetTicks();

	// SVG Arc Path
	protected string ArcPath => DescribeArc(Center, Center, Radius, StartAngle, StartAngle + ArcAngle * Value);

	// Indicator
	protected double IndicatorAngle => StartAngle + ArcAngle * Value;
	protected double IndicatorX => Center + Radius * Math.Sin(Deg2Rad(IndicatorAngle));
	protected double IndicatorY => Center - Radius * Math.Cos(Deg2Rad(IndicatorAngle));

	protected override void OnParametersSet()
	{
		if (Mode == PDKnobMode.Volume)
		{
			SnapPoints = MaxDisplay + 1;
		}

		base.OnParametersSet();
	}

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

	protected static double Deg2Rad(double deg) => deg * Math.PI / 180.0;

	protected static string DescribeArc(double cx, double cy, double r, double startAngle, double endAngle)
	{
		var (startX, startY) = PolarToCartesian(cx, cy, r, endAngle);
		var (endX, endY) = PolarToCartesian(cx, cy, r, startAngle);
		var largeArcFlag = endAngle - startAngle <= 180 ? "0" : "1";
		return $"M {startX.ToString(CultureInfo.InvariantCulture)} {startY.ToString(CultureInfo.InvariantCulture)} " +
			   $"A {r.ToString(CultureInfo.InvariantCulture)} {r.ToString(CultureInfo.InvariantCulture)} 0 {largeArcFlag} 0 {endX.ToString(CultureInfo.InvariantCulture)} {endY.ToString(CultureInfo.InvariantCulture)}";
	}

	protected static (double X, double Y) PolarToCartesian(double cx, double cy, double r, double angleDeg)
	{
		var angleRad = Deg2Rad(angleDeg);
		return (cx + r * Math.Sin(angleRad), cy - r * Math.Cos(angleRad));
	}

	protected List<Mark> GetMarkings()
	{
		var marks = new List<Mark>();

		switch (Mode)
		{
			case PDKnobMode.Volume:
				{
					int step = CalculateMarkingStep(MaxDisplay);
					for (int i = 0; i <= MaxDisplay; i += step)
					{
						double frac = (double)i / MaxDisplay;
						double angle = StartAngle + ArcAngle * frac;
						var (x, y) = PolarToCartesian(Center, Center, Radius + 10, angle);
						marks.Add(new Mark(x, y, i.ToString(CultureInfo.InvariantCulture)));
					}

					// Ensure the last mark (MaxVolume) is always included if not already
					if (MaxDisplay % step != 0 && MaxDisplay > 0)
					{
						double frac = (double)MaxDisplay / MaxDisplay;
						double angle = StartAngle + ArcAngle * frac;
						var (x, y) = PolarToCartesian(Center, Center, Radius + 10, angle);
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
					var (xL, yL) = PolarToCartesian(Center, Center, Radius + 10, StartAngle);
					var (xR, yR) = PolarToCartesian(Center, Center, Radius + 10, EndAngle);
					marks.Add(new Mark(xL, yL, "L"));
					marks.Add(new Mark(xR, yR, "R"));
					break;
				}

			case PDKnobMode.Gain:
				{
					// -∞ and +∞
					var (xMin, yMin) = PolarToCartesian(Center, Center, Radius + 10, StartAngle);
					var (xMax, yMax) = PolarToCartesian(Center, Center, Radius + 10, EndAngle);
					marks.Add(new Mark(xMin, yMin, "-∞"));
					marks.Add(new Mark(xMax, yMax, "+∞"));
					break;
				}
		}

		return marks;
	}

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

	protected override string JsFileName => "./_content/PanoramicData.Blazor/PDKnob.razor.js";
}