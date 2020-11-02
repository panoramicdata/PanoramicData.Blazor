namespace PanoramicData.Blazor.Demo.Data
{
	public class EventArgument
	{
		public EventArgument()
		{
		}

		public EventArgument(string name, object? value)
		{
			Name = name;
			Value = value?.ToString() ?? ("(null)");
		}

		public string Name { get; set; }
		public string Value { get; set; }
	}
}
