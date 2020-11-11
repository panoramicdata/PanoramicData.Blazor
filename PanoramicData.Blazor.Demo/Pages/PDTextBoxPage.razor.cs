using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Pages
{
    public partial class PDTextBoxPage
    {
		[CascadingParameter] protected EventManager? EventManager { get; set; }

		private bool Visible { get; set; } = true;
		private bool Enabled { get; set; } = true;
		private string Value { get; set; } = "Hello World!";

		private void OnValueChanged(string value)
		{
			Value = value;
			EventManager?.Add(new Event("ValueChanged", new EventArgument("Value", value)));
		}

	}
}
