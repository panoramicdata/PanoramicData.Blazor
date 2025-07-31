namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDKnobPage
{
	private double _volumeValue = 0.5;
	private double _balanceValue = 0.5;
	private double _gainValue = 0.5;
	private double _snapValue = 0.5;
	private int _maxVolume = 11;
	private double _snapIncrement = 0.1;
	private string _knobColor = "#eee";
	private string _activeColor = "#2196f3";
	private string _knobLabel = "Volume";
	private PDLabelPosition _labelPosition = PDLabelPosition.Below;
	private int _labelHeightPx = 20;
	private string _labelCssClass = "";
}
