using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Upload callback tests for <see cref="PDDropZone"/>.
/// </summary>
public partial class PDDropZoneTests
{
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
}
