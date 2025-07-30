namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDKnobPage
{
	private double volumeValue = 0.5;
	private double balanceValue = 0.5;
	private double gainValue = 0.5;
	private double snapValue = 0.5;
	private int maxVolume = 11;
	private double snapIncrement = 0.1;
	private string knobColor = "#eee";
	private string activeColor = "#2196f3";
	private string knobLabel = "Volume";
	private LabelPosition labelPosition = LabelPosition.Below;
	private int labelHeightPx = 20;
	private string labelCssClass = "";
}
