namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDropDownPage
{
	private PDDropDown _dropdown = null!;

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	private void OnLogEvent(string name)
	{
		EventManager?.Add(new Event(name));
	}
}
