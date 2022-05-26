namespace PanoramicData.Blazor.Demo.Data;

public class Event
{
	public Event()
	{
	}

	public Event(string name, params EventArgument[] args)
	{
		Name = name;
		Arguments.AddRange(args);
	}

	public string Name { get; set; } = string.Empty;

	public List<EventArgument> Arguments { get; } = new List<EventArgument>();
}
