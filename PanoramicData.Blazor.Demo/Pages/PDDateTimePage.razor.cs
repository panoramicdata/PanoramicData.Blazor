namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDateTimePage
{
	private DateTime _value1 = DateTime.Now;
	private DateTime _value2 = DateTime.Now;
	// MUST initialize time to 0 seconds if wishing to show only hours and minutes
	private DateTime _value3 = DateTime.Today.AddHours(DateTime.Now.Hour).AddMinutes(DateTime.Now.Minute);
	// MUST initialize time to 0 seconds if wishing to show only hours
	private DateTime _value4 = DateTime.Today.AddHours(DateTime.Now.Hour);
	private DateTimeOffset _value5 = DateTime.Now;
	private DateTimeOffset _value6 = DateTime.Now;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void OnValue1Changed(DateTime dateTime)
	{
		_value1 = dateTime;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Value1", dateTime.ToString())));
	}

	private void OnValue2Changed(DateTime dateTime)
	{
		_value2 = dateTime;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Value2", dateTime.ToString())));
	}

	private void OnValue3Changed(DateTime dateTime)
	{
		_value3 = dateTime;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Value3", dateTime.ToString())));
	}

	private void OnValue4Changed(DateTime dateTime)
	{
		_value4 = dateTime;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Value4", dateTime.ToString())));
	}

	private void OnValue5Changed(DateTimeOffset dateTime)
	{
		_value5 = dateTime;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Value5", dateTime.ToString())));
	}

	private void OnValue6Changed(DateTimeOffset dateTime)
	{
		_value6 = dateTime;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Value6", dateTime.ToString())));
	}
}
