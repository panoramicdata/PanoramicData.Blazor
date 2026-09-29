using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDDropZone"/> initialises its JavaScript uploader and turns the uploader's callbacks
/// into the matching event callbacks.
/// </summary>
/// <remarks>
/// The callbacks are the <c>[JSInvokable]</c> methods the JavaScript module calls, so the tests call them
/// directly exactly as the module would.
/// </remarks>
public class PDDropZoneTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDDropZone.razor.js";

	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context and the drop zone's JavaScript module.</summary>
	public PDDropZoneTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
	}

	/// <summary>The zone renders its id, CSS class and child content, generating an id when none is given.</summary>
	[Fact]
	public void Renders_id_css_and_child_content()
	{
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.CssClass, "extra")
			.Add(p => p.ChildContent, (RenderFragment)(b => b.AddContent(0, "Drop here"))));

		var zone = component.Find("div.pddropzone");
		zone.Id.Should().StartWith("pddz");
		zone.ClassList.Should().Contain("extra");
		zone.TextContent.Trim().Should().Be("Drop here");

		var named = Render<PDDropZone>(parameters => parameters.Add(p => p.Id, "mine"));
		named.Find("div.pddropzone").Id.Should().Be("mine");
	}

	/// <summary>With an upload URL the module is initialised against the zone with the configured options.</summary>
	[Fact]
	public void An_upload_url_initialises_the_uploader()
	{
		Render<PDDropZone>(parameters => parameters
			.Add(p => p.Id, "zone")
			.Add(p => p.UploadUrl, "/upload")
			.Add(p => p.SessionId, "session-1")
			.Add(p => p.Timeout, 5));

		var initialize = _module.VerifyInvoke("initialize");
		initialize.Arguments[0].Should().Be("#zone");
		initialize.Arguments[2].Should().Be("session-1");
		initialize.Arguments[1]!.ToString().Should().Contain("url = /upload").And.Contain("timeout = 5000");
	}

	/// <summary>Without an upload URL the module is never loaded.</summary>
	[Fact]
	public void No_upload_url_does_not_initialise_the_uploader()
	{
		Render<PDDropZone>();

		_module.Invocations["initialize"].Should().BeEmpty();
	}

	/// <summary>A key press is passed on through KeyDown.</summary>
	[Fact]
	public void Key_down_is_raised()
	{
		string? key = null;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.KeyDown, (KeyboardEventArgs a) => key = a.Key));

		component.Find("div.pddropzone").KeyDown(new KeyboardEventArgs { Key = "Delete" });

		key.Should().Be("Delete");
	}

	/// <summary>A drop raises Drop, and whatever the handler sets is returned to the uploader.</summary>
	[Fact]
	public async Task OnDrop_raises_Drop_and_returns_the_handler_decision()
	{
		DropZoneEventArgs? received = null;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.Drop, (DropZoneEventArgs a) =>
			{
				received = a;
				a.Cancel = true;
				a.CancelReason = "no";
				a.BaseFolder = "/root";
			}));
		var files = new[] { new DropZoneFile { Name = "a.txt", Path = "/" } };

		var result = await component.InvokeAsync(() => component.Instance.OnDrop(files));

		received!.Files.Should().BeSameAs(files);
		received.Sender.Should().BeSameAs(component.Instance);
		var text = result.ToString();
		text.Should().Contain("cancel = True").And.Contain("reason = no").And.Contain("rootDir = /root");
	}

	/// <summary>An upload beginning raises UploadStarted and returns any form fields as name=value pairs.</summary>
	[Fact]
	public async Task OnUploadBegin_returns_the_form_fields()
	{
		DropZoneUploadEventArgs? started = null;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.UploadStarted, (DropZoneUploadEventArgs a) =>
			{
				started = a;
				a.FormFields["folder"] = "docs";
			}));

		var fields = await component.InvokeAsync(() => component.Instance.OnUploadBeginAsync(File("a.txt")));

		fields.Should().Equal("folder=docs");
		started!.Name.Should().Be("a.txt");
		started.Key.Should().Be("k-a.txt");
		started.Size.Should().Be(42);
	}

	/// <summary>With no form fields an upload beginning returns an empty list.</summary>
	[Fact]
	public async Task OnUploadBegin_without_fields_returns_none()
	{
		var component = Render<PDDropZone>();

		var fields = await component.InvokeAsync(() => component.Instance.OnUploadBeginAsync(File("a.txt")));

		fields.Should().BeEmpty();
	}

	/// <summary>Every per-file callback refuses a missing file, path or name.</summary>
	[Fact]
	public async Task Per_file_callbacks_refuse_incomplete_files()
	{
		var zone = Render<PDDropZone>().Instance;

		await FluentActions.Invoking(() => zone.OnUploadBeginAsync(null!)).Should().ThrowAsync<ArgumentNullException>();
		await FluentActions.Invoking(() => zone.OnUploadBeginAsync(new DropZoneFile { Name = "a" })).Should().ThrowAsync<ArgumentException>();
		await FluentActions.Invoking(() => zone.OnUploadBeginAsync(new DropZoneFile { Path = "/" })).Should().ThrowAsync<ArgumentException>();

		FluentActions.Invoking(() => zone.OnUploadProgress(null!)).Should().Throw<ArgumentNullException>();
		FluentActions.Invoking(() => zone.OnUploadProgress(new DropZoneFileUploadProgress { Name = "a" })).Should().Throw<ArgumentException>();
		FluentActions.Invoking(() => zone.OnUploadProgress(new DropZoneFileUploadProgress { Path = "/" })).Should().Throw<ArgumentException>();

		FluentActions.Invoking(() => zone.OnUploadEnd(null!)).Should().Throw<ArgumentNullException>();
		FluentActions.Invoking(() => zone.OnUploadEnd(new DropZoneFileUploadOutcome { Name = "a" })).Should().Throw<ArgumentException>();
		FluentActions.Invoking(() => zone.OnUploadEnd(new DropZoneFileUploadOutcome { Path = "/" })).Should().Throw<ArgumentException>();
	}

	/// <summary>Upload progress is passed on with the percentage.</summary>
	[Fact]
	public async Task OnUploadProgress_raises_UploadProgress()
	{
		DropZoneUploadProgressEventArgs? progress = null;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.UploadProgress, (DropZoneUploadProgressEventArgs a) => progress = a));

		await component.InvokeAsync(() => component.Instance.OnUploadProgress(new DropZoneFileUploadProgress { Name = "a.txt", Path = "/", Progress = 55 }));

		progress!.Progress.Should().Be(55);
		progress.Name.Should().Be("a.txt");
	}

	/// <summary>
	/// A batch reports its size when ready, each completed file counts towards the batch, a failure carries its
	/// reason, and skipped files are removed before processing.
	/// </summary>
	[Fact]
	public async Task A_batch_counts_progress_and_skips_files()
	{
		var started = new List<int>();
		var completed = new List<DropZoneUploadCompletedEventArgs>();
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.Id, "zone")
			.Add(p => p.UploadUrl, "/upload")
			.Add(p => p.AllUploadsStarted, (int count) => started.Add(count))
			.Add(p => p.UploadCompleted, (DropZoneUploadCompletedEventArgs a) => completed.Add(a))
			.Add(p => p.AllUploadsReady, (UploadsReadyEventArgs a) =>
			{
				a.FilesToSkip = [a.Files[2]];
				a.Overwrite = true;
			}));
		DropZoneFile[] files = [File("a"), File("b"), File("c")];

		await component.InvokeAsync(() => component.Instance.OnAllUploadsReadyAsync(files));
		await component.InvokeAsync(() => component.Instance.OnUploadEnd(new DropZoneFileUploadOutcome { Name = "a", Path = "/", Success = true, Reason = "ignored" }));
		await component.InvokeAsync(() => component.Instance.OnUploadEnd(new DropZoneFileUploadOutcome { Name = "b", Path = "/", Reason = "too big" }));

		started.Should().Equal(2);
		_module.VerifyInvoke("removeFile").Arguments.Should().Equal("#zone", "k-c");
		_module.VerifyInvoke("process").Arguments.Should().Equal("#zone", true);
		completed.Select(c => (c.BatchCount, c.BatchProgress, c.Success, c.Reason)).Should().Equal(
			(2, 1, true, string.Empty),
			(2, 2, false, "too big"));
	}

	/// <summary>A batch cancelled by the handler clears the queue and starts nothing.</summary>
	[Fact]
	public async Task A_cancelled_batch_clears_the_queue()
	{
		var started = 0;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.Id, "zone")
			.Add(p => p.UploadUrl, "/upload")
			.Add(p => p.AllUploadsStarted, (int _) => started++)
			.Add(p => p.AllUploadsReady, (UploadsReadyEventArgs a) => a.Cancel = true));

		await component.InvokeAsync(() => component.Instance.OnAllUploadsReadyAsync([File("a")]));

		started.Should().Be(0);
		_module.VerifyInvoke("cancel").Arguments.Should().Equal("#zone");
		_module.Invocations["process"].Should().BeEmpty();
	}

	/// <summary>Without a loaded module a batch is still counted and completion is still reported.</summary>
	[Fact]
	public async Task A_batch_without_a_module_is_counted_and_completes()
	{
		var started = new List<int>();
		var completed = 0;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.AllUploadsStarted, (int count) => started.Add(count))
			.Add(p => p.AllUploadsReady, (UploadsReadyEventArgs a) => a.Cancel = a.Files.Length > 1)
			.Add(p => p.AllUploadsComplete, () => completed++));

		await component.InvokeAsync(() => component.Instance.OnAllUploadsReadyAsync([File("a")]));
		await component.InvokeAsync(() => component.Instance.OnAllUploadsReadyAsync([File("a"), File("b")]));
		await component.InvokeAsync(component.Instance.OnAllUploadsComplete);

		started.Should().Equal(1);
		completed.Should().Be(1);
		_module.Invocations["cancel"].Should().BeEmpty();
	}

	/// <summary>Completing a batch clears the queue, resets the count and reports completion.</summary>
	[Fact]
	public async Task OnAllUploadsComplete_clears_and_reports()
	{
		var completed = 0;
		DropZoneUploadCompletedEventArgs? last = null;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.Id, "zone")
			.Add(p => p.UploadUrl, "/upload")
			.Add(p => p.AllUploadsComplete, () => completed++)
			.Add(p => p.UploadCompleted, (DropZoneUploadCompletedEventArgs a) => last = a));
		await component.InvokeAsync(() => component.Instance.OnAllUploadsReadyAsync([File("a")]));

		await component.InvokeAsync(component.Instance.OnAllUploadsComplete);
		await component.InvokeAsync(() => component.Instance.OnUploadEnd(new DropZoneFileUploadOutcome { Name = "late", Path = "/", Success = true }));

		completed.Should().Be(1);
		_module.VerifyInvoke("cancel").Arguments.Should().Equal("#zone");
		last!.BatchCount.Should().Be(0);
		last.BatchProgress.Should().Be(1);
	}

	/// <summary>Overall progress is passed on with its byte counts.</summary>
	[Fact]
	public async Task OnAllUploadsProgress_raises_AllUploadsProgress()
	{
		DropZoneAllProgressEventArgs? progress = null;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.AllUploadsProgress, (DropZoneAllProgressEventArgs a) => progress = a));

		await component.InvokeAsync(() => component.Instance.OnAllUploadsProgress(50, 200, 100));

		progress!.UploadProgress.Should().Be(50);
		progress.TotalBytes.Should().Be(200);
		progress.TotalBytesSent.Should().Be(100);
	}

	/// <summary>
	/// CancelAsync and ClearAsync pass the module a CSS selector for the zone, which is what its
	/// <c>document.querySelector</c> needs; the bare id would look for an element named after it (#184).
	/// </summary>
	[Fact]
	public async Task Cancel_and_clear_select_the_zone_by_id()
	{
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.Id, "zone")
			.Add(p => p.UploadUrl, "/upload"));

		await component.InvokeAsync(() => component.Instance.CancelAsync());
		await component.InvokeAsync(() => component.Instance.ClearAsync());

		_module.VerifyInvoke("cancel").Arguments.Should().Equal("#zone");
		_module.VerifyInvoke("clear").Arguments.Should().Equal("#zone");
	}

	/// <summary>
	/// Disposing destroys the uploader in the module. The module's <c>dispose</c> looks the zone up with
	/// <c>document.getElementById</c>, so it is given the bare id, unlike the selector-based functions.
	/// </summary>
	[Fact]
	public async Task Dispose_destroys_the_uploader()
	{
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.Id, "zone")
			.Add(p => p.UploadUrl, "/upload"));

		await component.Instance.DisposeAsync();

		_module.VerifyInvoke("dispose").Arguments.Should().Equal("zone");
	}

	private static DropZoneFile File(string name) => new()
	{
		Name = name,
		Path = "/",
		Size = 42,
		Key = $"k-{name}",
		SessionId = "s"
	};
}
