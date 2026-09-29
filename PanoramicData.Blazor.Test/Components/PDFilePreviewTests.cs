using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFilePreview"/> renders what its preview provider returns, shows a basic preview
/// while a slow preview loads, falls back on failure, and does not re-request an unchanged item.
/// </summary>
public class PDFilePreviewTests : BunitContext
{
	/// <summary>
	/// How long to wait for a render that another thread or a timer brings about. Generous because a busy
	/// machine (the whole suite under coverage) can hold the renderer's dispatcher well past bUnit's default.
	/// </summary>
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	private static readonly FileExplorerItem _item = new() { Path = "/docs/readme.md", Name = "readme.md" };

	/// <summary>Verifies that a preview with a URL is shown in a sandboxed iframe pointing at it.</summary>
	[Fact]
	public void A_url_preview_renders_a_sandboxed_iframe()
	{
		var provider = new FakePreviewProvider(new PreviewInfo { Url = "https://example.com/", CssClass = "url" });

		var component = Render<PDFilePreview>(parameters => parameters
			.Add(p => p.PreviewProvider, provider)
			.Add(p => p.Item, _item));

		var frame = component.Find(".pdfilepreview.url iframe");
		frame.GetAttribute("src").Should().Be("https://example.com/");
		frame.HasAttribute("sandbox").Should().BeTrue();
	}

	/// <summary>Verifies that an html preview is shown via srcdoc in a sandboxed iframe, never inline.</summary>
	[Fact]
	public void An_html_preview_renders_via_srcdoc()
	{
		var provider = new FakePreviewProvider(new PreviewInfo
		{
			CssClass = "html",
			HtmlContent = new MarkupString("<b>hello</b>")
		});

		var component = Render<PDFilePreview>(parameters => parameters
			.Add(p => p.PreviewProvider, provider)
			.Add(p => p.Item, _item));

		var frame = component.Find("iframe.web");
		frame.GetAttribute("srcdoc").Should().Be("<b>hello</b>");
		component.FindAll(".pdfilepreview > b").Should().BeEmpty();
	}

	/// <summary>Verifies that any other preview renders its markup inline.</summary>
	[Fact]
	public void Other_previews_render_their_markup_inline()
	{
		var provider = new FakePreviewProvider(new PreviewInfo
		{
			CssClass = "md",
			HtmlContent = new MarkupString("<h1>Title</h1>")
		});

		var component = Render<PDFilePreview>(parameters => parameters
			.Add(p => p.PreviewProvider, provider)
			.Add(p => p.Item, _item));

		component.Find(".pdfilepreview.md h1").TextContent.Should().Be("Title");
		component.FindAll("iframe").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the same item supplied again is not previewed a second time, so re-renders of a file
	/// explorer do not re-download the file.
	/// </summary>
	[Fact]
	public void An_unchanged_item_is_not_previewed_again()
	{
		var provider = new FakePreviewProvider(new PreviewInfo { CssClass = "txt" });

		var component = Render<PDFilePreview>(parameters => parameters
			.Add(p => p.PreviewProvider, provider)
			.Add(p => p.Item, _item));

		component.Render(parameters => parameters
			.Add(p => p.Item, new FileExplorerItem { Path = _item.Path }));

		provider.PreviewedPaths.Should().Equal(_item.Path);
	}

	/// <summary>Verifies that a different item is previewed.</summary>
	[Fact]
	public void A_different_item_is_previewed()
	{
		var provider = new FakePreviewProvider(new PreviewInfo { CssClass = "txt" });

		var component = Render<PDFilePreview>(parameters => parameters
			.Add(p => p.PreviewProvider, provider)
			.Add(p => p.Item, _item));

		component.Render(parameters => parameters
			.Add(p => p.Item, new FileExplorerItem { Path = "/other.txt" }));

		provider.PreviewedPaths.Should().Equal(_item.Path, "/other.txt");
	}

	/// <summary>
	/// Verifies that a preview slower than the spinner trigger first shows the basic preview with spinner,
	/// then the full preview once it arrives.
	/// </summary>
	[Fact]
	public void A_slow_preview_shows_the_basic_spinner_preview_first()
	{
		var pending = new TaskCompletionSource<PreviewInfo>();
		var provider = new FakePreviewProvider(pending.Task);

		var component = Render<PDFilePreview>(parameters => parameters
			.Add(p => p.PreviewProvider, provider)
			.Add(p => p.Item, _item));

		component.WaitForAssertion(() => component.Find(".pdfilepreview.basic .spinner").Should().NotBeNull(), Patience);
		provider.SpinnerRequests.Should().Be(1);

		pending.SetResult(new PreviewInfo { CssClass = "md", HtmlContent = new MarkupString("<p>done</p>") });

		component.WaitForAssertion(() => component.Find(".pdfilepreview.md p").TextContent.Should().Be("done"), Patience);
	}

	/// <summary>
	/// Verifies that a failing preview falls back to the basic preview and hands the exception to the
	/// exception handler rather than breaking the component.
	/// </summary>
	[Fact]
	public void A_failing_preview_falls_back_and_reports_the_exception()
	{
		var failure = new InvalidOperationException("download failed");
		var provider = new FakePreviewProvider(Task.FromException<PreviewInfo>(failure));
		var reported = new List<Exception>();

		var component = Render<PDFilePreview>(parameters => parameters
			.Add(p => p.PreviewProvider, provider)
			.Add(p => p.ExceptionHandler, (Exception ex) => reported.Add(ex))
			.Add(p => p.Item, _item));

		component.WaitForAssertion(() => reported.Should().ContainSingle().Which.Should().BeSameAs(failure), Patience);
		component.Find(".pdfilepreview.basic .basic-content").Should().NotBeNull();
	}

	/// <summary>
	/// A preview provider returning a fixed result, recording which items are previewed and how often a spinner is asked for.
	/// Both timings are zero so the spinner path is decided by whether the preview task has completed.
	/// </summary>
	private sealed class FakePreviewProvider(Task<PreviewInfo> preview) : IPreviewProvider
	{
		public FakePreviewProvider(PreviewInfo info)
			: this(Task.FromResult(info))
		{
		}

		public List<string?> PreviewedPaths { get; } = [];

		public int SpinnerRequests { get; private set; }

		public string DateTimeFormat { get; set; } = "yyyy-MM-dd";

		public int SpinnerTriggerMs { get; set; }

		public int SpinnerMinDisplayMs { get; set; }

		public Task<PreviewInfo> GetBasicPreviewInfoAsync(FileExplorerItem? item, bool spinner = false)
		{
			if (spinner)
			{
				SpinnerRequests++;
			}

			var content = spinner ? "<i class=\"spinner\"></i>" : $"<span class=\"basic-content\">{item?.Name}</span>";
			return Task.FromResult(new PreviewInfo { CssClass = "basic", HtmlContent = new MarkupString(content) });
		}

		public Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item)
		{
			PreviewedPaths.Add(item?.Path);
			return preview;
		}
	}
}
