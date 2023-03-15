namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDToggleSwitchPage
{
	private bool _toggle1 = false;

	private readonly PDToggleSwitchOptions _options1 = new()
	{
		OffText = "OFF",
		OnText = "ON",
		Rounded = true
	};

	private readonly PDToggleSwitchOptions _options2 = new()
	{
		OffText = "OFF",
		OnText = "ON",
		Rounded = true,
		Width = 110
	};
}
