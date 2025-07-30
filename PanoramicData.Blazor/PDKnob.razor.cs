using PanoramicData.Blazor.Enums;
using System.Globalization;

namespace PanoramicData.Blazor;

public enum LabelPosition
{
	Above,
	Below
}

public partial class PDKnob : ComponentBase, IAsyncDisposable
{
	[Parameter] public PDKnobMode Mode { get; set; } = PDKnobMode.Volume;
	[Parameter] public double Value { get; set; } = 0.5;
	[Parameter] public EventCallback<double> ValueChanged { get; set; }
	[Parameter] public double? DefaultValue { get; set; }
	[Parameter] public int MaxVolume { get; set; } = 11;
	[Parameter] public bool IsEnabled { get; set; } = true;
	[Parameter] public int SizePx { get; set; } = 60;
	[Parameter] public string KnobColor { get; set; } = "#eee";
	[Parameter] public string ActiveColor { get; set; } = "#2196f3";
	[Parameter] public double SnapIncrement { get; set; }
	[Parameter] public bool ShowTicks { get; set; } = true;
	[Parameter] public string? Label { get; set; }
	[Parameter] public LabelPosition LabelPosition { get; set; } = LabelPosition.Below;
	[Parameter] public int LabelHeightPx { get; set; } = 20;
	[Parameter] public string? LabelCssClass { get; set; }

	private ElementReference _svgRef;
	private bool _isDragging;
	private double _dragOriginAngle;
	private double _dragOriginValue;
	private double _dragOriginY;
	private DotNetObjectReference<PDKnob>? _dotNetRef;
	private IJSObjectReference? _jsModule;

	[Inject] protected ILogger<PDKnob> Logger { get; set; } = default!;

	// Geometry
	protected double Center => SizePx / 2.0;
	protected double Radius => SizePx * 0.3;
	protected double StartAngle => -160;
	protected double EndAngle => 160;
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
		if (DefaultValue == null)
		{
			DefaultValue = 0.5;
		}
		Value = Math.Clamp(Value, 0, 1);
		if (SnapIncrement > 0)
		{
			Value = Math.Round(Value / SnapIncrement) * SnapIncrement;
		}
	}

	protected async Task OnPointerDown(PointerEventArgs e)
	{
		if (!IsEnabled) return;
		Logger.LogInformation("OnPointerDown: ClientY={ClientY}, Value={Value}", e.ClientY, Value);
		_isDragging = true;
		_dragOriginY = e.ClientY;
		_dragOriginValue = Value;

		_dotNetRef ??= DotNetObjectReference.Create(this);
		_jsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDKnob.razor.js");
		await _jsModule.InvokeVoidAsync("registerKnobEvents", _dotNetRef);
	}

	[JSInvokable]
	public async Task OnPointerMove(double clientY)
	{
		if (!_isDragging) return;
		var deltaY = _dragOriginY - clientY;
		var sensitivity = 150.0;
		var newValue = _dragOriginValue + (deltaY / sensitivity);
		Logger.LogInformation("OnPointerMove: ClientY={ClientY}, deltaY={DeltaY}, newValue={NewValue}", clientY, deltaY, newValue);
		newValue = Math.Clamp(newValue, 0, 1);
		if (SnapIncrement > 0)
		{
			newValue = Math.Round(newValue / SnapIncrement) * SnapIncrement;
		}
		if (newValue != Value)
		{
			Value = newValue;
			await ValueChanged.InvokeAsync(Value);
			StateHasChanged();
		}
	}

	[JSInvokable]
	public void OnPointerUp(double clientY)
	{
		Logger.LogInformation("OnPointerUp: ClientY={ClientY}, Value={Value}", clientY, Value);
		_isDragging = false;
	}

	protected async void OnDoubleClick()
	{
		Logger.LogInformation("OnDoubleClick: Resetting to DefaultValue={DefaultValue}", DefaultValue);
		Value = DefaultValue ?? 0.5;
		if (SnapIncrement > 0)
		{
			Value = Math.Round(Value / SnapIncrement) * SnapIncrement;
		}
		await ValueChanged.InvokeAsync(Value);
		StateHasChanged();
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

	protected string DescribeArc(double cx, double cy, double r, double startAngle, double endAngle)
	{
		var start = PolarToCartesian(cx, cy, r, endAngle);
		var end = PolarToCartesian(cx, cy, r, startAngle);
		var largeArcFlag = endAngle - startAngle <= 180 ? "0" : "1";
		return $"M {start.X.ToString(CultureInfo.InvariantCulture)} {start.Y.ToString(CultureInfo.InvariantCulture)} " +
			   $"A {r.ToString(CultureInfo.InvariantCulture)} {r.ToString(CultureInfo.InvariantCulture)} 0 {largeArcFlag} 0 {end.X.ToString(CultureInfo.InvariantCulture)} {end.Y.ToString(CultureInfo.InvariantCulture)}";
	}

	protected (double X, double Y) PolarToCartesian(double cx, double cy, double r, double angleDeg)
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
                marks.Add(new Mark(x, y, i.ToString()));
            }

            // Ensure the last mark (MaxVolume) is always included if not already
            if (MaxVolume % step != 0 && MaxVolume > 0)
            {
                double frac = (double)MaxVolume / MaxVolume;
                double angle = StartAngle + ArcAngle * frac;
                var (x, y) = PolarToCartesian(Center, Center, Radius + 10, angle);
                if (!marks.Any(m => m.Label == MaxVolume.ToString()))
                {
                    marks.Add(new Mark(x, y, MaxVolume.ToString()));
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

    private int CalculateMarkingStep(int maxVolume)
    {
        if (maxVolume <= 12)
        {
            return 1;
        }

        double targetMarks = 10.0;
        double rawStep = maxVolume / targetMarks;

        if (rawStep <= 1) return 1;
        if (rawStep <= 2) return 2;
        if (rawStep <= 5) return 5;
        if (rawStep <= 10) return 10;
        if (rawStep <= 20) return 20;
        if (rawStep <= 50) return 50;
        if (rawStep <= 100) return 100;
        return (int)Math.Ceiling(rawStep / 10) * 10;
    }

	[Inject] protected IJSRuntime JS { get; set; } = default!;

	public async ValueTask DisposeAsync()
	{
		_dotNetRef?.Dispose();
		if (_jsModule is not null)
			await _jsModule.DisposeAsync();
	}
}