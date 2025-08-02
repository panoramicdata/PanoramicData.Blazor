using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

public partial class PDFader : PDAudioControl
{
	[Parameter] public int Width { get; set; } = 40;
	[Parameter] public int Height { get; set; } = 150;
	[Parameter] public string FaderColor { get; set; } = "#888";
	[Parameter] public string CenterLineColor { get; set; } = "#fff";
	[Parameter] public string MarkingColor { get; set; } = "#000";
	[Parameter] public int MinValue { get; set; } = 0;
	[Parameter] public int MaxValue { get; set; } = 10;
	[Parameter] public PDFaderLabelPosition FaderLabelPosition { get; set; } = PDFaderLabelPosition.Both;

	protected override string JsFileName => "./_content/PanoramicData.Blazor/PDFader.razor.js";

	// Geometry

	protected static double GripHeight => 20;
	protected double GripWidth => Width * 0.75;
	protected static double GripRx => 2;
	protected double GripX => (Width - GripWidth) / 2;
	protected double GripY => (Height - GripHeight) * (1 - Value);
	protected double GripX2 => GripX + GripWidth;
	protected double CenterLineY => GripY + (GripHeight / 2.0);
	protected static string HighlightColor => "#aaa";
	protected static string ShadowColor => "#666";

	// Markings
	protected List<Mark> Markings => GetMarkings();
	protected record Mark(double Y, string Label);

	protected override void OnParametersSet()
	{
		// Only set SnapPoints if not already set by the user (i.e., SnapPoints is null)
		SnapPoints ??= MaxValue - MinValue + 1;

		base.OnParametersSet();
	}

	protected List<Mark> GetMarkings()
	{
		var marks = new List<Mark>();
		var step = CalculateMarkingStep(MaxValue - MinValue);
		for (var i = MinValue; i <= MaxValue; i += step)
		{
			var y = (Height - GripHeight) * (1 - (double)(i - MinValue) / (MaxValue - MinValue)) + (GripHeight / 2.0);
			marks.Add(new Mark(y, i.ToString(CultureInfo.InvariantCulture)));
		}

		return marks;
	}
}
