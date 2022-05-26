namespace PanoramicData.Blazor.Models;

public class TimeRange
{
	private DateTime _startTime;
	private DateTime _endTime;

	public TimeRange()
	{
		StartTime = DateTime.Today.Date;
		EndTime = StartTime.AddDays(1);
	}

	public DateTime StartTime
	{
		get { return _startTime; }
		set { _startTime = value; }
	}

	public DateTime EndTime
	{
		get { return _endTime; }
		set { _endTime = value; }
	}

	public override string ToString()
	{
		return $"{StartTime:g} - {EndTime:g}";
	}
}
