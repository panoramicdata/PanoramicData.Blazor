using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

public partial class PDFader : PDAudioControl
{
	/// <summary>
	/// Gets or sets the width of the fader.
	/// </summary>
	[Parameter] public int Width { get; set; } = 40;

	/// <summary>
	/// Gets or sets the height of the fader.
	/// </summary>
	[Parameter] public int Height { get; set; } = 150;

	/// <summary>
	/// Gets or sets the color of the fader.
	/// </summary>
	[Parameter] public string FaderColor { get; set; } = "#888";

	/// <summary>
	/// Gets or sets the color of the center line.
	/// </summary>
	[Parameter] public string CenterLineColor { get; set; } = "#fff";

	/// <summary>
	/// Gets or sets the color of the markings.
	/// </summary>
	[Parameter] public string MarkingColor { get; set; } = "#000";

	/// <summary>
	/// Gets or sets the minimum value of the fader.
	/// </summary>
	[Parameter] public int MinValue { get; set; } = 0;

	/// <summary>
	/// Gets or sets the maximum value of the fader.
	/// </summary>
	[Parameter] public int MaxValue { get; set; } = 10;

	/// <summary>
	/// Gets or sets the position of the fader labels.
	/// </summary>
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
