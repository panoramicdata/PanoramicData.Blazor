namespace PanoramicData.Blazor.Demo.Data;

public class EventManager
{
	private readonly List<Event> _events = [];

	public event Action<Event>? EventAdded;

	public void Add(Event evt)
	{
		_events.Insert(0, evt);
		if (EventAdded != null)
		{
			EventAdded(evt);
		}
	}

	public Event[] Events => [.. _events];
}
