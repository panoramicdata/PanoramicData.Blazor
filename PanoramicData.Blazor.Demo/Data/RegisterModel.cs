namespace PanoramicData.Blazor.Demo.Data;

public class RegisterModel
{
	[Required]
	[MaxLength(100)]
	public string EmailAddress { get; set; } = string.Empty;

	[Display(Name = "First Name")]
	[Required]
	[MaxLength(50)]
	public string FirstName { get; set; } = string.Empty;

	[MaxLength(10)]
	public string Initials { get; set; } = string.Empty;

	[Display(Name = "Last Name")]
	[Required]
	[MaxLength(50)]
	public string LastName { get; set; } = string.Empty;

	public bool ReportFormatDocx { get; set; } = false;

	public bool ReportFormatHtml { get; set; } = false;

	public bool ReportFormatPdf { get; set; } = false;

	public bool ReportFormatXlsx { get; set; } = false;

	public bool Report { get; set; }

	[Display(Name = "Email Address")]
	public string ReportEmailAddress { get; set; } = string.Empty;

	[Display(Name = "Frequency Period")]
	public TimePeriods ReportFrequencyPeriod { get; set; } = TimePeriods.Year;

	[Display(Name = "Frequency Unit")]
	[Range(1, 99)]
	public ushort ReportFrequencyUnit { get; set; } = 1;
}
