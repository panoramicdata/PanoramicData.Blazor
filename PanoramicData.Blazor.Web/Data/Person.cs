using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Web.Data
{
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
		[Required]
		[MinLength(2, ErrorMessage = "Minimum length is 2 characters")]
		[Display(Name = "First Name", Description = "Persons first / forename")]
		public string FirstName { get; set; }
		[Display(Description = "Optional middle name initials")]
		public string Initials { get; set; }
		[Display(Name = "Last Name", Description = "Persons last / surname")]
		public string LastName { get; set; }
		[Required]
		[Display(Name = "Email Address", Description = "Primary email address used to contact the person")]
		public string Email { get; set; }
		[Display(Name = "Date of Birth", Description = "Persons date of birth")]
		public DateTime Dob { get; set; }
		[Display(Description = "Department person is currently working within")]
		public Departments Department { get; set; }
		[Display(Description = "Sales persons monthly sales target")]
		public decimal Target { get; set; }
		[Display(Description = "Major city that the person works nearest")]
		public int Location { get; set; }
		[Display(Description = "Free form notes and comments")]
		public string Comments { get; set; }
		[Display(Name = "Login?", Description = "Is the person able to access the system?")]
		public bool AllowLogin { get; set; }
		[Display(Description = "Password person uses to access the system")]
		public string Password { get; set; }
		[Display(Name = "Created", Description = "Date and time the record was created")]
		public DateTimeOffset DateCreated { get; set; }
		[Display(Name = "Modified", Description = "Date and time the record was last modified")]
		public DateTimeOffset DateModified { get; set; }
	}

	public enum Departments
	{
		Marketing,

		Operations,

		Sales,

		[Display(Name = "IT Helpdesk")]
		Support
	}
}
