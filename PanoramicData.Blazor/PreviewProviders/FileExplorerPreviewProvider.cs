using Humanizer;
using System.Net.Http;

namespace PanoramicData.Blazor.PreviewProviders;

/// <summary>
/// A <see cref="DefaultPreviewProvider"/> that integrates with a <see cref="PDFileExplorer"/> to download
/// file content and generate richer detail panels including file icons, names, type, size, and timestamps.
/// </summary>
public class FileExplorerPreviewProvider : DefaultPreviewProvider
{
	private static readonly Lazy<HttpClient> _sharedHttpClient = new(CreateSharedHttpClient);

	/// <summary>
	/// Initializes a new instance of the <see cref="FileExplorerPreviewProvider"/> class that downloads content
	/// with a client shared by every provider that is not given one.
	/// </summary>
	public FileExplorerPreviewProvider()
	{
		// HttpClient stays null, so downloads use the shared client
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FileExplorerPreviewProvider"/> class that downloads content
	/// with the given client.
	/// </summary>
	/// <param name="httpClient">The client used to download file content, for example one from an <c>IHttpClientFactory</c>.</param>
	/// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is null.</exception>
	public FileExplorerPreviewProvider(HttpClient httpClient)
	{
		HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	/// <summary>Gets or sets the <see cref="PDFileExplorer"/> instance used to obtain download URLs and format settings.</summary>
	public PDFileExplorer? FileExplorer { get; set; }

	/// <summary>
	/// Gets or sets the client used to download file content for previews. When null, a single client shared by
	/// all providers is used, with the platform's default handler and proxy settings. Supply a client (for
	/// example one from an <c>IHttpClientFactory</c>) to control the handler, proxy, base address, headers or
	/// credentials. The provider never disposes a supplied client.
	/// </summary>
	public HttpClient? HttpClient { get; set; }

	/// <inheritdoc />
	protected override async Task<byte[]> DownloadContentAsync(FileExplorerItem item)
	{
		if (FileExplorer == null)
		{
			return await base.DownloadContentAsync(item);
		}

		// skip if preview panel is not shown
		if (!FileExplorer.PreviewPanelVisible)
		{
			return [];
		}

		// no address, so nothing to download: the basic details are shown instead
		var url = GetDownloadAddress(FileExplorer.DownloadUrlFunc(item));
		if (url is null)
		{
			return [];
		}

		var httpClient = HttpClient ?? _sharedHttpClient.Value;
		return await httpClient.GetByteArrayAsync(url);
	}

	/// <summary>
	/// Gets the address to download from, given the explorer's download string. An http or https URL is used as
	/// it is. Otherwise the string is taken to be in the drag-and-drop "type:name:url" form, and the address is
	/// what follows the second colon, as it always has been.
	/// </summary>
	/// <param name="downloadUrl">The explorer's download string for the item.</param>
	/// <returns>The address to download from, or null when there is none.</returns>
	private static string? GetDownloadAddress(string? downloadUrl)
	{
		if (string.IsNullOrWhiteSpace(downloadUrl))
		{
			return null;
		}

		if (downloadUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| downloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return downloadUrl;
		}

		if (!downloadUrl.Contains(':'))
		{
			return downloadUrl;
		}

		var index = downloadUrl
			.Select((c, i) => new { Character = c, Index = i })
			.Where(x => x.Character == ':')
			.Select(x => x.Index)
			.ElementAtOrDefault(1);
		return downloadUrl[++index..];
	}

	/// <summary>
	/// Creates the client shared by providers that are not given one. Outside the browser its pooled connections
	/// are recycled periodically so that a long-lived client still sees DNS changes.
	/// </summary>
	private static HttpClient CreateSharedHttpClient()
		=> OperatingSystem.IsBrowser() ? new HttpClient() : new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });

	/// <inheritdoc />
	protected override List<string> GetFileDetails(FileExplorerItem item)
	{
		if (FileExplorer is null)
		{
			return base.GetFileDetails(item);
		}
		else
		{
			var dc = item.DateCreated?.ToLocalTime().ToString(FileExplorer.DateFormat, CultureInfo.InvariantCulture);
			var dm = item.DateModified?.ToLocalTime().ToString(FileExplorer.DateFormat, CultureInfo.InvariantCulture);
			var details = new List<string>
			{
				$"<i class=\"fa-4x {FileExplorer.GetIconCssClass(item)}\"></i>"
			};

			if (item.EntryType == FileExplorerItemType.Directory)
			{
				details.Add($"<span class=\"h1 user-select-none\">{item.Name}</span>");
				details.Add("<span class=\"h4 user-select-none\">Folder</span>");
			}
			else
			{
				details.Add($"<span class=\"h1 user-select-none\">{Path.GetFileNameWithoutExtension(item.Name)}</span>");
				details.Add($"<span class=\"h4 user-select-none\">{Path.GetExtension(item.Name)[1..].ToUpperInvariant()} File</span>");
				details.Add($"<span class=\"user-select-none\" title=\"{item.FileSize:N0} bytes\">{item.FileSize.Bytes().Humanize(CultureInfo.InvariantCulture)}</span>");
			}

			details.Add($"<span title=\"{item.DateCreated}\" class=\"text-small text-muted user-select-none\">Created: {dc}</span>");
			details.Add($"<span title=\"{item.DateModified}\" class=\"text-small text-muted user-select-none\">Modified: {dm}</span>");

			return details;
		}
	}
}
