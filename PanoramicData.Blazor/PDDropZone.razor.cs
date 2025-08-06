namespace PanoramicData.Blazor;

public partial class PDDropZone : IAsyncDisposable
{
	private static int _idSequence;
	private DotNetObjectReference<PDDropZone>? _dotNetReference;
	private int _batchCount;
	private int _batchProgress;
	private IJSObjectReference? _module;

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;


	/// <summary>
	/// Gets or sets the child content that the drop zone wraps.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets whether the user can click to initiate an upload.
	/// </summary>
	[Parameter] public bool Clickable { get; set; } = true;

	/// <summary>
	/// Sets additional CSS classes.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Event raised whenever the user drops files onto the drop zone.
	/// </summary>
	[Parameter] public EventCallback<DropZoneEventArgs> Drop { get; set; }

	/// <summary>
	/// Event raised whenever the user finished pressing a key.
	/// </summary>
	[Parameter] public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

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
	/// Event raised when all files are ready to be uploaded.
	/// </summary>
	[Parameter] public EventCallback<UploadsReadyEventArgs> AllUploadsReady { get; set; }

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

	public async Task CancelAsync()
	{
		if (_module != null)
		{
			await _module.InvokeVoidAsync("cancel", Id).ConfigureAwait(true);
		}
	}

	public async Task ClearAsync()
	{
		if (_module != null)
		{
			await _module.InvokeVoidAsync("clear", Id).ConfigureAwait(true);
		}
	}

	protected override void OnInitialized()
	{
		base.OnInitialized();
		if (Id == string.Empty)
		{
			Id = $"pddz{++_idSequence}";
		}
	}

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_dotNetReference = DotNetObjectReference.Create(this);
				var options = new
				{
					clickable = Clickable,
					url = UploadUrl,
					autoProcessQueue = false,
					timeout = Timeout * 1000,
					autoScroll = AutoScroll,
					maxFilesize = MaxFileSize,
					previewsContainer = PreviewContainer,
					previewItemTemplate = PreviewTemplate
				};
				if (!string.IsNullOrWhiteSpace(UploadUrl))
				{
					_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDDropZone.razor.js").ConfigureAwait(true);
					if (_module != null)
					{
						await _module.InvokeVoidAsync("initialize", $"#{Id}", options, SessionId, _dotNetReference).ConfigureAwait(true);
					}
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
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
			args.Files
		};
	}

	private Task OnKeyDown(KeyboardEventArgs args)
	{
		return KeyDown.InvokeAsync(args);
	}

	[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadBegin")]
	public async Task<string[]> OnUploadBeginAsync(DropZoneFile file)
	{
		ArgumentNullException.ThrowIfNull(file);

		if (file.Path is null)
		{
			throw new ArgumentException("file's Path Property should not be null.", nameof(file));
		}

		if (file.Name is null)
		{
			throw new ArgumentException("file's Name Property should not be null.", nameof(file));
		}

		var args = new DropZoneUploadEventArgs(file.Path, file.Name, file.Size, file.Key, file.SessionId)
		{
			//_batchProgress++;
			BatchCount = _batchCount,
			BatchProgress = _batchProgress
		};
		await UploadStarted.InvokeAsync(args).ConfigureAwait(true);
		if (args.FormFields.Count == 0)
		{
			return [];
		}
		else
		{
			var fields = new List<string>();
			foreach (var kvp in args.FormFields)
			{
				fields.Add($"{kvp.Key}={kvp.Value}");
			}

			return [.. fields];
		}
	}

	[JSInvokable("PanoramicData.Blazor.PDDropZone.OnUploadProgress")]
	public void OnUploadProgress(DropZoneFileUploadProgress file)
	{
		ArgumentNullException.ThrowIfNull(file);

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
		ArgumentNullException.ThrowIfNull(file);

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
			BatchProgress = ++_batchProgress,
			Success = file.Success
		};
		if (!args.Success)
		{
			args.Reason = file.Reason;
		}

		UploadCompleted.InvokeAsync(args);
	}

	[JSInvokable("PanoramicData.Blazor.PDDropZone.OnAllUploadsReady")]
	public async Task OnAllUploadsReadyAsync(DropZoneFile[] files)
	{
		// allow app to check for conflicts and prompt user
		var args = new UploadsReadyEventArgs
		{
			Files = files
		};
		await AllUploadsReady.InvokeAsync(args).ConfigureAwait(true);

		if (args.Cancel)
		{
			// reset batch vars
			_batchCount = 0;
			_batchProgress = 0;

			// clear dropzone queue
			if (_module != null)
			{
				await _module.InvokeVoidAsync("cancel", $"#{Id}").ConfigureAwait(true);
			}
		}
		else
		{
			// initialize batch
			_batchCount = args.Files.Length - args.FilesToSkip.Length;
			_batchProgress = 0;
			await AllUploadsStarted.InvokeAsync(_batchCount).ConfigureAwait(true);

			if (_module != null)
			{
				// remove skipped files and then proceed
				foreach (var file in args.FilesToSkip)
				{
					await _module.InvokeVoidAsync("removeFile", $"#{Id}", file.Key).ConfigureAwait(true);
				}

				// begin processing queue
				await _module.InvokeVoidAsync("process", $"#{Id}", args.Overwrite).ConfigureAwait(true);
			}
		}
	}

	[JSInvokable("PanoramicData.Blazor.PDDropZone.OnAllUploadsComplete")]
	public async Task OnAllUploadsComplete()
	{
		// reste batch vars
		_batchCount = 0;
		_batchProgress = 0;

		// clear dropzone queue
		if (_module != null)
		{
			await _module.InvokeVoidAsync("cancel", $"#{Id}").ConfigureAwait(true);
		}

		// notify app
		await AllUploadsComplete.InvokeAsync(null).ConfigureAwait(true);
	}

	[JSInvokable("PanoramicData.Blazor.PDDropZone.OnAllUploadsProgress")]
	public void OnAllUploadsProgress(double uploadProgress, long totalBytes, long totalBytesSent) => AllUploadsProgress.InvokeAsync(new DropZoneAllProgressEventArgs
	{
		TotalBytes = totalBytes,
		TotalBytesSent = totalBytesSent,
		UploadProgress = uploadProgress
	});

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				await _module.InvokeVoidAsync("dispose", Id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}

			_dotNetReference?.Dispose();
		}
		catch
		{
		}
	}
}
