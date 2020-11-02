using System;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Shared
{
	public partial class EventView : IDisposable
    {
    	[Parameter] public RenderFragment? ChildContent { get; set; }

		[CascadingParameter] protected EventManager? EventManager { get; set; }

		protected override void OnInitialized()
		{
			if (EventManager != null)
			{
				EventManager.EventAdded += OnEventAdded;
			}
		}

		private void OnEventAdded(Event evt)
		{
			StateHasChanged();
		}

		public void Dispose()
		{
			if (EventManager != null)
			{
				EventManager.EventAdded -= OnEventAdded;
			}
		}
	}
}
