using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDDropZone : IDisposable
    {
		private static int _idSequence;
		private DotNetObjectReference<PDDropZone>? _dotNetReference;

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		/// <summary>
		/// Gets or sets the child content that the drop zone wraps.
		/// </summary>
		[Parameter] public RenderFragment? ChildContent { get; set; }

		/// <summary>
		/// Event raised whenever the user drops files onto the drop zone.
		/// </summary>
		[Parameter] public EventCallback<DropZoneEventArgs> Drop { get; set; }

		/// <summary>
		/// Gets or sets the URL where file uploads should be sent.
		/// </summary>
		[Parameter] public string? UploadUrl { get; set; }

		/// <summary>
		/// Gets the unique identifier of this panel.
		/// </summary>
		public string Id { get; private set; } = string.Empty;

		protected override void OnInitialized()
		{
			Id = $"pddz{++_idSequence}";
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if(firstRender)
			{
				_dotNetReference = DotNetObjectReference.Create(this);
				await JSRuntime.InvokeVoidAsync("initializeDropZone", Id, UploadUrl, _dotNetReference);
			}
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnDrop")]
		public object OnDrop(DropZoneFile[] files)
		{
			var args = new DropZoneEventArgs(this, files);
			Drop.InvokeAsync(args);
			return new
			{
				cancel = args.Cancel,
				reason = args.CancelReason,
				state = args.State
			};
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadBegin")]
		public void OnUploadBegin(DropZoneFile file)
		{
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadProgress")]
		public void OnUploadProgress(DropZoneFile file)
		{
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadEnd")]
		public void OnUploadEnd(DropZoneFileUploadOutcome outcome)
		{
			var a = 1;
		}

		public void Dispose()
		{
			JSRuntime.InvokeVoidAsync("disposeDropZone", Id);
			if (_dotNetReference != null)
			{
				_dotNetReference.Dispose();
			}
		}
	}
}
