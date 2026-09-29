using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The in-memory file system provider shared by the <see cref="PDFileExplorer"/> tests.
/// </summary>
public partial class PDFileExplorerTests
{
	/// <summary>An in-memory file system that records what it is asked to do.</summary>
	private sealed class FileProvider : DataProviderBase<FileExplorerItem>
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

		public List<(string Path, IDictionary<string, object?> Delta)> Updates { get; } = [];

		public List<string> Deletes { get; } = [];

		public List<string> Creates { get; } = [];

		private bool _failUpdates;
		private bool _failCreates;
		private string? _throwOnDeletePath;

		/// <summary>Makes every later update, which is how moves, copies and renames arrive, refuse.</summary>
		public void RefuseUpdates() => _failUpdates = true;

		/// <summary>Makes every later create refuse.</summary>
		public void RefuseCreates() => _failCreates = true;

		/// <summary>Makes a later delete of exactly <paramref name="path"/> throw.</summary>
		/// <param name="path">The path whose deletion should fail.</param>
		public void ThrowOnDelete(string path) => _throwOnDeletePath = path;

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

		public override Task<OperationResponse> CreateAsync(FileExplorerItem item, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Creates.Add(item.Path);
			if (!_failCreates)
			{
				Items.Add(Copy(item));
			}

			return Task.FromResult(new OperationResponse { Success = !_failCreates });
		}

		public override Task<OperationResponse> UpdateAsync(FileExplorerItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Updates.Add((item.Path, delta));
			if (_failUpdates)
			{
				return Task.FromResult(new OperationResponse { ErrorMessage = "refused" });
			}

			var newPath = (string)delta["Path"]!;
			var isCopy = delta.TryGetValue("Copy", out var copy) && copy is true;
			foreach (var existing in Items.Where(x => x.Path == item.Path || x.Path.StartsWith(item.Path + "/", StringComparison.Ordinal)).ToList())
			{
				var target = isCopy ? Copy(existing) : existing;
				target.Path = newPath + existing.Path[item.Path.Length..];
				target.Name = FileExplorerItem.GetNameFromPath(target.Path);
				if (isCopy)
				{
					Items.Add(target);
				}
			}

			return Task.FromResult(new OperationResponse { Success = true });
		}

		public override Task<OperationResponse> DeleteAsync(FileExplorerItem item, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (item.Path == _throwOnDeletePath)
			{
				throw new InvalidOperationException($"cannot delete {item.Path}");
			}

			Deletes.Add(item.Path);
			Items.RemoveAll(x => x.Path == item.Path || x.Path.StartsWith(item.Path + "/", StringComparison.Ordinal));
			return Task.FromResult(new OperationResponse { Success = true });
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
