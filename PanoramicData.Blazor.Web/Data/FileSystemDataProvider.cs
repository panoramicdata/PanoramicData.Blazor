using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Web.Data
{
	public class FileSystemDataProvider : IDataProviderService<FileExplorerItem>
	{
		public FileSystemDataProvider()
		{
		}

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
						foreach (var folder in Directory.GetDirectories(request.SearchText))
						{
							var info = new DirectoryInfo(folder);
							items.Add(new FileExplorerItem { Path = folder, EntryType = FileExplorerItemType.Directory, DateCreated = info.CreationTimeUtc, DateModified = info.LastWriteTimeUtc });
						}
						foreach (var file in Directory.GetFiles(request.SearchText))
						{
							var info = new FileInfo(file);
							items.Add(new FileExplorerItem { Path = file, EntryType = FileExplorerItemType.File, FileSize = info.Length, DateCreated = info.CreationTimeUtc, DateModified = info.LastWriteTimeUtc });
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
