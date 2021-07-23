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
		private int _batchCount;
		private int _batchProgress;

		[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

		/// <summary>
		/// Sets additional CSS classes.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = string.Empty;

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
		[Parameter] public EventCallback<DropZoneUploadCompletedEventArgs> UploadCompleted { get; set; }

		/// <summary>
		/// Event raised before uploads have started.
		/// </summary>
		[Parameter] public EventCallback<int> AllUploadsStarted { get; set; }

		/// <summary>
		/// Event raised during batch uploads.
		/// </summary>
		[Parameter] public EventCallback<DropZoneAllProgressEventArgs> AllUploadsProgress { get; set; }

		/// <summary>
		/// Event raised when all uploads have completed.
		/// </summary>
		[Parameter] public EventCallback AllUploadsComplete { get; set; }

		/// <summary>
		/// Gets or sets the URL where file uploads should be sent.
		/// </summary>
		[Parameter] public string? UploadUrl { get; set; }

		/// <summary>
		/// Gets or sets a unique identifier for the upload session.
		/// </summary>
		[Parameter] public string SessionId { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Sets the maximum time in seconds to wait for an upload to complete.
		/// </summary>
		[Parameter] public int Timeout { get; set; } = 60;

		/// <summary>
		/// Sets the maximum file upload size in MB.
		/// </summary>
		[Parameter] public int MaxFileSize { get; set; } = 256;

		/// <summary>
		/// Sets whether to auto scroll when multiple files uploaded.
		/// </summary>
		[Parameter] public bool AutoScroll { get; set; } = true;

		/// <summary>
		/// Optional CSS selector where preview elements are added.
		/// </summary>
		[Parameter] public string PreviewContainer { get; set; } = string.Empty;

		/// <summary>
		/// Optional CSS selector identifying upload item template.
		/// </summary>
		[Parameter] public string PreviewTemplate { get; set; } = string.Empty;

		/// <summary>
		/// Gets the unique identifier of this panel.
		/// </summary>
		[Parameter] public string Id { get; set; } = string.Empty;

		protected override void OnInitialized()
		{
			if (Id == string.Empty)
			{
				Id = $"pddz{++_idSequence}";
			}
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				_dotNetReference = DotNetObjectReference.Create(this);
				var options = new
				{
					url = UploadUrl,
					timeout = Timeout * 1000,
					autoScroll = AutoScroll,
					maxFilesize = MaxFileSize,
					previewsContainer = PreviewContainer,
					previewItemTemplate = PreviewTemplate
				};
				if (!string.IsNullOrWhiteSpace(UploadUrl))
				{
					await JSRuntime.InvokeVoidAsync("panoramicData.initDropzone", $"#{Id}", options, SessionId, _dotNetReference).ConfigureAwait(true);
				}
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
				rootDir = args.BaseFolder,
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
			var args = new DropZoneUploadEventArgs(file.Path, file.Name, file.Size, file.Key, file.SessionId);
			//			_batchProgress++;
			args.BatchCount = _batchCount;
			args.BatchProgress = _batchProgress;
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
			UploadProgress.InvokeAsync(new DropZoneUploadProgressEventArgs(file.Path, file.Name, file.Size, file.Key, file.SessionId, file.Progress));
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
			var args = new DropZoneUploadCompletedEventArgs(file.Path, file.Name, file.Size, file.Key, file.SessionId)
			{
				BatchCount = _batchCount,
				BatchProgress = ++_batchProgress
			};
			if (file.Success)
			{
				UploadCompleted.InvokeAsync(args);
			}
			else
			{
				args.Reason = file.Reason;
				UploadCompleted.InvokeAsync(args);
			}
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnAllUploadsComplete")]
		public void OnAllUploadsComplete()
		{
			_batchCount = 0;
			_batchProgress = 0;
			AllUploadsComplete.InvokeAsync(null);
		}

		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnAllUploadsProgress")]
		public void OnAllUploadsProgress(double uploadProgress, long totalBytes, long totalBytesSent)
		{
			AllUploadsProgress.InvokeAsync(new DropZoneAllProgressEventArgs
			{
				TotalBytes = totalBytes,
				TotalBytesSent = totalBytesSent,
				UploadProgress = uploadProgress
			});
		}
		[JSInvokable("PanoramicData.Blazor.PDDropZone.OnAllUploadsStarted")]
		public void OnAllUploadsStarted(int fileCount)
		{
			_batchCount = fileCount;
			_batchProgress = 0;
			AllUploadsStarted.InvokeAsync(fileCount);
		}

		public void Dispose()
		{
			JSRuntime.InvokeVoidAsync("panoramicData.disposeDropZone", Id);
			_dotNetReference?.Dispose();
		}
	}
}
