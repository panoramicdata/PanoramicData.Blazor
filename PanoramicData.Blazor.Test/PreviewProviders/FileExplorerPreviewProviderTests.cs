using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.PreviewProviders;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.PreviewProviders;

/// <summary>Tests for <see cref="FileExplorerPreviewProvider"/>.</summary>
/// <remarks>
/// A successful download is deliberately not tested. The provider creates its own
/// <see cref="System.Net.Http.HttpClient"/>, so the only way to exercise it is a real socket and whatever proxy
/// resolution the machine applies, which a test cannot control; an earlier version with a local server stalled
/// intermittently in combined runs. The provider needs an injectable client or handler before that path can be
/// tested deterministically.
/// </remarks>
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
	/// With the panel shown, the download address is the URL at the end of the explorer's "type:name:url"
	/// download string. A URL whose scheme HttpClient refuses shows which address was used without any
	/// network access: the refusal names the scheme of the URL after the second colon.
	/// </summary>
	[Fact]
	public async Task VisiblePreviewPanel_DownloadsFromUrlAfterTypeAndName()
	{
		var explorer = RenderExplorer(FilePreviewModes.On, item => $"text/markdown:{item.Name}:ftp://files.example/{item.Name}");
		var provider = new FileExplorerPreviewProvider { FileExplorer = explorer };

		var act = () => provider.GetPreviewInfoAsync(Item("/notes.md"));

		(await act.Should().ThrowAsync<NotSupportedException>()).Which.Message.Should().Contain("'ftp'");
	}
}
