using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.PreviewProviders;
using PanoramicData.Blazor.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PanoramicData.Blazor.Test.PreviewProviders;

/// <summary>Tests for <see cref="FileExplorerPreviewProvider"/>.</summary>
public class FileExplorerPreviewProviderTests : BunitContext
{
	private static readonly DateTimeOffset _modified = new(2024, 6, 7, 8, 9, 10, TimeSpan.Zero);

	/// <summary>Sets up the rendering context.</summary>
	public FileExplorerPreviewProviderTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private static FileExplorerItem Item(string path, FileExplorerItemType type = FileExplorerItemType.File) => new()
	{
		Path = path,
		Name = FileExplorerItem.GetNameFromPath(path),
		EntryType = type,
		FileSize = 10,
		DateModified = _modified
	};

	private PDFileExplorer RenderExplorer(FilePreviewModes mode, Func<FileExplorerItem, string?>? downloadUrl = null)
		=> Render<PDFileExplorer>(parameters => parameters
			.Add(p => p.DataProvider, new ListDataProviderService<FileExplorerItem>())
			.Add(p => p.PreviewPanel, mode)
			.Add(p => p.DateFormat, "yyyy-MM-dd")
			.Add(p => p.DownloadUrlFunc, downloadUrl ?? (_ => null))).Instance;

	/// <summary>Without a file explorer, the provider behaves exactly as the default provider.</summary>
	[Fact]
	public async Task NoExplorer_BehavesAsDefault()
	{
		var info = await new FileExplorerPreviewProvider().GetPreviewInfoAsync(Item("/notes.md"));

		info.CssClass.Should().Be("basic");
		info.HtmlContent.Value.Should().Contain(">MD File</span>").And.NotContain("fa-4x");
	}

	/// <summary>While the explorer's preview panel is hidden nothing is downloaded, and the details use the explorer's icon.</summary>
	[Fact]
	public async Task HiddenPreviewPanel_ShowsExplorerDetails()
	{
		var explorer = RenderExplorer(FilePreviewModes.OptionalOff);
		var provider = new FileExplorerPreviewProvider { FileExplorer = explorer };

		var info = await provider.GetPreviewInfoAsync(Item("/notes.md"));

		explorer.PreviewPanelVisible.Should().BeFalse();
		info.CssClass.Should().Be("basic");
		info.HtmlContent.Value.Should().Contain("<i class=\"fa-4x far fa-fw fa-file\"></i>")
			.And.Contain(">notes</span>")
			.And.Contain(">MD File</span>")
			.And.Contain("title=\"10 bytes\"")
			.And.Contain($"Modified: {_modified.ToLocalTime():yyyy-MM-dd}");
	}

	/// <summary>A folder's details use the folder icon and say it is a folder.</summary>
	[Fact]
	public async Task Folder_ShowsFolderDetails()
	{
		var provider = new FileExplorerPreviewProvider { FileExplorer = RenderExplorer(FilePreviewModes.OptionalOff) };

		var info = await provider.GetPreviewInfoAsync(Item("/docs/Reports", FileExplorerItemType.Directory));

		info.HtmlContent.Value.Should().Contain("fa fa-fw fa-folder").And.Contain(">Reports</span>").And.Contain(">Folder</span>");
	}

	/// <summary>
	/// With the panel shown, the content is downloaded from the URL at the end of the explorer's
	/// "type:name:url" download string, and previewed.
	/// </summary>
	[Fact]
	public async Task VisiblePreviewPanel_DownloadsAndPreviewsContent()
	{
		using var server = new ContentServer("# Downloaded");
		var explorer = RenderExplorer(FilePreviewModes.On, item => $"text/markdown:{item.Name}:{server.Url}");
		var provider = new FileExplorerPreviewProvider { FileExplorer = explorer };

		var info = await provider.GetPreviewInfoAsync(Item("/notes.md"));

		info.CssClass.Should().Be("md");
		info.HtmlContent.Value.Should().Contain("<h1>Downloaded</h1>");
	}

	/// <summary>Serves one fixed response on a free local port, on a background loop.</summary>
	private sealed class ContentServer : IDisposable
	{
		private readonly HttpListener _listener = new();

		public ContentServer(string content)
		{
			var port = FreePort();
			Url = $"http://localhost:{port}/content";
			_listener.Prefixes.Add($"http://localhost:{port}/");
			_listener.Start();
			var body = Encoding.UTF8.GetBytes(content);
			_ = Task.Run(() => ServeAsync(body));
		}

		public string Url { get; }

		public void Dispose() => _listener.Close();

		private static int FreePort()
		{
			var probe = new TcpListener(IPAddress.Loopback, 0);
			probe.Start();
			var port = ((IPEndPoint)probe.LocalEndpoint).Port;
			probe.Stop();
			return port;
		}

		private async Task ServeAsync(byte[] body)
		{
			try
			{
				while (_listener.IsListening)
				{
					var context = await _listener.GetContextAsync().ConfigureAwait(false);
					context.Response.ContentLength64 = body.Length;
					await context.Response.OutputStream.WriteAsync(body).ConfigureAwait(false);
					context.Response.Close();
				}
			}
			catch (HttpListenerException)
			{
				// The listener was closed.
			}
			catch (ObjectDisposedException)
			{
				// The listener was disposed.
			}
		}
	}
}
