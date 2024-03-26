namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDateTimePage : IDisposable
{
	private DateTime _value1 = DateTime.Now;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	protected override void OnInitialized()
	{
	}

	public void Dispose()
	{
	}

	private void OnValue1Changed(DateTime dateTime)
	{
		_value1 = dateTime;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Value", dateTime.ToString())));
	}
}
