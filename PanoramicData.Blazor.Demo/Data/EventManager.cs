namespace PanoramicData.Blazor.Demo.Data;

public class EventManager
{
	public List<Event> _events = [];

	public event Action<Event>? EventAdded;

	public void Add(Event evt)
	{
		_events.Insert(0, evt);
		EventAdded?.Invoke(evt);
	}

	public Event[] Events => _events.ToArray();
}
