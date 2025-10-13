namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDAudioChannelPage
{
	private double _faderValue = 0.7;
	private double _gainValue = 0.6;
	private double _panValue = 0.5;
	private double _muteValue = 0;
	private bool _muteChecked = false;

	private void OnMuteChanged()
	{
		_muteValue = _muteChecked ? 1 : 0;
	}
}
