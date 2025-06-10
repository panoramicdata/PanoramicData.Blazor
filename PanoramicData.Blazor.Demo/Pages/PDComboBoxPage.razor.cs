namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDComboBoxPage
{
	private static readonly List<Country> _items =
	[
		new Country { Name = "United States", Code = "US", Continent = "North America" },
		new Country { Name = "Canada", Code = "CA", Continent = "North America" },
		new Country { Name = "Mexico", Code = "MX", Continent = "North America" },
		new Country { Name = "United Kingdom", Code = "UK", Continent = "Europe" },
		new Country { Name = "Germany", Code = "DE", Continent = "Europe" },
		new Country { Name = "France", Code = "FR", Continent = "Europe" },
		new Country { Name = "Japan", Code = "JP", Continent = "Asia" },
		new Country { Name = "Australia", Code = "AU", Continent = "Oceania" }
	];

	private readonly Country _selectedItem = _items[0];

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	private void OnLogEvent(string name)
	{
		EventManager?.Add(new Event(name));
	}

	class Country
	{
		public required string Name { get; set; }
		public required string Code { get; set; }
		public required string Continent { get; set; }

		public override string ToString() => $"{Name} ({Code})";
	}
}
