using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The in-memory folder tree used by the <see cref="PDFileModal"/> tests.
/// </summary>
public partial class PDFileModalTests
{
	/// <summary>An in-memory folder tree: a root holding one folder and one file, the folder holding one file.</summary>
	private sealed class FileProvider : DataProviderBase<FileExplorerItem>
	{
		private readonly List<FileExplorerItem> _items =
		[
			new() { Path = "/", Name = "", EntryType = FileExplorerItemType.Directory, HasSubFolders = true },
			new() { Path = "/Docs", Name = "Docs", EntryType = FileExplorerItemType.Directory, HasSubFolders = false },
			new() { Path = "/readme.txt", Name = "readme.txt", EntryType = FileExplorerItemType.File, FileSize = 10 },
			new() { Path = "/Docs/notes.md", Name = "notes.md", EntryType = FileExplorerItemType.File, FileSize = 20 }
		];

		public int Requests { get; private set; }

		public override Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Requests++;
			List<FileExplorerItem> items = request.SearchText switch
			{
				null => [.. _items],
				"" => [_items[0]],
				var parent => [.. _items.Where(i => i.Path != "/" && i.ParentPath == parent)]
			};
			return Task.FromResult(new DataResponse<FileExplorerItem>(items, items.Count));
		}
	}
}
