using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Web.Data
{
	public class FileSystemDataProvider : IDataProviderService<FileExplorerItem>
	{
		/// <summary>
		/// Gets or sets whether folders should be returned.
		/// </summary>
		public bool ShowFolders { get; set; } = true;

		/// <summary>
		/// Gets or sets whether files should be returned.
		/// </summary>
		public bool ShowFiles { get; set; } = true;

		/// <summary>
		/// Gets or sets whether system files and folders should be returned.
		/// </summary>
		public bool ShowSystem { get; set; }

		/// <summary>
		/// Gets or sets whether hidden files and folders should be returned.
		/// </summary>
		public bool ShowHidden { get; set; }

		public async Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
		{
			var items = new List<FileExplorerItem>();
			await Task.Run(() =>
			{
				if (string.IsNullOrWhiteSpace(request.SearchText))
				{
					var info = new DirectoryInfo("C:\\");
					items.Add(new FileExplorerItem { Path = "C:\\", EntryType = FileExplorerItemType.Directory, DateCreated = info.CreationTimeUtc, DateModified = info.LastWriteTimeUtc });
				}
				else
				{
					// request search if set will be full path of parent
					try
					{
						if (ShowFolders)
						{
							foreach (var folder in Directory.GetDirectories(request.SearchText))
							{
								var info = new DirectoryInfo(folder);
								var item = new FileExplorerItem
								{
									Path = folder,
									EntryType = FileExplorerItemType.Directory,
									DateCreated = info.CreationTimeUtc,
									DateModified = info.LastWriteTimeUtc,
									IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden),
									IsSystem = info.Attributes.HasFlag(FileAttributes.System),
									IsReadOnly = info.Attributes.HasFlag(FileAttributes.ReadOnly)
								};
								if ((ShowHidden || !item.IsHidden) && (ShowSystem || !item.IsSystem))
								{
									items.Add(item);
								}
							}
						}
						if (ShowFiles)
						{
							foreach (var file in Directory.GetFiles(request.SearchText))
							{
								var info = new FileInfo(file);
								var item = new FileExplorerItem
								{
									Path = file,
									EntryType = FileExplorerItemType.File,
									FileSize = info.Length,
									DateCreated = info.CreationTimeUtc,
									DateModified = info.LastWriteTimeUtc,
									IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden),
									IsSystem = info.Attributes.HasFlag(FileAttributes.System),
									IsReadOnly = info.Attributes.HasFlag(FileAttributes.ReadOnly)
								};
								if ((ShowHidden || !item.IsHidden) && (ShowSystem || !item.IsSystem))
								{
									items.Add(item);
								}
							}
						}
					}
					catch
					{
					}
				}


			}).ConfigureAwait(false);
			return new DataResponse<FileExplorerItem>(items, null);
		}
	}
}
