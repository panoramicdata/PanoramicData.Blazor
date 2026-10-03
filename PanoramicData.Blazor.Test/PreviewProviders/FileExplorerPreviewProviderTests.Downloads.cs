using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.PreviewProviders;
using System.Net;
using System.Net.Http;
using System.Text;

namespace PanoramicData.Blazor.Test.PreviewProviders;

/// <summary>
/// Download tests for <see cref="FileExplorerPreviewProvider"/>: download addresses, HTTP clients and the recording handler they use.
/// </summary>
public partial class FileExplorerPreviewProviderTests
{
	/// <summary>
	/// With the panel shown, a "type:name:url" download string is downloaded from the URL after the type and name,
	/// and the content is rendered.
	/// </summary>
	[Fact]
	public async Task VisiblePreviewPanel_DownloadsFromUrlAfterTypeAndName()
	{
		var handler = new RecordingHandler("# Notes");
		var explorer = RenderExplorer(FilePreviewModes.On, item => $"text/markdown:{item.Name}:https://files.example:8443/{item.Name}");
		var provider = new FileExplorerPreviewProvider(new HttpClient(handler)) { FileExplorer = explorer };

		var info = await provider.GetPreviewInfoAsync(Item("/notes.md"));

		handler.Requests.Should().Equal(new Uri("https://files.example:8443/notes.md"));
		info.CssClass.Should().Be("md");
		info.HtmlContent.Value.Should().Contain("Notes</h1>");
	}

	/// <summary>A plain URL is downloaded as given, whatever colons it contains (#174).</summary>
	/// <param name="url">The download URL the explorer supplies.</param>
	[Theory]
	[InlineData("https://files.example/notes.txt")]
	[InlineData("http://files.example:8080/notes.txt")]
	[InlineData("HTTPS://files.example/a:b/notes.txt")]
	public async Task VisiblePreviewPanel_PlainUrl_IsDownloadedUnchanged(string url)
	{
		var handler = new RecordingHandler("hello");
		var explorer = RenderExplorer(FilePreviewModes.On, _ => url);
		var provider = new FileExplorerPreviewProvider { FileExplorer = explorer, HttpClient = new HttpClient(handler) };

		var info = await provider.GetPreviewInfoAsync(Item("/notes.txt"));

		handler.Requests.Should().Equal(new Uri(url));
		info.CssClass.Should().Be("txt");
		info.HtmlContent.Value.Should().Be("hello");
	}

	/// <summary>A download string with no colon is an address relative to the client's base address.</summary>
	[Fact]
	public async Task VisiblePreviewPanel_RelativeAddress_IsDownloadedFromTheBaseAddress()
	{
		var handler = new RecordingHandler("hello");
		var explorer = RenderExplorer(FilePreviewModes.On, item => $"files/{item.Name}");
		var client = new HttpClient(handler) { BaseAddress = new Uri("https://files.example/") };
		var provider = new FileExplorerPreviewProvider(client) { FileExplorer = explorer };

		var info = await provider.GetPreviewInfoAsync(Item("/notes.txt"));

		handler.Requests.Should().Equal(new Uri("https://files.example/files/notes.txt"));
		info.CssClass.Should().Be("txt");
	}

	/// <summary>
	/// With no download URL (the explorer's default) nothing is downloaded and the basic details are shown,
	/// rather than a request with no address being sent (#174).
	/// </summary>
	/// <param name="url">The download URL the explorer supplies.</param>
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public async Task VisiblePreviewPanel_NoDownloadUrl_ShowsBasicDetails(string? url)
	{
		var handler = new RecordingHandler("hello");
		var explorer = RenderExplorer(FilePreviewModes.On, _ => url);
		var provider = new FileExplorerPreviewProvider(new HttpClient(handler)) { FileExplorer = explorer };

		var info = await provider.GetPreviewInfoAsync(Item("/notes.md"));

		handler.Requests.Should().BeEmpty();
		info.CssClass.Should().Be("basic");
		info.HtmlContent.Value.Should().Contain(">MD File</span>");
	}

	/// <summary>
	/// Without a supplied client the provider uses its shared one. A URL whose scheme HttpClient refuses shows
	/// which address was used without any network access: the refusal names the scheme of the URL after the
	/// second colon.
	/// </summary>
	[Fact]
	public async Task NoSuppliedClient_UsesSharedClient()
	{
		var explorer = RenderExplorer(FilePreviewModes.On, item => $"text/markdown:{item.Name}:ftp://files.example/{item.Name}");
		var provider = new FileExplorerPreviewProvider { FileExplorer = explorer };

		var act = () => provider.GetPreviewInfoAsync(Item("/notes.md"));

		provider.HttpClient.Should().BeNull();
		(await act.Should().ThrowAsync<NotSupportedException>()).Which.Message.Should().Contain("'ftp'");
	}

	/// <summary>The constructor refuses a null client.</summary>
	[Fact]
	public void Constructor_NullClient_Throws()
	{
		var act = () => new FileExplorerPreviewProvider(null!);

		act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
	}

	/// <summary>Outside the browser the shared client recycles its pooled connections, so it sees DNS changes.</summary>
	[Fact]
	public void CreateSharedHttpClient_OutsideTheBrowser_RecyclesPooledConnections()
	{
		using var client = FileExplorerPreviewProvider.CreateSharedHttpClient(false);

		GetHandler(client).Should().BeOfType<SocketsHttpHandler>()
			.Which.PooledConnectionLifetime.Should().Be(TimeSpan.FromMinutes(5));
	}

	/// <summary>In the browser the shared client keeps the platform's default handler.</summary>
	[Fact]
	public void CreateSharedHttpClient_InTheBrowser_UsesTheDefaultHandler()
	{
		using var client = FileExplorerPreviewProvider.CreateSharedHttpClient(true);

		GetHandler(client).Should().NotBeOfType<SocketsHttpHandler>();
	}

	/// <summary>Reads the handler a client sends through; <see cref="HttpMessageInvoker"/> does not expose it.</summary>
	private static HttpMessageHandler GetHandler(HttpClient client)
		=> (HttpMessageHandler)typeof(HttpMessageInvoker)
			.GetField("_handler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
			.GetValue(client)!;

	/// <summary>A message handler that records each request address and answers it with fixed UTF-8 content.</summary>
	/// <param name="content">The content every response carries.</param>
	private sealed class RecordingHandler(string content) : HttpMessageHandler
	{
		public List<Uri?> Requests { get; } = [];

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Requests.Add(request.RequestUri);
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8) });
		}
	}
}
