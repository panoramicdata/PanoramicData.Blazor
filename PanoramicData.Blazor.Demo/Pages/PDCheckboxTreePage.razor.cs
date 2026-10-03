namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCheckboxTreePage
{
	private readonly IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
	private readonly IDataProviderService<FileExplorerItem> _folderProvider = new FoldersOnlyDataProvider(new TestFileSystemDataProvider());

	protected bool ShowLines { get; set; } = true;
	protected bool ShowRoot { get; set; } = true;

	private List<string> _checkedKeys = [];
	protected List<string> CheckedFolders { get; set; } = [];

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void OnCheckedKeysChanged(List<string> keys)
	{
		_checkedKeys = keys;
		EventManager?.Add(new Event("CheckedKeysChanged", new EventArgument("Count", keys.Count), new EventArgument("Keys", string.Join(", ", keys))));
	}

	private const string _folderIconCssClass = "fas fa-fw fa-folder text-warning me-1";

	private static string GetIconCssClass(FileExplorerItem item)
		=> item.EntryType == FileExplorerItemType.Directory
			? _folderIconCssClass
			: "far fa-fw fa-file text-secondary me-1";

	private static int OnSort(FileExplorerItem a, FileExplorerItem b)
	{
		// Folders before files, then by name
		var typeComparison = a.EntryType.CompareTo(b.EntryType);
		return typeComparison != 0
			? typeComparison
			: string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Checking a folder implies its whole subtree, so descendants of a checked folder are disabled.
	/// </summary>
	private bool IsUnderCheckedFolder(FileExplorerItem item)
		=> CheckedFolders.Exists(checkedPath =>
			item.Path != checkedPath &&
			item.Path.StartsWith(checkedPath.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Wraps a file system data provider, returning only its folders.
	/// </summary>
	private sealed class FoldersOnlyDataProvider(IDataProviderService<FileExplorerItem> inner) : IDataProviderService<FileExplorerItem>
	{
		public async Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
		{
			var response = await inner.GetDataAsync(request, cancellationToken).ConfigureAwait(false);
			var folders = (response.Items ?? []).Where(x => x.EntryType == FileExplorerItemType.Directory).ToList();
			return new DataResponse<FileExplorerItem>(folders, folders.Count);
		}

		public Task<OperationResponse> CreateAsync(FileExplorerItem item, CancellationToken cancellationToken)
			=> inner.CreateAsync(item, cancellationToken);

		public Task<OperationResponse> DeleteAsync(FileExplorerItem item, CancellationToken cancellationToken)
			=> inner.DeleteAsync(item, cancellationToken);

		public Task<OperationResponse> UpdateAsync(FileExplorerItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
			=> inner.UpdateAsync(item, delta, cancellationToken);
	}
}
