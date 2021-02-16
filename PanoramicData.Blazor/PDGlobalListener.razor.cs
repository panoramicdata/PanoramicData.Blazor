using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDGlobalListener : IDisposable
	{
		private DotNetObjectReference<PDGlobalListener>? _dotNetObjectReference;

		[Parameter] public RenderFragment? ChildContent { get; set; }

		[Inject] private IGlobalEventService GlobalEventService { get; set; } = null!;

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		public void Dispose()
		{
			GlobalEventService.ShortcutsChanged -= GlobalEventService_ShortcutsChanged;
			JSRuntime!.InvokeVoidAsync("panoramicData.destroyGlobalListener").GetAwaiter().GetResult();
		}

		protected override void OnInitialized()
		{
			GlobalEventService.ShortcutsChanged += GlobalEventService_ShortcutsChanged;
		}

		private void GlobalEventService_ShortcutsChanged(object sender, IEnumerable<ShortcutKey> shortcuts)
		{
			JSRuntime!.InvokeVoidAsync("panoramicData.registerShortcutKeys", shortcuts);
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender && JSRuntime != null)
			{
				_dotNetObjectReference = DotNetObjectReference.Create<PDGlobalListener>(this);
				await JSRuntime.InvokeVoidAsync("panoramicData.initGlobalListener", _dotNetObjectReference).ConfigureAwait(true);
			}
		}

		[JSInvokable]
		public void OnKeyDown(KeyboardInfo keyboardInfo)
		{
			GlobalEventService?.KeyDown(keyboardInfo);
		}

		[JSInvokable]
		public void OnKeyUp(KeyboardInfo keyboardInfo)
		{
			GlobalEventService?.KeyUp(keyboardInfo);
		}
	}
}
