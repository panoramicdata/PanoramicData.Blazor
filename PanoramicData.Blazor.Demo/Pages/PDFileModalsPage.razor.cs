namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFileModalsPage
{
	protected PDFileModal FileModal { get; set; } = null!;
	protected PDFileModal CustomButtonModal { get; set; } = null!;
	protected PDFileModal ExcludedPathsModal { get; set; } = null!;
	protected PDFileModal ReadOnlyModal { get; set; } = null!;
	protected PDFileModal LargeModal { get; set; } = null!;

	private string _openResult = string.Empty;
	private string _saveAsResult = string.Empty;
	private string _customResult = string.Empty;
	private string _excludedResult = string.Empty;
	private string _readOnlyResult = string.Empty;
	private string _largeResult = string.Empty;

	private readonly IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
	private readonly IDataProviderService<FileExplorerItem> _readOnlyDataProvider = new ReadOnlyDemoDataProvider();
	private bool _showOpen;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	// Virtual folders to prioritize at the top of the tree
	private readonly string[] _virtualFolders = ["/Library", "/Users"];

	private static string GetIconCssClass(FileExplorerItem item)
	{
		if (item.EntryType == FileExplorerItemType.Directory && item.Name != "..")
		{
			if (item.Path == "/Library")
			{
				return "fas fa-book";
			}

			if (item.Path == "/Users")
			{
				return "fas fa-users";
			}

			if (item.Path == "/")
			{
				return "fas fa-server";
			}

			if (item.ParentPath == "/")
			{
				return "fas fa-hdd";
			}
		}

		return TestFileSystemDataProvider.GetIconClass(item);
	}

	private int OnTreeSort(FileExplorerItem item1, FileExplorerItem item2)
	{
		if (_virtualFolders.Contains(item1.Path) && !_virtualFolders.Contains(item2.Path))
		{
			return -1;
		}

		if (!_virtualFolders.Contains(item1.Path) && _virtualFolders.Contains(item2.Path))
		{
			return 1;
		}

		return item1.Name.CompareTo(item2.Name);
	}

	private void OnModalHidden(string result)
	{
		if (_showOpen)
		{
			_openResult = result;
		}
		else
		{
			_saveAsResult = result;
		}

		EventManager?.Add(new Event("ModalHidden", new EventArgument("Result", result)));
	}

	private async Task ShowFileOpenModalAndWaitResult()
	{
		_openResult = await FileModal.ShowOpenAndWaitResultAsync().ConfigureAwait(true);
		EventManager?.Add(new Event("OpenResult", new EventArgument("Path", _openResult)));
	}

	private async Task ShowFileOpenFilteredModalAndWaitResult()
	{
		_openResult = await FileModal.ShowOpenAndWaitResultAsync(false, "*.docx;*.xlsx").ConfigureAwait(true);
		EventManager?.Add(new Event("OpenResult (filtered)", new EventArgument("Path", _openResult)));
	}

	private async Task ShowFolderOpenModalAndWaitResult()
	{
		_openResult = await FileModal.ShowOpenAndWaitResultAsync(true).ConfigureAwait(true);
		EventManager?.Add(new Event("FolderResult", new EventArgument("Path", _openResult)));
	}

	private async Task ShowFileSaveAsModalAndWaitResult()
	{
		_saveAsResult = await FileModal.ShowSaveAsAndWaitResultAsync("NewFile.html").ConfigureAwait(true);
		EventManager?.Add(new Event("SaveAsResult", new EventArgument("Path", _saveAsResult)));
	}

	private async Task ShowFileOpenModal()
	{
		_showOpen = true;
		await FileModal.ShowOpenAsync().ConfigureAwait(true);
	}

	private async Task ShowFileSaveAsModal()
	{
		_showOpen = false;
		await FileModal.ShowSaveAsAsync(_openResult).ConfigureAwait(true);
	}

	private async Task ShowCustomOpenAndWait()
	{
		_customResult = await CustomButtonModal.ShowOpenAndWaitResultAsync().ConfigureAwait(true);
		EventManager?.Add(new Event("CustomOpen", new EventArgument("Path", _customResult)));
	}

	private async Task ShowCustomSaveAsAndWait()
	{
		_customResult = await CustomButtonModal.ShowSaveAsAndWaitResultAsync().ConfigureAwait(true);
		EventManager?.Add(new Event("CustomSaveAs", new EventArgument("Path", _customResult)));
	}

	private async Task ShowExcludedPathsOpenAndWait()
	{
		_excludedResult = await ExcludedPathsModal.ShowOpenAndWaitResultAsync().ConfigureAwait(true);
		EventManager?.Add(new Event("ExcludedOpen", new EventArgument("Path", _excludedResult)));
	}

	private async Task ShowReadOnlyOpenAndWait()
	{
		_readOnlyResult = await ReadOnlyModal.ShowOpenAndWaitResultAsync().ConfigureAwait(true);
		EventManager?.Add(new Event("ReadOnlyOpen", new EventArgument("Path", _readOnlyResult)));
	}

	private async Task ShowLargeOpenAndWait()
	{
		_largeResult = await LargeModal.ShowOpenAndWaitResultAsync().ConfigureAwait(true);
		EventManager?.Add(new Event("LargeOpen", new EventArgument("Path", _largeResult)));
	}
}

/// <summary>
/// Wraps the standard test provider and marks every other file as read-only for demo purposes.
/// </summary>
file sealed class ReadOnlyDemoDataProvider : IDataProviderService<FileExplorerItem>
{
	private readonly TestFileSystemDataProvider _inner = new();

	public async Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
	{
		var response = await _inner.GetDataAsync(request, cancellationToken).ConfigureAwait(false);
		var items = response.Items.ToList();
		for (var i = 0; i < items.Count; i++)
		{
			if (items[i].EntryType == FileExplorerItemType.File && i % 2 == 0)
			{
				items[i].IsReadOnly = true;
			}
		}

		return new DataResponse<FileExplorerItem>(items, response.TotalCount);
	}

	public Task<OperationResponse> CreateAsync(FileExplorerItem item, CancellationToken cancellationToken) => _inner.CreateAsync(item, cancellationToken);

	public Task<OperationResponse> DeleteAsync(FileExplorerItem item, CancellationToken cancellationToken) => _inner.DeleteAsync(item, cancellationToken);

	public Task<OperationResponse> UpdateAsync(FileExplorerItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken) => _inner.UpdateAsync(item, delta, cancellationToken);
}
