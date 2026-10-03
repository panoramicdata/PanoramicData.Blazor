namespace PanoramicData.Blazor.Demo.Shared;

public partial class EventView : IDisposable
{
	[Parameter] public RenderFragment? ChildContent { get; set; }

	private readonly Action<Event> _onEventAdded;

	public EventView()
	{
		// the new event is read from the EventManager when re-rendering
		_onEventAdded = _ => StateHasChanged();
	}

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	protected override void OnInitialized()
	{
		if (EventManager != null)
		{
			EventManager.EventAdded += _onEventAdded;
		}
	}

	public void Dispose()
	{
		if (EventManager != null)
		{
			EventManager.EventAdded -= _onEventAdded;
		}

		GC.SuppressFinalize(this);
	}
}
