using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.PreviewProviders;
using System.Text;

namespace PanoramicData.Blazor.Test.PreviewProviders;

/// <summary>Tests for <see cref="DefaultPreviewProvider"/>.</summary>
public class DefaultPreviewProviderTests
{
	private static readonly DateTimeOffset _created = new(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
	private static readonly DateTimeOffset _modified = new(2024, 6, 7, 8, 9, 10, TimeSpan.Zero);

	private static FileExplorerItem File(string path, long size = 2048) => new()
	{
		Path = path,
		Name = FileExplorerItem.GetNameFromPath(path),
		EntryType = FileExplorerItemType.File,
		FileSize = size,
		DateCreated = _created,
		DateModified = _modified
	};

	private static FileExplorerItem Folder(string path) => new()
	{
		Path = path,
		Name = FileExplorerItem.GetNameFromPath(path),
		EntryType = FileExplorerItemType.Directory,
		DateCreated = _created,
		DateModified = _modified
	};

	/// <summary>No item, or the parent folder entry, has no preview.</summary>
	[Fact]
	public async Task GetPreviewInfoAsync_NothingToPreview()
	{
		var provider = new DefaultPreviewProvider();

		foreach (var item in new[] { null, Folder("/a/..") })
		{
			var info = await provider.GetPreviewInfoAsync(item);

			info.HtmlContent.Value.Should().Contain("No Preview");
			info.CssClass.Should().Be("basic");
		}
	}

	/// <summary>A folder is previewed with its name, type and dates.</summary>
	[Fact]
	public async Task GetPreviewInfoAsync_Folder_ShowsDetails()
	{
		var info = await new DefaultPreviewProvider().GetPreviewInfoAsync(Folder("/docs/Reports"));

		info.CssClass.Should().Be("basic");
		info.HtmlContent.Value.Should().StartWith("<div class=\"stacked\">")
			.And.Contain(">Reports</span>")
			.And.Contain(">Folder</span>")
			.And.Contain("Created: 02/01/24 03:04:05")
			.And.Contain("Modified: 07/06/24 08:09:10");
	}

	/// <summary>A file that cannot be downloaded for preview shows its name, type, size and dates.</summary>
	[Fact]
	public async Task GetPreviewInfoAsync_OtherFile_ShowsDetails()
	{
		var provider = new DefaultPreviewProvider { DateTimeFormat = "yyyy-MM-dd" };

		var info = await provider.GetPreviewInfoAsync(File("/docs/report.pdf"));

		info.HtmlContent.Value.Should().Contain(">report</span>")
			.And.Contain(">PDF File</span>")
			.And.Contain("title=\"2,048 bytes\">2 KB</span>")
			.And.Contain("Created: 2024-01-02")
			.And.Contain("Modified: 2024-06-07");
	}

	/// <summary>The default provider downloads nothing, so even a previewable file falls back to its details.</summary>
	[Fact]
	public async Task GetPreviewInfoAsync_DefaultDownload_FallsBackToDetails()
	{
		var info = await new DefaultPreviewProvider().GetPreviewInfoAsync(File("/notes.md"));

		info.CssClass.Should().Be("basic");
		info.HtmlContent.Value.Should().Contain(">MD File</span>");
	}

	/// <summary>Downloaded HTML, markdown and text are previewed as content.</summary>
	[Theory]
	[InlineData("/page.html", "<p>Hi</p>", "html", "<p>Hi</p>")]
	[InlineData("/page.htm", "<p>Hi</p>", "html", "<p>Hi</p>")]
	[InlineData("/notes.md", "# Title", "md", "<h1>Title</h1>")]
	[InlineData("/notes.txt", "plain text", "txt", "plain text")]
	public async Task GetPreviewInfoAsync_DownloadedContent_IsPreviewed(string path, string content, string cssClass, string expected)
	{
		var info = await new ContentProvider(content).GetPreviewInfoAsync(File(path));

		info.CssClass.Should().Be(cssClass);
		info.HtmlContent.Value.Should().Contain(expected);
	}

	/// <summary>An internet shortcut is previewed as the URL it points to.</summary>
	[Fact]
	public async Task GetPreviewInfoAsync_UrlShortcut_ExtractsUrl()
	{
		var info = await new ContentProvider("[InternetShortcut]\nURL=https://example.com/page\n").GetPreviewInfoAsync(File("/link.url"));

		info.CssClass.Should().Be("url");
		info.Url.Should().Be("https://example.com/page");
	}

	/// <summary>An internet shortcut with no URL line falls back to the file details.</summary>
	[Fact]
	public async Task GetPreviewInfoAsync_UrlShortcutWithoutUrl_FallsBack()
	{
		var info = await new ContentProvider("[InternetShortcut]").GetPreviewInfoAsync(File("/link.url"));

		info.CssClass.Should().Be("basic");
		info.Url.Should().BeEmpty();
	}

	/// <summary>The basic preview can include a loading spinner, and copes with no item at all.</summary>
	[Fact]
	public async Task GetBasicPreviewInfoAsync_WithSpinner()
	{
		var provider = new DefaultPreviewProvider();

		var info = await provider.GetBasicPreviewInfoAsync(null, true);

		info.HtmlContent.Value.Should().Be($"<div class=\"stacked\">{provider.GetSpinnerHtml()}</div>");
		provider.GetSpinnerHtml().Should().Contain("fa-spinner");
	}

	/// <summary>The spinner timings have their documented defaults.</summary>
	[Fact]
	public void SpinnerTimings_HaveDefaults()
	{
		var provider = new DefaultPreviewProvider();

		provider.SpinnerTriggerMs.Should().Be(500);
		provider.SpinnerMinDisplayMs.Should().Be(1000);
		provider.DateTimeFormat.Should().Be("dd/MM/yy HH:mm:ss");
	}

	private sealed class ContentProvider(string content) : DefaultPreviewProvider
	{
		protected override Task<byte[]> DownloadContentAsync(FileExplorerItem item) => Task.FromResult(Encoding.UTF8.GetBytes(content));
	}
}
