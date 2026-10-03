namespace PanoramicData.Blazor;

/// <summary>
/// Uploading files into a <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorer
{
	private async Task OnFilesDroppedAsync(DropZoneEventArgs args)
	{
		if (Tree?.SelectedNode?.Data != null)
		{
			// ensure class is added to drop zone dialog to hide message
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("addClass", "pdfe-drop-zone-1", "dz-started").ConfigureAwait(true);
			}

			// set current folder
			args.BaseFolder = FolderPath;

			// add current path so it can be passed along with uploads
			args.State = Tree.SelectedNode.Data.Path;
		}
	}

	private async Task OnUploadStartedAsync(DropZoneUploadEventArgs args)
	{
		_batchCount = args.BatchCount;
		_batchProgress = args.BatchProgress;
		_batchFiles.Add(args.FullPath, 0);

		await UploadStarted.InvokeAsync(args).ConfigureAwait(false);
	}

	private async Task OnUploadProgressAsync(DropZoneUploadProgressEventArgs args)
	{
		if (_batchFiles.ContainsKey(args.FullPath))
		{
			_batchFiles[args.FullPath] = args.Progress;
		}

		if (Table is null)
		{
			return;
		}

		// fetch / create UI item to provide upload feedback
		var item = GetVirtualFileItem(args);
		if (item != null)
		{
			item.UploadProgress = args.Progress;
		}

		await UploadProgress.InvokeAsync(args).ConfigureAwait(true);
	}

	private async Task OnUploadCompletedAsync(DropZoneUploadCompletedEventArgs args)
	{
		if (_batchFiles.ContainsKey(args.FullPath))
		{
			_batchFiles.Remove(args.FullPath);
		}

		_batchProgress = args.BatchProgress;

		// get virtual file item
		var item = GetVirtualFileItem(args);
		if (item != null)
		{
			item.IsUploading = false;
		}

		await UploadCompleted.InvokeAsync(args).ConfigureAwait(true);
	}

	private async Task OnAllUploadsReady(UploadsReadyEventArgs args)
	{
		if (Tree!.SelectedNode?.Data != null)
		{
			// check for conflicts?
			var targetRootFolder = Tree!.SelectedNode.Data.Path;
			var moveCopyArgs = new MoveCopyArgs
			{
				ConflictResolution = ConflictResolution,
				TargetPath = targetRootFolder,
				Payload = [.. args.Files.Select(x => new FileExplorerItem { State = x.Key, Path = $"{x.Path?.TrimEnd('/')}/{x.Name?.TrimStart('/')}" })]
			};

			await GetUploadConflictsAsync(moveCopyArgs).ConfigureAwait(true);

			if (moveCopyArgs.Conflicts.Count != 0)
			{
				var result = await PromptUserForConflictResolution([.. moveCopyArgs.Conflicts.Select(x => x.Path)], true, false).ConfigureAwait(true);
				if (result == ConflictResolutions.Cancel)
				{
					args.Cancel = true;
				}
				else if (result == ConflictResolutions.Overwrite)
				{
					args.Overwrite = true;
				}
				else if (result == ConflictResolutions.Skip)
				{
					// remove files from queue before proceeding
					var ids = moveCopyArgs.Conflicts.Select(x => x.State).ToArray();
					args.FilesToSkip = [.. args.Files.Where(x => ids.Contains(x.Key))];
				}
			}
		}
	}

	private async Task OnAllUploadsStarted(int fileCount)
	{
		_batchCount = fileCount;
		_batchProgress = 0;

		// show progress dialog
		if (ShowUploadProgressDialog && fileCount > UploadProgressDialogThreshold)
		{
			await Task.WhenAll(UploadDialog!.HideAsync(), ProgressDialog!.ShowAsync()).ConfigureAwait(true);
		}
	}

	private void OnAllUploadsProgress(DropZoneAllProgressEventArgs args)
	{
		_batchTotalBytes = args.TotalBytes;
		_batchTotalBytesSent = args.TotalBytesSent;
	}

	private async Task OnAllUploadsComplete()
	{
		// close progress dialog
		await ProgressDialog!.HideAsync().ConfigureAwait(true);

		// expire conflict caches
		_conflictCache.Clear();
		_conflicts.Clear();
		await RefreshTreeAsync().ConfigureAwait(true);
		await RefreshTableAsync().ConfigureAwait(true);
	}

	private FileExplorerItem? GetVirtualFileItem(DropZoneUploadEventArgs args)
	{
		// add virtual file item
		if (Table != null)
		{
			if (args.Path == FolderPath) // in target folder so add file
			{
				var item = Table.ItemsToDisplay.Find(x => x.Name == args.Name);
				if (item is null)
				{
					item = new FileExplorerItem
					{
						Name = args.Name,
						Path = $"{args.Path.TrimEnd('/')}/{args.Name}",
						DateCreated = DateTimeOffset.Now,
						DateModified = DateTimeOffset.Now,
						EntryType = FileExplorerItemType.File,
						FileSize = args.Size,
						IsUploading = true
					};
					Table.ItemsToDisplay.Add(item);
				}

				return item;
			}
			else if (args.Path.StartsWith(FolderPath.TrimEnd('/') + "/", StringComparison.Ordinal)) // in higher folder
			{
				var relativePath = args.Path[FolderPath.Length..].TrimStart('/');
				var idx = relativePath.IndexOf('/');
				var subFolder = idx == -1 ? relativePath : relativePath[..idx];
				var item = Table.ItemsToDisplay.Find(x => x.Name == subFolder);
				if (item is null)
				{
					item = new FileExplorerItem
					{
						Name = subFolder,
						Path = $"{FolderPath.TrimEnd('/')}/{subFolder}",
						DateCreated = DateTimeOffset.Now,
						DateModified = DateTimeOffset.Now,
						EntryType = FileExplorerItemType.Directory,
						IsUploading = true
					};
					Table.ItemsToDisplay.Add(item);
				}

				return item;
			}
		}

		return null;
	}

	/// <summary>
	/// Populates the move copy arguments with conflicting items.
	/// </summary>
	private async Task GetUploadConflictsAsync(MoveCopyArgs args)
	{
		var conflicts = new List<FileExplorerItem>();
		// group files by parent directory
		var folders = args.Payload.GroupBy(x => x.ParentPath).ToArray();
		if (folders != null)
		{
			foreach (var folder in folders)
			{
				var folderPath = folder.Key;
				CachedResult<Task<DataResponse<FileExplorerItem>>>? cachedTask = null;
				lock (_conflictCache)
				{
					if (_conflictCache.TryGetValue(folderPath, out CachedResult<Task<DataResponse<FileExplorerItem>>>? value) && value.HasExpired)
					{
						_conflictCache.Remove(folderPath);
					}

					if (_conflictCache.TryGetValue(folderPath, out CachedResult<Task<DataResponse<FileExplorerItem>>>? value2))
					{
						cachedTask = value2;
					}
					else
					{
						var task = DataProvider.GetDataAsync(new DataRequest<FileExplorerItem>() { SearchText = folderPath }, default);
						cachedTask = new CachedResult<Task<DataResponse<FileExplorerItem>>>(folderPath, task)
						{
							Expiry = DateTimeOffset.UtcNow.AddSeconds(30)
						};
						_conflictCache.Add(folderPath, cachedTask);
					}
				}

				if (cachedTask != null)
				{
					// wait for cache to load
					var result = await cachedTask.Result.ConfigureAwait(true);
					var names = folder.Select(x => FileExplorerItem.GetNameFromPath(x.Path)).ToArray();
					args.TargetItems = [.. result.Items];
					foreach (var folderItem in folder)
					{
						var match = args.TargetItems.FirstOrDefault(x => FileExplorerItem.GetNameFromPath(x.Path) == FileExplorerItem.GetNameFromPath(folderItem.Path));
						if (match != null)
						{
							conflicts.Add(folderItem);
						}
					}
				}
			}
		}

		args.Conflicts = [.. conflicts.OrderBy(x => x.Path)];
	}

	private async Task OnHideUploadDialog(string _)
	{
		if (UploadDialog != null)
		{
			await UploadDialog.HideAsync().ConfigureAwait(true);
		}
	}

	private async Task OnClearUploadFiles()
	{
		if (_commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("removeClass", "pdfe-drop-zone-1", "dz-started").ConfigureAwait(true);
		}

		var tasks = new List<Task>
		{
			_dropZone1.ClearAsync(),
			_dropZone2.ClearAsync()
		};

		await Task.WhenAll(tasks).ConfigureAwait(true);
	}

	private async Task OnCancelUploadFiles()
	{
		BlockOverlayService.Show();
		var tasks = new List<Task>
		{
			_dropZone1.CancelAsync(),
			_dropZone2.CancelAsync()
		};
		await Task.WhenAll(tasks).ConfigureAwait(true);
	}

	private async Task OnClickToBrowseFilesAsync(string elementId)
	{
		if (_commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("clickClosest", elementId, ".pddropzone").ConfigureAwait(true);
		}
	}
}
