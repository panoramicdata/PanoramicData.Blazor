using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.PreviewProviders;
using PanoramicData.Blazor.Services;
using System.Net.Http;

namespace PanoramicData.Blazor.Test.PreviewProviders;

/// <summary>Tests for <see cref="FileExplorerPreviewProvider"/>.</summary>
/// <remarks>
/// Downloads go through an <see cref="HttpClient"/> built on <see cref="RecordingHandler"/>, so no socket is
/// opened and no machine proxy setting is involved: the handler records the address asked for and answers it.
/// </remarks>
public partial class FileExplorerPreviewProviderTests : BunitContext
{
	private static readonly DateTimeOffset _created = new(2023, 1, 2, 3, 4, 5, TimeSpan.Zero);
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
		DateCreated = _created,
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
		var handler = new RecordingHandler("# Notes");
		var explorer = RenderExplorer(FilePreviewModes.OptionalOff, item => $"https://files.example/{item.Name}");
		var provider = new FileExplorerPreviewProvider(new HttpClient(handler)) { FileExplorer = explorer };

		var info = await provider.GetPreviewInfoAsync(Item("/notes.md"));

		explorer.PreviewPanelVisible.Should().BeFalse();
		handler.Requests.Should().BeEmpty();
		info.CssClass.Should().Be("basic");
		info.HtmlContent.Value.Should().Contain("<i class=\"fa-4x far fa-fw fa-file\"></i>")
			.And.Contain(">notes</span>")
			.And.Contain(">MD File</span>")
			.And.Contain("title=\"10 bytes\"")
			.And.Contain($"Modified: {_modified.ToLocalTime():yyyy-MM-dd}");
	}

	/// <summary>The details show the item's creation date as Created, not its modification date (#174).</summary>
	[Fact]
	public async Task Details_ShowCreatedDateAsCreated()
	{
		var provider = new FileExplorerPreviewProvider { FileExplorer = RenderExplorer(FilePreviewModes.OptionalOff) };

		var info = await provider.GetPreviewInfoAsync(Item("/notes.md"));

		info.HtmlContent.Value.Should().Contain($"Created: {_created.ToLocalTime():yyyy-MM-dd}")
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
}
