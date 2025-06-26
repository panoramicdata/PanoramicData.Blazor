namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDComboBoxPage
{
	private static readonly List<Country> _items =
	[
		new Country { Name = "Afghanistan", Code = "AF", Continent = "Asia" },
		new Country { Name = "Albania", Code = "AL", Continent = "Europe" },
		new Country { Name = "Algeria", Code = "DZ", Continent = "Africa" },
		new Country { Name = "Andorra", Code = "AD", Continent = "Europe" },
		new Country { Name = "Angola", Code = "AO", Continent = "Africa" },
		new Country { Name = "Antigua and Barbuda", Code = "AG", Continent = "North America" },
		new Country { Name = "Argentina", Code = "AR", Continent = "South America" },
		new Country { Name = "Armenia", Code = "AM", Continent = "Asia" },
		new Country { Name = "Australia", Code = "AU", Continent = "Oceania" },
		new Country { Name = "Austria", Code = "AT", Continent = "Europe" },
		new Country { Name = "Azerbaijan", Code = "AZ", Continent = "Asia" },
		new Country { Name = "Bahamas", Code = "BS", Continent = "North America" },
		new Country { Name = "Bahrain", Code = "BH", Continent = "Asia" },
		new Country { Name = "Bangladesh", Code = "BD", Continent = "Asia" },
		new Country { Name = "Barbados", Code = "BB", Continent = "North America" },
		new Country { Name = "Belarus", Code = "BY", Continent = "Europe" },
		new Country { Name = "Belgium", Code = "BE", Continent = "Europe" },
		new Country { Name = "Belize", Code = "BZ", Continent = "North America" },
		new Country { Name = "Benin", Code = "BJ", Continent = "Africa" },
		new Country { Name = "Bhutan", Code = "BT", Continent = "Asia" },
		new Country { Name = "Bolivia", Code = "BO", Continent = "South America" },
		new Country { Name = "Bosnia and Herzegovina", Code = "BA", Continent = "Europe" },
		new Country { Name = "Botswana", Code = "BW", Continent = "Africa" },
		new Country { Name = "Brazil", Code = "BR", Continent = "South America" },
		new Country { Name = "Brunei", Code = "BN", Continent = "Asia" },
		new Country { Name = "Bulgaria", Code = "BG", Continent = "Europe" },
		new Country { Name = "Burkina Faso", Code = "BF", Continent = "Africa" },
		new Country { Name = "Burundi", Code = "BI", Continent = "Africa" },
		new Country { Name = "Cabo Verde", Code = "CV", Continent = "Africa" },
		new Country { Name = "Cambodia", Code = "KH", Continent = "Asia" },
		new Country { Name = "Cameroon", Code = "CM", Continent = "Africa" },
		new Country { Name = "Canada", Code = "CA", Continent = "North America" },
		new Country { Name = "Central African Republic", Code = "CF", Continent = "Africa" },
		new Country { Name = "Chad", Code = "TD", Continent = "Africa" },
		new Country { Name = "Chile", Code = "CL", Continent = "South America" },
		new Country { Name = "China", Code = "CN", Continent = "Asia" },
		new Country { Name = "Colombia", Code = "CO", Continent = "South America" },
		new Country { Name = "Comoros", Code = "KM", Continent = "Africa" },
		new Country { Name = "Congo (Congo-Brazzaville)", Code = "CG", Continent = "Africa" },
		new Country { Name = "Costa Rica", Code = "CR", Continent = "North America" },
		new Country { Name = "Croatia", Code = "HR", Continent = "Europe" },
		new Country { Name = "Cuba", Code = "CU", Continent = "North America" },
		new Country { Name = "Cyprus", Code = "CY", Continent = "Asia" },
		new Country { Name = "Czechia", Code = "CZ", Continent = "Europe" },
		new Country { Name = "Democratic Republic of the Congo", Code = "CD", Continent = "Africa" },
		new Country { Name = "Denmark", Code = "DK", Continent = "Europe" },
		new Country { Name = "Djibouti", Code = "DJ", Continent = "Africa" },
		new Country { Name = "Dominica", Code = "DM", Continent = "North America" },
		new Country { Name = "Dominican Republic", Code = "DO", Continent = "North America" },
		new Country { Name = "Ecuador", Code = "EC", Continent = "South America" },
		new Country { Name = "Egypt", Code = "EG", Continent = "Africa" },
		new Country { Name = "El Salvador", Code = "SV", Continent = "North America" },
		new Country { Name = "Equatorial Guinea", Code = "GQ", Continent = "Africa" },
		new Country { Name = "Eritrea", Code = "ER", Continent = "Africa" },
		new Country { Name = "Estonia", Code = "EE", Continent = "Europe" },
		new Country { Name = "Eswatini", Code = "SZ", Continent = "Africa" },
		new Country { Name = "Ethiopia", Code = "ET", Continent = "Africa" },
		new Country { Name = "Fiji", Code = "FJ", Continent = "Oceania" },
		new Country { Name = "Finland", Code = "FI", Continent = "Europe" },
		new Country { Name = "France", Code = "FR", Continent = "Europe" },
		new Country { Name = "Gabon", Code = "GA", Continent = "Africa" },
		new Country { Name = "Gambia", Code = "GM", Continent = "Africa" },
		new Country { Name = "Georgia", Code = "GE", Continent = "Asia" },
		new Country { Name = "Germany", Code = "DE", Continent = "Europe" },
		new Country { Name = "Ghana", Code = "GH", Continent = "Africa" },
		new Country { Name = "Greece", Code = "GR", Continent = "Europe" },
		new Country { Name = "Grenada", Code = "GD", Continent = "North America" },
		new Country { Name = "Guatemala", Code = "GT", Continent = "North America" },
		new Country { Name = "Guinea", Code = "GN", Continent = "Africa" },
		new Country { Name = "Guinea-Bissau", Code = "GW", Continent = "Africa" },
		new Country { Name = "Guyana", Code = "GY", Continent = "South America" },
		new Country { Name = "Haiti", Code = "HT", Continent = "North America" },
		new Country { Name = "Honduras", Code = "HN", Continent = "North America" },
		new Country { Name = "Hungary", Code = "HU", Continent = "Europe" },
		new Country { Name = "Iceland", Code = "IS", Continent = "Europe" },
		new Country { Name = "India", Code = "IN", Continent = "Asia" },
		new Country { Name = "Indonesia", Code = "ID", Continent = "Asia" },
		new Country { Name = "Iran", Code = "IR", Continent = "Asia" },
		new Country { Name = "Iraq", Code = "IQ", Continent = "Asia" },
		new Country { Name = "Ireland", Code = "IE", Continent = "Europe" },
		new Country { Name = "Israel", Code = "IL", Continent = "Asia" },
		new Country { Name = "Italy", Code = "IT", Continent = "Europe" },
		new Country { Name = "Jamaica", Code = "JM", Continent = "North America" },
		new Country { Name = "Japan", Code = "JP", Continent = "Asia" },
		new Country { Name = "Jordan", Code = "JO", Continent = "Asia" },
		new Country { Name = "Kazakhstan", Code = "KZ", Continent = "Asia" },
		new Country { Name = "Kenya", Code = "KE", Continent = "Africa" },
		new Country { Name = "Kiribati", Code = "KI", Continent = "Oceania" },
		new Country { Name = "Kuwait", Code = "KW", Continent = "Asia" },
		new Country { Name = "Kyrgyzstan", Code = "KG", Continent = "Asia" },
		new Country { Name = "Laos", Code = "LA", Continent = "Asia" },
		new Country { Name = "Latvia", Code = "LV", Continent = "Europe" },
		new Country { Name = "Lebanon", Code = "LB", Continent = "Asia" },
		new Country { Name = "Lesotho", Code = "LS", Continent = "Africa" },
		new Country { Name = "Liberia", Code = "LR", Continent = "Africa" },
		new Country { Name = "Libya", Code = "LY", Continent = "Africa" },
		new Country { Name = "Liechtenstein", Code = "LI", Continent = "Europe" },
		new Country { Name = "Lithuania", Code = "LT", Continent = "Europe" },
		new Country { Name = "Luxembourg", Code = "LU", Continent = "Europe" },
		new Country { Name = "Madagascar", Code = "MG", Continent = "Africa" },
		new Country { Name = "Malawi", Code = "MW", Continent = "Africa" },
		new Country { Name = "Malaysia", Code = "MY", Continent = "Asia" },
		new Country { Name = "Maldives", Code = "MV", Continent = "Asia" },
		new Country { Name = "Mali", Code = "ML", Continent = "Africa" },
		new Country { Name = "Malta", Code = "MT", Continent = "Europe" },
		new Country { Name = "Marshall Islands", Code = "MH", Continent = "Oceania" },
		new Country { Name = "Mauritania", Code = "MR", Continent = "Africa" },
		new Country { Name = "Mauritius", Code = "MU", Continent = "Africa" },
		new Country { Name = "Mexico", Code = "MX", Continent = "North America" },
		new Country { Name = "Micronesia", Code = "FM", Continent = "Oceania" },
		new Country { Name = "Moldova", Code = "MD", Continent = "Europe" },
		new Country { Name = "Monaco", Code = "MC", Continent = "Europe" },
		new Country { Name = "Mongolia", Code = "MN", Continent = "Asia" },
		new Country { Name = "Montenegro", Code = "ME", Continent = "Europe" },
		new Country { Name = "Morocco", Code = "MA", Continent = "Africa" },
		new Country { Name = "Mozambique", Code = "MZ", Continent = "Africa" },
		new Country { Name = "Myanmar (Burma)", Code = "MM", Continent = "Asia" },
		new Country { Name = "Namibia", Code = "NA", Continent = "Africa" },
		new Country { Name = "Nauru", Code = "NR", Continent = "Oceania" },
		new Country { Name = "Nepal", Code = "NP", Continent = "Asia" },
		new Country { Name = "Netherlands", Code = "NL", Continent = "Europe" },
		new Country { Name = "New Zealand", Code = "NZ", Continent = "Oceania" },
		new Country { Name = "Nicaragua", Code = "NI", Continent = "North America" },
		new Country { Name = "Niger", Code = "NE", Continent = "Africa" },
		new Country { Name = "Nigeria", Code = "NG", Continent = "Africa" },
		new Country { Name = "North Korea", Code = "KP", Continent = "Asia" },
		new Country { Name = "North Macedonia", Code = "MK", Continent = "Europe" },
		new Country { Name = "Norway", Code = "NO", Continent = "Europe" },
		new Country { Name = "Oman", Code = "OM", Continent = "Asia" },
		new Country { Name = "Pakistan", Code = "PK", Continent = "Asia" },
		new Country { Name = "Palau", Code = "PW", Continent = "Oceania" },
		new Country { Name = "Palestine", Code = "PS", Continent = "Asia" },
		new Country { Name = "Panama", Code = "PA", Continent = "North America" },
		new Country { Name = "Papua New Guinea", Code = "PG", Continent = "Oceania" },
		new Country { Name = "Paraguay", Code = "PY", Continent = "South America" },
		new Country { Name = "Peru", Code = "PE", Continent = "South America" },
		new Country { Name = "Philippines", Code = "PH", Continent = "Asia" },
		new Country { Name = "Poland", Code = "PL", Continent = "Europe" },
		new Country { Name = "Portugal", Code = "PT", Continent = "Europe" },
		new Country { Name = "Qatar", Code = "QA", Continent = "Asia" },
		new Country { Name = "Romania", Code = "RO", Continent = "Europe" },
		new Country { Name = "Russia", Code = "RU", Continent = "Europe" },
		new Country { Name = "Rwanda", Code = "RW", Continent = "Africa" },
		new Country { Name = "Saint Kitts and Nevis", Code = "KN", Continent = "North America" },
		new Country { Name = "Saint Lucia", Code = "LC", Continent = "North America" },
		new Country { Name = "Saint Vincent and the Grenadines", Code = "VC", Continent = "North America" },
		new Country { Name = "Samoa", Code = "WS", Continent = "Oceania" },
		new Country { Name = "San Marino", Code = "SM", Continent = "Europe" },
		new Country { Name = "Sao Tome and Principe", Code = "ST", Continent = "Africa" },
		new Country { Name = "Saudi Arabia", Code = "SA", Continent = "Asia" },
		new Country { Name = "Senegal", Code = "SN", Continent = "Africa" },
		new Country { Name = "Serbia", Code = "RS", Continent = "Europe" },
		new Country { Name = "Seychelles", Code = "SC", Continent = "Africa" },
		new Country { Name = "Sierra Leone", Code = "SL", Continent = "Africa" },
		new Country { Name = "Singapore", Code = "SG", Continent = "Asia" },
		new Country { Name = "Slovakia", Code = "SK", Continent = "Europe" },
		new Country { Name = "Slovenia", Code = "SI", Continent = "Europe" },
		new Country { Name = "Solomon Islands", Code = "SB", Continent = "Oceania" },
		new Country { Name = "Somalia", Code = "SO", Continent = "Africa" },
		new Country { Name = "South Africa", Code = "ZA", Continent = "Africa" },
		new Country { Name = "South Korea", Code = "KR", Continent = "Asia" },
		new Country { Name = "South Sudan", Code = "SS", Continent = "Africa" },
		new Country { Name = "Spain", Code = "ES", Continent = "Europe" },
		new Country { Name = "Sri Lanka", Code = "LK", Continent = "Asia" },
		new Country { Name = "Sudan", Code = "SD", Continent = "Africa" },
		new Country { Name = "Suriname", Code = "SR", Continent = "South America" },
		new Country { Name = "Sweden", Code = "SE", Continent = "Europe" },
		new Country { Name = "Switzerland", Code = "CH", Continent = "Europe" },
		new Country { Name = "Syria", Code = "SY", Continent = "Asia" },
		new Country { Name = "Tajikistan", Code = "TJ", Continent = "Asia" },
		new Country { Name = "Tanzania", Code = "TZ", Continent = "Africa" },
		new Country { Name = "Thailand", Code = "TH", Continent = "Asia" },
		new Country { Name = "Timor-Leste", Code = "TL", Continent = "Asia" },
		new Country { Name = "Togo", Code = "TG", Continent = "Africa" },
		new Country { Name = "Tonga", Code = "TO", Continent = "Oceania" },
		new Country { Name = "Trinidad and Tobago", Code = "TT", Continent = "North America" },
		new Country { Name = "Tunisia", Code = "TN", Continent = "Africa" },
		new Country { Name = "Turkey", Code = "TR", Continent = "Asia" },
		new Country { Name = "Turkmenistan", Code = "TM", Continent = "Asia" },
		new Country { Name = "Tuvalu", Code = "TV", Continent = "Oceania" },
		new Country { Name = "Uganda", Code = "UG", Continent = "Africa" },
		new Country { Name = "Ukraine", Code = "UA", Continent = "Europe" },
		new Country { Name = "United Arab Emirates", Code = "AE", Continent = "Asia" },
		new Country { Name = "United Kingdom", Code = "GB", Continent = "Europe" },
		new Country { Name = "United States", Code = "US", Continent = "North America" },
		new Country { Name = "Uruguay", Code = "UY", Continent = "South America" },
		new Country { Name = "Uzbekistan", Code = "UZ", Continent = "Asia" },
		new Country { Name = "Vanuatu", Code = "VU", Continent = "Oceania" },
		new Country { Name = "Vatican City", Code = "VA", Continent = "Europe" },
		new Country { Name = "Venezuela", Code = "VE", Continent = "South America" },
		new Country { Name = "Vietnam", Code = "VN", Continent = "Asia" },
		new Country { Name = "Yemen", Code = "YE", Continent = "Asia" },
		new Country { Name = "Zambia", Code = "ZM", Continent = "Africa" },
		new Country { Name = "Zimbabwe", Code = "ZW", Continent = "Africa" }
	];

	private Country? SelectedItem { get; set; } = _items[0];
	private Country? Combo4SelectedItem { get; set; } = _items[0];

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	private void OnLogEvent(string name)
	{
		EventManager?.Add(new Event(name));
	}

	private void OnSelectedItemChanged(Country? item)
	{
		OnLogEvent($"Selected Item Changed: {item}");

		if (item == null)
		{
			return;
		}
		SelectedItem = item;
	}

    private async void OnCombo4SelectedItemChanged(Country? item)
    {
		Combo4SelectedItem = item;
		StateHasChanged();
		// Simulate some work as behaviour is different
		await Task.Delay(500);
		OnLogEvent($"Combo4 Selected Item Changed: {item}");
    }

	class Country
	{
		public required string Name { get; set; }
		public required string Code { get; set; }
		public required string Continent { get; set; }

		public override string ToString() => $"{Name} ({Code})";
	}
}