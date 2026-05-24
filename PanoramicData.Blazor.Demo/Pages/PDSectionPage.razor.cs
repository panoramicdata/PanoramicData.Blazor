namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDSectionPage
{
    [CascadingParameter] protected EventManager? EventManager { get; set; }

    private bool _collapsed1;
    private bool _collapsed2;
    private string _actionMessage = string.Empty;
    private PDSection? _refSection;

	private void OnToggled(bool isCollapsed) => EventManager?.Add(new Event("PDSection.Toggled", new EventArgument("IsCollapsed", isCollapsed)));

	private void OnActionClicked()
    {
        _actionMessage = $"Action button clicked at {DateTime.Now:HH:mm:ss}";
        EventManager?.Add(new Event("PDSection.HeaderAction", new EventArgument("Message", _actionMessage)));
    }
}
