using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Models
{
	public enum FilterTypes
	{
		[Display(Name = "Equals")]
		Equals,
		[Display(Name = "Does Not Equal")]
		DoesNotEqual,
		[Display(Name = "Starts With")]
		StartsWith,
		[Display(Name = "Ends With")]
		EndsWith,
		[Display(Name = "Contains")]
		Contains,
		[Display(Name = "Does Not Contain")]
		DoesNotContain,
		[Display(Name = "In")]
		In
	}
}
