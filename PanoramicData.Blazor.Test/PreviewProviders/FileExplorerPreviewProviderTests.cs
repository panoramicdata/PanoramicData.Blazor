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
	/// <remarks>
	/// The provider creates its own <see cref="System.Net.Http.HttpClient"/>, so there is no handler to replace
	/// and a real socket is needed. The server holds its port from bind to disposal, is addressed by IP rather
	/// than "localhost", and answers on a dedicated thread, and the call is bounded so that a stall fails in
	/// seconds rather than at the client's 100 second default timeout.
	/// </remarks>
	[Fact]
	public async Task VisiblePreviewPanel_DownloadsAndPreviewsContent()
	{
		using var server = new ContentServer("# Downloaded");
		var explorer = RenderExplorer(FilePreviewModes.On, item => $"text/markdown:{item.Name}:{server.Url}");
		var provider = new FileExplorerPreviewProvider { FileExplorer = explorer };

		var info = await provider.GetPreviewInfoAsync(Item("/notes.md"))
			.WaitAsync(TimeSpan.FromSeconds(15), Xunit.TestContext.Current.CancellationToken);

		info.CssClass.Should().Be("md");
		info.HtmlContent.Value.Should().Contain("<h1>Downloaded</h1>");
		server.RequestCount.Should().Be(1);
	}

	/// <summary>
	/// A minimal HTTP server on a loopback port that it owns exclusively for its whole lifetime, answering
	/// every request with one fixed body on a dedicated thread, so that neither port reuse nor thread pool
	/// starvation can leave a request unanswered.
	/// </summary>
	private sealed class ContentServer : IDisposable
	{
		private readonly TcpListener _listener = new(IPAddress.Loopback, 0) { ExclusiveAddressUse = true };
		private readonly byte[] _response;
		private readonly Thread _thread;
		private int _requestCount;

		public ContentServer(string content)
		{
			var body = Encoding.UTF8.GetBytes(content);
			var header = Encoding.ASCII.GetBytes(
				$"HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
			_response = [.. header, .. body];
			_listener.Start();
			Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/content";
			_thread = new Thread(Serve) { IsBackground = true, Name = nameof(ContentServer) };
			_thread.Start();
		}

		public string Url { get; }

		public int RequestCount => Volatile.Read(ref _requestCount);

		public void Dispose()
		{
			_listener.Stop();
			_thread.Join(TimeSpan.FromSeconds(5));
		}

		private void Serve()
		{
			try
			{
				while (true)
				{
					using var client = _listener.AcceptTcpClient();
					client.ReceiveTimeout = 5000;
					client.SendTimeout = 5000;
					using var stream = client.GetStream();
					ReadRequestHeaders(stream);
					stream.Write(_response);
					Interlocked.Increment(ref _requestCount);
				}
			}
			catch (SocketException)
			{
				// The listener was stopped.
			}
			catch (IOException)
			{
				// The client went away; there is nothing further to serve.
			}
			catch (ObjectDisposedException)
			{
				// The listener was disposed.
			}
		}

		private static void ReadRequestHeaders(NetworkStream stream)
		{
			// Read up to the blank line that ends the request headers; a GET has no body to consume.
			byte[] terminator = [13, 10, 13, 10];
			var matched = 0;
			while (matched < terminator.Length)
			{
				var next = stream.ReadByte();
				if (next < 0)
				{
					return;
				}

				matched = next == terminator[matched] ? matched + 1 : (next == 13 ? 1 : 0);
			}
		}
	}
}
