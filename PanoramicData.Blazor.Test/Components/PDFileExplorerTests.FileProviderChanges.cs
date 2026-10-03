using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The changes the in-memory file system provider of the <see cref="PDFileExplorer"/> tests makes and records,
/// and the failures a test can ask it for.
/// </summary>
public partial class PDFileExplorerTests
{
	/// <summary>The creates, updates and deletes of the in-memory file system.</summary>
	private sealed partial class FileProvider
	{
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
	}
}
