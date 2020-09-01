using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Web.Data
{
	public class Person
	{
		public int Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public Departments Department { get; set; }
		public bool AllowLogin { get; set; }
		public decimal Target { get; set; }
		public string Comments { get; set; }
		public DateTimeOffset DateCreated { get; set; }
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
