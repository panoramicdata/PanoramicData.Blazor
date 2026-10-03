using System.Security.Cryptography;

namespace PanoramicData.Blazor.Demo.Data;

public partial class PersonDataProvider : DataProviderBase<Person>
{
	private static readonly string _loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce at leo eu risus faucibus facilisis quis in tortor. Phasellus gravida libero sit amet ullamcorper rhoncus. Ut at viverra lectus. Vestibulum mi eros, egestas vel nulla at, lacinia ornare mauris. Morbi a pulvinar lacus. Praesent ut convallis magna. Etiam est sem, feugiat a leo in, viverra scelerisque lectus. Vivamus dictum luctus eros non ultrices. Curabitur enim enim, porta eu lorem ut, varius venenatis sem.";
	private static readonly string[] _firstNames = ["Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Judy", "Mike"];
	private static readonly string[] _lastNames = ["Smith", "Cooper", "Watkins", "Jenkins", "Van Holden", "Williams", "Jones", "Smithson", "Carter", "Miller", "Baker"];
	private static readonly List<Person> _people = [];
	public static readonly string[] Locations = ["Paris", "Rome", "Milan", "New York", "Peckham", "Sydney"];

	public PersonDataProvider() : this(255) { }

	/// <summary>
	/// Add a small delay to simulate network latency.
	/// </summary>
	public bool AddDelay { get; set; }

	public static List<Person> GetAllPersons()
	{
		return _people;
	}

	public PersonDataProvider(int count)
	{
		// generate random rows
		if (_people.Count == 0)
		{
			foreach (var id in Enumerable.Range(1, count))
			{
				_people.Add(CreateRandomPerson(id));
			}
		}
	}

	private static Person CreateRandomPerson(int id)
	{
		var boss1 = new Person
		{
			FirstName = "Peter",
			LastName = "Simmons"
		};
		var boss2 = new Person
		{
			FirstName = "Lucy",
			LastName = "Waterman"
		};
		var person = new Person
		{
			Id = id,
			AllowLogin = RandomNumberGenerator.GetInt32(0, 2) == 1,
			DateCreated = DateTimeOffset.Now.AddDays(RandomNumberGenerator.GetInt32(-365, 0)),
			DateModified = RandomNumberGenerator.GetInt32(10) < 3 ? null : DateTimeOffset.Now.AddDays(RandomNumberGenerator.GetInt32(-30, 0)),
			Department = (Departments)RandomNumberGenerator.GetInt32(0, 4),
			FirstName = RandomNumberGenerator.GetInt32(10) < 2 ? null : _firstNames[RandomNumberGenerator.GetInt32(_firstNames.Length)],
			LastName = _lastNames[RandomNumberGenerator.GetInt32(_lastNames.Length)],
			Location = RandomNumberGenerator.GetInt32(Locations.Length),
			Dob = DateTime.Today.AddYears(-RandomNumberGenerator.GetInt32(20, 50)),
			Comments = _loremIpsum[..RandomNumberGenerator.GetInt32(0, _loremIpsum.Length)],
			Password = "Password",
			IsFirstAider = RandomNumberGenerator.GetInt32(0, 4) switch { 0 => true, 1 => false, _ => null },
			Dependents = RandomNumberGenerator.GetInt32(0, 2) == 0 ? null : RandomNumberGenerator.GetInt32(1, 4),
		};
		List<Person?> managers = [boss1, boss2, null];
		person.Manager = managers[RandomNumberGenerator.GetInt32(0, 3)]!;
		person.Email = RandomNumberGenerator.GetInt32(10) < 2
			? string.Empty
			: $"{person.FirstName?.ToLowerInvariant() ?? _firstNames[RandomNumberGenerator.GetInt32(_firstNames.Length)].ToLowerInvariant()}.{person.LastName.ToLowerInvariant()}@acme.com";
		return person;
	}
}
