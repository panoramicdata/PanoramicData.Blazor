using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Upload behaviour of <see cref="PDFileExplorer"/>, driven through the callbacks its drop zones raise:
/// dropping files, per-file progress rows, upload conflicts and the upload and progress dialogs.
/// </summary>
public partial class PDFileExplorerTests
{
	private const string UploadUrl = "https://example.com/upload";
	private const string DropZoneModulePath = "./_content/PanoramicData.Blazor/PDDropZone.razor.js";

	private static IRenderedComponent<PDDropZone> DropZone(IRenderedComponent<PDFileExplorer> cut, string id)
		=> cut.FindComponents<PDDropZone>().Single(z => z.Instance.Id == id);

	private static DropZoneUploadProgressEventArgs Progress(string path, string name, double percent)
		=> new(path, name, 10, "k", "s", percent);

	private IRenderedComponent<PDFileExplorer> RenderUploader(Action<ComponentParameterCollectionBuilder<PDFileExplorer>>? configure = null)
		=> RenderExplorer(p =>
		{
			p.Add(x => x.UploadUrl, UploadUrl);
			configure?.Invoke(p);
		});

	/// <summary>Files dropped are tagged with the current folder, and the dialog's drop message is hidden.</summary>
	[Fact]
	public async Task FilesDropped_AreTaggedWithCurrentFolder()
	{
		var cut = RenderUploader();
		await NavigateAsync(cut, "/Docs");
		var zone = DropZone(cut, "pdfe-drop-zone-2");
		var args = new DropZoneEventArgs(zone.Instance, []);

		await zone.InvokeAsync(() => zone.Instance.Drop.InvokeAsync(args));

		args.BaseFolder.Should().Be("/Docs");
		args.State.Should().Be("/Docs");
		_common.Invocations["addClass"].Should().ContainSingle().Which.Arguments.Should().Equal("pdfe-drop-zone-1", "dz-started");
	}

	/// <summary>Upload start, progress and completion are passed on to the application.</summary>
	[Fact]
	public async Task UploadEvents_AreRaisedToTheApplication()
	{
		var events = new List<string>();
		var cut = RenderUploader(p => p
			.Add(x => x.UploadStarted, (DropZoneUploadEventArgs a) => events.Add($"start {a.FullPath}"))
			.Add(x => x.UploadProgress, (DropZoneUploadProgressEventArgs a) => events.Add($"progress {a.Progress}"))
			.Add(x => x.UploadCompleted, (DropZoneUploadCompletedEventArgs a) => events.Add($"done {a.FullPath}")));
		var zone = DropZone(cut, "pdfe-drop-zone-3");

		await zone.InvokeAsync(() => zone.Instance.UploadStarted.InvokeAsync(new DropZoneUploadEventArgs("/", "new.bin", 10, "k", "s") { BatchCount = 1 }));
		await zone.InvokeAsync(() => zone.Instance.UploadProgress.InvokeAsync(Progress("/", "new.bin", 40)));
		await zone.InvokeAsync(() => zone.Instance.UploadCompleted.InvokeAsync(new DropZoneUploadCompletedEventArgs("/", "new.bin", 10, "k", "s")));

		events.Should().Equal("start /new.bin", "progress 40", "done /new.bin");
	}

	/// <summary>A file uploading into the current folder appears as an uploading row with its progress, until it completes.</summary>
	[Fact]
	public async Task UploadIntoCurrentFolder_ShowsProgressRow()
	{
		var cut = RenderUploader();
		var zone = DropZone(cut, "pdfe-drop-zone-3");

		await zone.InvokeAsync(() => zone.Instance.UploadProgress.InvokeAsync(Progress("/", "new.bin", 40)));
		await zone.InvokeAsync(() => zone.Instance.UploadProgress.InvokeAsync(Progress("/", "new.bin", 60)));

		var row = Table(cut).Instance.ItemsToDisplay.Should().ContainSingle(x => x.Name == "new.bin").Subject;
		row.IsUploading.Should().BeTrue();
		row.UploadProgress.Should().Be(60);
		row.Path.Should().Be("/new.bin");
		cut.WaitForAssertion(() => cut.Find("tr[id='/new.bin']").ClassList.Should().Contain("uploading"));
		cut.Find("tr[id='/new.bin'] div.upload-progress-bar").GetAttribute("style").Should().Contain("width: 60%");

		await zone.InvokeAsync(() => zone.Instance.UploadCompleted.InvokeAsync(new DropZoneUploadCompletedEventArgs("/", "new.bin", 10, "k", "s")));

		row.IsUploading.Should().BeFalse();
	}

