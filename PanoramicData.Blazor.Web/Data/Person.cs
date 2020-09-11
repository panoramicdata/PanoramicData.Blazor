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
		public int Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		[Display(Name = "Date of Birth")]
		public DateTime Dob { get; set; }
		public Departments Department { get; set; }
		public decimal Target { get; set; }
		public string Comments { get; set; }
		public bool AllowLogin { get; set; }
		[Display(Name = "Login Id")]
		public string LoginId { get; set; }
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
