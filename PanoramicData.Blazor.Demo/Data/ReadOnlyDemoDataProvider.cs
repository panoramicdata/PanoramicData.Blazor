namespace PanoramicData.Blazor.Demo.Data;

/// <summary>
/// Wraps the standard test provider and marks every other file as read-only for demo purposes.
/// </summary>
internal sealed class ReadOnlyDemoDataProvider : IDataProviderService<FileExplorerItem>
{
	private readonly TestFileSystemDataProvider _inner = new();

	public async Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
	{
		var response = await _inner.GetDataAsync(request, cancellationToken).ConfigureAwait(false);
		var items = response.Items.ToList();
		foreach (var item in items.Where((item, index) => index % 2 == 0 && item.EntryType == FileExplorerItemType.File))
		{
			item.IsReadOnly = true;
		}

		return new DataResponse<FileExplorerItem>(items, response.TotalCount);
	}

	public Task<OperationResponse> CreateAsync(FileExplorerItem item, CancellationToken cancellationToken) => _inner.CreateAsync(item, cancellationToken);

	public Task<OperationResponse> DeleteAsync(FileExplorerItem item, CancellationToken cancellationToken) => _inner.DeleteAsync(item, cancellationToken);

	public Task<OperationResponse> UpdateAsync(FileExplorerItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken) => _inner.UpdateAsync(item, delta, cancellationToken);
}
