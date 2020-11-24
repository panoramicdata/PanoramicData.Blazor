using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

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
			if (firstRender)
			{
				_dotNetReference = DotNetObjectReference.Create(this);
				await JSRuntime.InvokeVoidAsync("panoramicData.initializeDropZone", Id, UploadUrl, _dotNetReference);
			}
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnDrop")]
		public async Task<object> OnDrop(DropZoneFile[] files)
		{
			var args = new DropZoneEventArgs(this, files);
			await Drop.InvokeAsync(args).ConfigureAwait(true);
			return new
			{
				cancel = args.Cancel,
				reason = args.CancelReason,
				state = args.State,
				files
			};
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadBegin")]
		public async Task<string[]> OnUploadBeginAsync(DropZoneFile file)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}
			if (file.Path is null)
			{
				throw new ArgumentException("file's Path Property should not be null.", nameof(file));
			}
			if (file.Name is null)
			{
				throw new ArgumentException("file's Name Property should not be null.", nameof(file));
			}
			var args = new DropZoneUploadEventArgs(file.Path, file.Name, file.Size);
			await UploadStarted.InvokeAsync(args).ConfigureAwait(true);
			if (args.FormFields.Count == 0)
			{
				return new string[0];
			}
			else
			{
				var fields = new System.Collections.Generic.List<string>();
				foreach (var kvp in args.FormFields)
				{
					fields.Add($"{kvp.Key}={kvp.Value}");
				}
				return fields.ToArray();
			}

		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadProgress")]
		public void OnUploadProgress(DropZoneFileUploadProgress file)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}
			if (file.Path is null)
			{
				throw new ArgumentException("file's Path Property should not be null.", nameof(file));
			}
			if (file.Name is null)
			{
				throw new ArgumentException("file's Name Property should not be null.", nameof(file));
			}
			UploadProgress.InvokeAsync(new DropZoneUploadProgressEventArgs(file.Path, file.Name, file.Size, file.Progress));
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadEnd")]
		public void OnUploadEnd(DropZoneFileUploadOutcome file)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}
			if (file.Path is null)
			{
				throw new ArgumentException("file's Path Property should not be null.", nameof(file));
			}
			if (file.Name is null)
			{
				throw new ArgumentException("file's Name Property should not be null.", nameof(file));
			}

			UploadCompleted.InvokeAsync(new DropZoneUploadEventArgs(file.Path, file.Name, file.Size));
		}

		public void Dispose()
		{
			JSRuntime.InvokeVoidAsync("panoramicData.disposeDropZone", Id);
			_dotNetReference?.Dispose();
		}
	}
}
