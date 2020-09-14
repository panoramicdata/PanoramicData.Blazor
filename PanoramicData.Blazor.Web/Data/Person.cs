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

		public int Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		[Display(Name = "Date of Birth")]
		public DateTime Dob { get; set; }
		public Departments Department { get; set; }
		public decimal Target { get; set; }
		public string Location { get; set; }
		public string Comments { get; set; }
		[Display(Name = "Login?")]
		public bool AllowLogin { get; set; }
		public string Password { get; set; }
		[Display(Name = "Created")]
		public DateTimeOffset DateCreated { get; set; }
		[Display(Name = "Modified")]
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
