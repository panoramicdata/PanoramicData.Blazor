using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

public partial class PDKnob : PDAudioControl
{
	[Parameter] public PDKnobMode Mode { get; set; } = PDKnobMode.Volume;

	[Parameter] public int MaxVolume { get; set; } = 11;

	[Parameter] public int SizePx { get; set; } = 60;

	[Parameter] public string KnobColor { get; set; } = "#eee";

	[Parameter] public string ActiveColor { get; set; } = "#2196f3";

	[Parameter] public bool ShowTicks { get; set; } = true;

	[Parameter] public double StartAngle { get; set; } = -160;

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
			SnapPoints = MaxVolume + 1;
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
		if (Mode == PDKnobMode.Volume)
		{
			int step = CalculateMarkingStep(MaxVolume);
			for (int i = 0; i <= MaxVolume; i += step)
			{
				double frac = (double)i / MaxVolume;
				double angle = StartAngle + ArcAngle * frac;
				var (x, y) = PolarToCartesian(Center, Center, Radius + 10, angle);
				marks.Add(new Mark(x, y, i.ToString(CultureInfo.InvariantCulture)));
			}

			// Ensure the last mark (MaxVolume) is always included if not already
			if (MaxVolume % step != 0 && MaxVolume > 0)
			{
				double frac = (double)MaxVolume / MaxVolume;
				double angle = StartAngle + ArcAngle * frac;
				var (x, y) = PolarToCartesian(Center, Center, Radius + 10, angle);
				if (!marks.Any(m => m.Label == MaxVolume.ToString(CultureInfo.InvariantCulture)))
				{
					marks.Add(new Mark(x, y, MaxVolume.ToString(CultureInfo.InvariantCulture)));
				}
			}
		}
		else if (Mode == PDKnobMode.Balance)
		{
			// L and R
			var (xL, yL) = PolarToCartesian(Center, Center, Radius + 10, StartAngle);
			var (xR, yR) = PolarToCartesian(Center, Center, Radius + 10, EndAngle);
			marks.Add(new Mark(xL, yL, "L"));
			marks.Add(new Mark(xR, yR, "R"));
		}
		else if (Mode == PDKnobMode.Gain)
		{
			// -∞ and +∞
			var (xMin, yMin) = PolarToCartesian(Center, Center, Radius + 10, StartAngle);
			var (xMax, yMax) = PolarToCartesian(Center, Center, Radius + 10, EndAngle);
			marks.Add(new Mark(xMin, yMin, "-∞"));
			marks.Add(new Mark(xMax, yMax, "+∞"));
		}

		return marks;
	}

	protected List<Tick> GetTicks()
	{
		var ticks = new List<Tick>();
		if (ShowTicks && Mode == PDKnobMode.Volume)
		{
			int step = CalculateMarkingStep(MaxVolume);
			for (int i = 0; i <= MaxVolume; i += step)
			{
				double frac = (double)i / MaxVolume;
				double angle = StartAngle + ArcAngle * frac;
				var (x1, y1) = PolarToCartesian(Center, Center, Radius + 2, angle);
				var (x2, y2) = PolarToCartesian(Center, Center, Radius + 6, angle);
				ticks.Add(new Tick(x1, y1, x2, y2));
			}

			// Ensure the last tick (MaxVolume) is always included if not already
			if (MaxVolume % step != 0 && MaxVolume > 0)
			{
				double frac = (double)MaxVolume / MaxVolume;
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