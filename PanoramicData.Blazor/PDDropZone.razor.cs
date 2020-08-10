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
		/// Event raised whenever a file upload starts.
		/// </summary>
		[Parameter] public EventCallback<DropZoneUploadEventArgs> UploadStarted { get; set; }

		/// <summary>
		/// Event raised periodically during a file upload.
		/// </summary>
		[Parameter] public EventCallback<DropZoneUploadProgressEventArgs> UploadProgress { get; set; }

		/// <summary>
		/// Event raised whenever a file upload completes.
		/// </summary>
		[Parameter] public EventCallback<DropZoneUploadEventArgs> UploadCompleted { get; set; }

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
			UploadStarted.InvokeAsync(new DropZoneUploadEventArgs(file.Path, file.Name, file.Size));
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadProgress")]
		public void OnUploadProgress(DropZoneFileUploadProgress file)
		{
			UploadProgress.InvokeAsync(new DropZoneUploadProgressEventArgs(file.Path, file.Name, file.Size, file.Progress));
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadEnd")]
		public void OnUploadEnd(DropZoneFileUploadOutcome file)
		{
			UploadCompleted.InvokeAsync(new DropZoneUploadEventArgs(file.Path, file.Name, file.Size));
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
