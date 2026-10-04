namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFileModalsPage
{
	protected PDFileModal FileModal { get; set; } = null!;
	protected PDFileModal CustomButtonModal { get; set; } = null!;
	protected PDFileModal ExcludedPathsModal { get; set; } = null!;
	protected PDFileModal ReadOnlyModal { get; set; } = null!;
	protected PDFileModal LargeModal { get; set; } = null!;

	// The path chosen in each example ("open", "saveAs", "custom", "excluded", "readOnly" and "large")
	private readonly Dictionary<string, string> _results = [];

	private readonly IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
	private readonly IDataProviderService<FileExplorerItem> _readOnlyDataProvider = new ReadOnlyDemoDataProvider();
	private bool _showOpen;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	// Virtual folders to prioritize at the top of the tree
	private readonly string[] _virtualFolders = ["/Library", "/Users"];

	// Icons of the virtual folders and the root
	private static readonly Dictionary<string, string> _folderIcons = new()
	{
		["/Library"] = "fas fa-book",
		["/Users"] = "fas fa-users",
		["/"] = "fas fa-server"
	};

	private static string GetIconCssClass(FileExplorerItem item)
	{
		if (item.EntryType != FileExplorerItemType.Directory || item.Name == "..")
		{
			return TestFileSystemDataProvider.GetIconClass(item);
		}

		if (_folderIcons.TryGetValue(item.Path, out var icon))
		{
			return icon;
		}

		// the root's sub-folders are drives
		return item.ParentPath == "/" ? "fas fa-hdd" : TestFileSystemDataProvider.GetIconClass(item);
	}

	private bool IsVirtualFolder(FileExplorerItem item) => _virtualFolders.Contains(item.Path);

	private int OnTreeSort(FileExplorerItem item1, FileExplorerItem item2)
	{
		// virtual folders first, then by name
		var virtualFirst = IsVirtualFolder(item2).CompareTo(IsVirtualFolder(item1));
		return virtualFirst != 0 ? virtualFirst : item1.Name.CompareTo(item2.Name);
	}

	private string GetResult(string example) => _results.GetValueOrDefault(example, string.Empty);

	private void LogEvent(string name, string argumentName, string value)
		=> EventManager?.Add(new Event(name, new EventArgument(argumentName, value)));

	private void OnModalHidden(string result)
	{
		_results[_showOpen ? "open" : "saveAs"] = result;
		LogEvent("ModalHidden", "Result", result);
	}

	/// <summary>
	/// Awaits the path chosen in a modal shown with ShowOpenAndWaitResultAsync or ShowSaveAsAndWaitResultAsync,
	/// then records it as the given example's result.
	/// </summary>
	private async Task WaitForResultAsync(string example, string eventName, Task<string> modalResult)
	{
		var path = await modalResult.ConfigureAwait(true);
		_results[example] = path;
		LogEvent(eventName, "Path", path);
	}

	private async Task ShowFileOpenModal()
	{
		_showOpen = true;
		await FileModal.ShowOpenAsync().ConfigureAwait(true);
	}

	private async Task ShowFileSaveAsModal()
	{
		_showOpen = false;
		await FileModal.ShowSaveAsAsync(GetResult("open")).ConfigureAwait(true);
	}
}