	/// <summary>A file uploading into a sub-folder shows that sub-folder as an uploading row.</summary>
	[Fact]
	public async Task UploadIntoSubFolder_ShowsFolderRow()
	{
		var cut = RenderUploader();
		var zone = DropZone(cut, "pdfe-drop-zone-3");

		await zone.InvokeAsync(() => zone.Instance.UploadProgress.InvokeAsync(Progress("/NewDir/deeper", "x.bin", 10)));
		await zone.InvokeAsync(() => zone.Instance.UploadProgress.InvokeAsync(Progress("/Docs", "y.bin", 10)));

		var rows = Table(cut).Instance.ItemsToDisplay;
		var folder = rows.Should().ContainSingle(x => x.Name == "NewDir").Subject;
		folder.EntryType.Should().Be(FileExplorerItemType.Directory);
		folder.IsUploading.Should().BeTrue();
		rows.Single(x => x.Name == "Docs").UploadProgress.Should().Be(10);
	}

	/// <summary>An upload into an unrelated folder adds no row.</summary>
	[Fact]
	public async Task UploadElsewhere_AddsNoRow()
	{
		var cut = RenderUploader();
		await NavigateAsync(cut, "/Docs");
		var zone = DropZone(cut, "pdfe-drop-zone-3");

		await zone.InvokeAsync(() => zone.Instance.UploadProgress.InvokeAsync(Progress("/Media", "x.bin", 10)));
		await zone.InvokeAsync(() => zone.Instance.UploadCompleted.InvokeAsync(new DropZoneUploadCompletedEventArgs("/Media", "x.bin", 10, "k", "s")));

		RowNames(cut).Should().NotContain("x.bin");
	}

	/// <summary>Uploads with no name clash proceed without prompting.</summary>
	[Fact]
	public async Task UploadsReady_NoConflicts_Proceed()
	{
		var cut = RenderUploader();
		var zone = DropZone(cut, "pdfe-drop-zone-1");
		var args = new UploadsReadyEventArgs { Files = [new DropZoneFile { Path = "/", Name = "fresh.bin", Key = "1" }] };

		await zone.InvokeAsync(() => zone.Instance.AllUploadsReady.InvokeAsync(args));

		args.Cancel.Should().BeFalse();
		args.Overwrite.Should().BeFalse();
		args.FilesToSkip.Should().BeEmpty();
	}

	/// <summary>Uploads that clash with existing files prompt; the answer sets cancel, overwrite or the files to skip.</summary>
	/// <param name="answer">The button chosen.</param>
	[Theory]
	[InlineData("Cancel")]
	[InlineData("Overwrite")]
	[InlineData("Skip")]
	public async Task UploadsReady_Conflicts_ApplyTheAnswer(string answer)
	{
		var cut = RenderUploader();
		await NavigateAsync(cut, "/Docs");
		var zone = DropZone(cut, "pdfe-drop-zone-1");
		var args = new UploadsReadyEventArgs
		{
			Files = [new DropZoneFile { Path = "/Docs", Name = "a.docx", Key = "1" }, new DropZoneFile { Path = "/Docs", Name = "fresh.bin", Key = "2" }]
		};

		var pending = zone.InvokeAsync(() => zone.Instance.AllUploadsReady.InvokeAsync(args));
		WaitForConflictPrompt(cut, "1 conflicts found");
		await AnswerAsync(Modal(cut, "Move / Copy Conflict"), answer);
		await pending;

		args.Cancel.Should().Be(answer == "Cancel");
		args.Overwrite.Should().Be(answer == "Overwrite");
		args.FilesToSkip.Select(x => x.Key).Should().Equal(answer == "Skip" ? ["1"] : Array.Empty<string>());
	}

	/// <summary>Conflict checks for a folder are cached, so a second batch does not query the provider again.</summary>
	[Fact]
	public async Task UploadsReady_CachesFolderListings()
	{
		var cut = RenderUploader();
		var zone = DropZone(cut, "pdfe-drop-zone-1");
		var args = new UploadsReadyEventArgs { Files = [new DropZoneFile { Path = "/Empty", Name = "x.bin", Key = "1" }] };
		await zone.InvokeAsync(() => zone.Instance.AllUploadsReady.InvokeAsync(args));
		_provider.Requests.Clear();

		await zone.InvokeAsync(() => zone.Instance.AllUploadsReady.InvokeAsync(args));

		_provider.Requests.Should().BeEmpty();
	}

