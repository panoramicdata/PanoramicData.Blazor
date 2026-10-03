using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDKnobPage
{
	protected double VolumeValue { get; set; } = 0.5;
	protected double BalanceValue { get; set; } = 0.5;
	protected double GainValue { get; set; } = 0.5;
	protected double SnapValue { get; set; } = 0.5;
	protected int MaxVolume { get; set; } = 11;
	private readonly double _snapIncrement = 0.1;
	protected string KnobColor { get; set; } = "#eee";
	protected string ActiveColor { get; set; } = "#2196f3";
	protected string KnobLabel { get; set; } = "Volume";
	protected PDLabelPosition LabelPosition { get; set; } = PDLabelPosition.Below;
	private readonly int _labelHeightPx = 20;
	protected string LabelCssClass { get; set; } = "";
}
