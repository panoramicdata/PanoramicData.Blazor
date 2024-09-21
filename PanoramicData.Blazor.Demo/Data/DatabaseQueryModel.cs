namespace PanoramicData.Blazor.Demo.Data;

public class DatabaseQueryModel
{
	[Display(Name = "Email Address")]
	public string EmailAddress { get; set; } = string.Empty;

	[Display(Name = "Frequency Period")]
	public TimePeriods FrequencyPeriod { get; set; } = TimePeriods.Week;

	[Display(Name = "Frequency Unit")]
	[Range(1, 99)]
	public ushort FrequencyUnit { get; set; } = 1;

	[Display(Name = "SQL Query")]
	[MaxLength(10000)]
	public string SqlQuery { get; set; } = string.Empty;
}
