namespace PanoramicData.Blazor.Models;

public class TimeRange
{
	public TimeRange()
	{
		StartTime = DateTime.Today.Date;
		EndTime = StartTime.AddDays(1);
	}

	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public override string ToString() => $"{StartTime:g} - {EndTime:g}";
}
