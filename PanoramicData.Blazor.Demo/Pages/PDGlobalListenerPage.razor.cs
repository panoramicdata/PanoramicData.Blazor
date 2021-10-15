using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;
using System;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDGlobalListenerPage : IDisposable
	{
		[CascadingParameter] protected EventManager? EventManager { get; set; }

		[Inject] public IGlobalEventService? GlobalEventService { get; set; }

		protected override void OnInitialized()
		{
			if (GlobalEventService != null)
			{
				GlobalEventService.KeyDownEvent += GlobalEventService_KeyDownEvent;
				GlobalEventService.KeyUpEvent += GlobalEventService_KeyUpEvent;
			}
		}

		private void GlobalEventService_KeyDownEvent(object? sender, KeyboardInfo e)
		{
			EventManager?.Add(new Event("KeyDown", new EventArgument("Key", e.Key),
						 						 new EventArgument("AltKey", e.AltKey),
						 						 new EventArgument("ShiftKey", e.ShiftKey),
												 new EventArgument("CtrlKey", e.CtrlKey)));
		}

		private void GlobalEventService_KeyUpEvent(object? sender, KeyboardInfo e)
		{
			EventManager?.Add(new Event("KeyUp", new EventArgument("Key", e.Key),
						 						 new EventArgument("AltKey", e.AltKey),
						 						 new EventArgument("ShiftKey", e.ShiftKey),
												 new EventArgument("CtrlKey", e.CtrlKey)));
		}

		public void Dispose()
		{
			if (GlobalEventService != null)
			{
				GlobalEventService.KeyUpEvent -= GlobalEventService_KeyUpEvent;
				GlobalEventService.KeyUpEvent -= GlobalEventService_KeyDownEvent;
			}
		}
	}
}
