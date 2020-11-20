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

		public string Name { get; set; } = string.Empty;
		public string Value { get; set; } = string.Empty;
	}
}
