

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDToggleSwitchPage
{
	private bool _toggle1 = false;
	private PDToggleSwitchOptions _options1 = new()
	{
		CssClass = "me-1",
		OffText = "OFF",
		OnText = "ON",
		Rounded = true
	};
	private PDToggleSwitchOptions _options2 = new()
	{
		CssClass = "mb-1 d-block",
		OffText = "OFF",
		OnText = "ON",
		Rounded = true,
		Width = 100
	};
}
