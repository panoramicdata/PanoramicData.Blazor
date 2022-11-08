namespace PanoramicData.Blazor.Demo.Data;

//[Display(Name = "Individual")]
public class Person
{
	public Person()
	{
		Dob = DateTime.Today;
		DateCreated = DateTimeOffset.Now;
	}

	[Display(Description = "Persons unique identifier")]
	public int Id { get; set; }

	[Display(Name = "First Name", Description = "Persons first / forename")]
	[FilterKey("fn")]
	[Required]
	[MinLength(2, ErrorMessage = "Minimum length is 2 characters")]
	[StringLength(10, ErrorMessage = "Maximum length is 10 characters")]
	public string? FirstName { get; set; } = string.Empty;

	[Display(Description = "Optional middle name initials")]
	public string Initials { get; set; } = string.Empty;

	[Display(Name = "Last Name", Description = "Persons last / surname", ShortName = "ln")]
	public string LastName { get; set; } = string.Empty;

	[Display(Name = "Email Address", Description = "Primary email address used to contact the person")]
	[Required]
	[RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Invalid Email Address")]
	public string Email { get; set; } = string.Empty;

	[Display(Name = "Date of Birth", Description = "Persons date of birth")]
	[FilterKey("dob")]
	public DateTime Dob { get; set; }

	[Display(Description = "Department person is currently working within")]
	[FilterKey("dept")]
	public Departments Department { get; set; }

	[Display(Description = "Sales persons monthly sales target")]
	[FilterKey("tgt")]
	public decimal Target { get; set; }

	[Display(Description = "Major city that the person works nearest")]
	[FilterKey("loc")]
	public int Location { get; set; }

	[Display(Description = "Free form notes and comments")]
	[FilterKey("com")]
	public string Comments { get; set; } = string.Empty;

	[Display(Name = "Login?", Description = "Is the person able to access the system?")]
	public bool AllowLogin { get; set; }

	[Display(Description = "Password person uses to access the system")]
	public string Password { get; set; } = string.Empty;

	[Display(Name = "Created", Description = "Date and time the record was created")]
	[FilterKey("created")]
	public DateTimeOffset DateCreated { get; set; }

	[Display(Name = "Modified", Description = "Date and time the record was last modified")]
	[FilterKey("modified")]
	public DateTimeOffset? DateModified { get; set; }

	[Display(Name = "Username", Description = "Login username")]
	[FilterKey("user")]
	public string Username { get; set; } = string.Empty;

	[FilterKey("boss")]
	public Person? Manager { get; set; }
}

public enum Departments
{
	Marketing,

	Operations,

	Sales,

	[Display(Name = "IT Helpdesk")]
	Support
}
