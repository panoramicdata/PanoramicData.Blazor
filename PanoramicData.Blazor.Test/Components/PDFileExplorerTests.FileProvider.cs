using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The in-memory file system provider shared by the <see cref="PDFileExplorer"/> tests: its contents and how it
/// reads them.
/// </summary>
public partial class PDFileExplorerTests
{
	/// <summary>An in-memory file system that records what it is asked to do.</summary>
	private sealed partial class FileProvider : DataProviderBase<FileExplorerItem>
	{
		public FileProvider()
		{
			Items.AddRange(
			[
				Dir("/", hasSubFolders: true),
				Dir("/Docs", hasSubFolders: true),
				Dir("/Docs/Sub"),
				File("/Docs/a.docx", 500),
				File("/Docs/b.xlsx", 2048),
				Dir("/Media", readOnly: true),
				File("/Media/pic.png", 4096),
				Dir("/Empty")
			]);
		}

		public List<FileExplorerItem> Items { get; } = [];

		public List<string?> Requests { get; } = [];

		public static FileExplorerItem Dir(string path, bool hasSubFolders = false, bool readOnly = false) => new()
		{
			Path = path,
			Name = path == "/" ? "Root" : FileExplorerItem.GetNameFromPath(path),
			EntryType = FileExplorerItemType.Directory,
			HasSubFolders = hasSubFolders,
			IsReadOnly = readOnly
		};

		public static FileExplorerItem File(string path, long size) => new()
		{
			Path = path,
			Name = FileExplorerItem.GetNameFromPath(path),
			EntryType = FileExplorerItemType.File,
			FileSize = size,
			DateCreated = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
			DateModified = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero)
		};

		public FileExplorerItem Get(string path) => Items.Single(x => x.Path == path);

		public override Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Requests.Add(request.SearchText);
			List<FileExplorerItem> items = request.SearchText switch
			{
				null => [.. Items.Select(Copy)],
				"" => [Copy(Get("/"))],
				var parent => [.. Items.Where(x => x.Path != "/" && x.ParentPath == parent).Select(Copy)]
			};
			if (request.SortFieldExpression is not null)
			{
				var key = request.SortFieldExpression.Compile();
				items = request.SortDirection == SortDirection.Descending ? [.. items.OrderByDescending(key)] : [.. items.OrderBy(key)];
			}

			return Task.FromResult(new DataResponse<FileExplorerItem>(items, items.Count));
		}

		private static FileExplorerItem Copy(FileExplorerItem x) => new()
		{
			Path = x.Path,
			Name = x.Name,
			EntryType = x.EntryType,
			FileSize = x.FileSize,
			HasSubFolders = x.HasSubFolders,
			IsReadOnly = x.IsReadOnly,
			IsHidden = x.IsHidden,
			IsSystem = x.IsSystem,
			CanCopyMove = x.CanCopyMove,
			CanDelete = x.CanDelete,
			CanRename = x.CanRename,
			DateCreated = x.DateCreated,
			DateModified = x.DateModified
		};
	}
}