	/// <summary>A batch above the threshold swaps the upload dialog for the progress dialog, which lists files in flight.</summary>
	[Fact]
	public async Task UploadsStarted_AboveThreshold_ShowsProgressDialog()
	{
		var cut = RenderUploader();
		var zone = DropZone(cut, "pdfe-drop-zone-1");

		await zone.InvokeAsync(() => zone.Instance.AllUploadsStarted.InvokeAsync(2));
		await zone.InvokeAsync(() => zone.Instance.UploadStarted.InvokeAsync(new DropZoneUploadEventArgs("/", "one.bin", 10, "k", "s") { BatchCount = 2, BatchProgress = 1 }));
		await zone.InvokeAsync(() => zone.Instance.AllUploadsProgress.InvokeAsync(new DropZoneAllProgressEventArgs { TotalBytes = 20, TotalBytesSent = 10 }));

		var progress = Modal(cut, "Upload Progress");
		progress.WaitForAssertion(() => progress.Find(".overall-progress").TextContent.Should().Contain("1").And.Contain("2"));
		progress.Find(".file-progress").TextContent.Should().Contain("/one.bin");
	}

	/// <summary>When every upload completes, caches are dropped and both panes are refreshed.</summary>
	[Fact]
	public async Task UploadsComplete_RefreshesTreeAndTable()
	{
		var cut = RenderUploader();
		var zone = DropZone(cut, "pdfe-drop-zone-1");
		_provider.Requests.Clear();

		await zone.InvokeAsync(() => zone.Instance.AllUploadsComplete.InvokeAsync());

		_provider.Requests.Should().Contain(string.Empty).And.Contain("/");
	}

	/// <summary>The upload dialog's Clear button clears both drop zones and restores the drop message.</summary>
	[Fact]
	public void ClearButton_ClearsDropZones()
	{
		var dropZones = JSInterop.SetupModule(DropZoneModulePath);
		var cut = RenderUploader();

		UploadModal(cut).Find("button.btn-default").Click();

		_common.Invocations["removeClass"].Should().ContainSingle();
		dropZones.Invocations["clear"].Should().HaveCount(2);
	}

	/// <summary>The progress dialog's Cancel button blocks the page and cancels both drop zones.</summary>
	[Fact]
	public void CancelButton_BlocksAndCancelsDropZones()
	{
		var dropZones = JSInterop.SetupModule(DropZoneModulePath);
		var overlay = Services.GetRequiredService<IBlockOverlayService>();
		var shown = 0;
		overlay.OnShow += _ => shown++;
		var cut = RenderUploader();
		var shownBefore = shown;

		Modal(cut, "Upload Progress").Find("button.btn-danger").Click();

		dropZones.Invocations["cancel"].Should().HaveCount(2);
		shown.Should().Be(shownBefore + 1);
	}

	/// <summary>Clicking the drop message asks the browser to open the file picker.</summary>
	[Fact]
	public void DropMessageClick_OpensFilePicker()
	{
		var cut = RenderUploader();

		cut.Find("#pdfe-drop-zone-div").Click();

		_common.Invocations["clickClosest"].Should().ContainSingle().Which.Arguments.Should().Equal("pdfe-drop-zone-div", ".pddropzone");
	}

	/// <summary>The upload toolbar button and menu entries open the upload dialog, and its Close button hides it.</summary>
	[Fact]
	public async Task UploadDialog_OpensAndCloses()
	{
		var modalObject = JSInterop.SetupModule("./_content/PanoramicData.Blazor/PDModal.razor.js").SetupModule("initialize", _ => true);
		var cut = RenderUploader();

		await ClickToolbarAsync(cut, "upload");
		await ClickMenuAsync(TableMenu(cut), "Upload Files");
		await ClickMenuAsync(TreeMenu(cut), "Upload Files");
		await AnswerAsync(UploadModal(cut), "No");

		modalObject.Invocations["show"].Should().HaveCount(3);
		modalObject.Invocations["hide"].Should().NotBeEmpty();
	}
}
