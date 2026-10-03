using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Content tests for <see cref="PDWidget"/>: HTML, URL, clock and image widgets.
/// </summary>
public partial class PDWidgetTests
{
	/// <summary>
	/// Verifies that HTML content is sanitised before display, and aligned as asked.
	/// </summary>
	[Theory]
	[InlineData(ContentAlignment.Top, "")]
	[InlineData(ContentAlignment.Center, "pd-widget-align-center")]
	[InlineData(ContentAlignment.Bottom, "pd-widget-align-bottom")]
	public void HtmlContent_IsSanitisedAndAligned(ContentAlignment alignment, string alignmentClass)
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Html)
			.Add(x => x.ContentAlignment, alignment)
			.Add(x => x.Content, "<p class=\"safe\">Hello</p><script>alert(1)</script>"));

		var content = widget.Find(".pd-widget-html");
		content.QuerySelector(".safe")!.TextContent.Should().Be("Hello");
		content.QuerySelector("script").Should().BeNull();
		if (alignmentClass.Length > 0)
		{
			content.ClassList.Should().Contain(alignmentClass);
		}
	}

	/// <summary>
	/// Verifies that URL content is fetched through the delegate and shown sanitised.
	/// </summary>
	[Fact]
	public void UrlContent_IsFetchedAndShown()
	{
		var urls = new List<string>();
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Url)
			.Add(x => x.Content, "https://example.test/report")
			.Add(x => x.FetchContent, url => { urls.Add(url); return Task.FromResult("<em>Fetched</em>"); }));

		widget.WaitForAssertion(() => widget.Find(".pd-widget-url em").TextContent.Should().Be("Fetched"));
		urls.Should().AllBe("https://example.test/report");
	}

	/// <summary>
	/// Verifies that a spinner is shown while URL content is being fetched, and replaced by the content once it arrives.
	/// </summary>
	[Fact]
	public void UrlContent_ShowsASpinnerWhileFetching()
	{
		var fetch = new TaskCompletionSource<string>();
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Url)
			.Add(x => x.Content, "https://example.test/report")
			.Add(x => x.FetchContent, _ => fetch.Task));

		widget.WaitForAssertion(() => widget.FindAll(".pd-widget-loading .spinner-border").Should().ContainSingle());

		fetch.SetResult("<em>Fetched</em>");

		widget.WaitForAssertion(() => widget.Find(".pd-widget-url em").TextContent.Should().Be("Fetched"));
		widget.FindAll(".pd-widget-loading").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a failed fetch, or a missing fetch delegate, is shown as an error in the widget.
	/// </summary>
	[Theory]
	[InlineData(true, "Failed to load content: Offline")]
	[InlineData(false, "No FetchContent delegate provided.")]
	public void UrlContent_ProblemsAreShownAsErrors(bool withDelegate, string message)
	{
		Func<string, Task<string>>? fetch = withDelegate ? _ => throw new HttpRequestException("Offline") : null;

		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Url)
			.Add(x => x.Content, "https://example.test/report")
			.Add(x => x.FetchContent, fetch));

		widget.WaitForAssertion(() => widget.Find(".pd-widget-error small").TextContent.Should().Be(message));
	}

	/// <summary>
	/// Verifies that a URL widget with no URL fetches nothing and shows no error.
	/// </summary>
	[Fact]
	public void UrlContent_WithoutAUrl_FetchesNothing()
	{
		var calls = 0;
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Url)
			.Add(x => x.FetchContent, _ => { calls++; return Task.FromResult(string.Empty); }));

		widget.FindAll(".pd-widget-error").Should().BeEmpty();
		calls.Should().Be(0);
	}

	/// <summary>
	/// Verifies that the clock shows the time and date in the given time zone and formats.
	/// </summary>
	[Fact]
	public void Clock_ShowsTheTimeAndDateInTheGivenZoneAndFormat()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Clock)
			.Add(x => x.ClockTimeZone, TimeZoneInfo.Utc)
			.Add(x => x.ClockTimeFormat, "'time' yyyy")
			.Add(x => x.ClockDateFormat, "'date' yyyy"));

		var year = DateTime.UtcNow.Year;
		widget.WaitForAssertion(() => widget.Find(".pd-widget-clock-time").TextContent.Should().Be($"time {year}"));
		widget.Find(".pd-widget-clock-date").TextContent.Should().Be($"date {year}");
	}

	/// <summary>
	/// Verifies that a clock without a time zone shows local time.
	/// </summary>
	[Fact]
	public void Clock_WithoutAZone_ShowsLocalTime()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Clock)
			.Add(x => x.ClockTimeFormat, "%K"));

		var offset = DateTime.Now.ToString("%K", System.Globalization.CultureInfo.CurrentCulture);
		widget.WaitForAssertion(() => widget.Find(".pd-widget-clock-time").TextContent.Should().Be(offset));
	}

	/// <summary>
	/// Verifies that image bytes are shown as a data URI of the given type, in preference to a URL.
	/// </summary>
	[Fact]
	public void Image_FromBytes_IsADataUri()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Image)
			.Add(x => x.ContentBytes, [1, 2, 3])
			.Add(x => x.ImageMimeType, "image/gif")
			.Add(x => x.Content, "https://example.test/ignored.png"));

		widget.WaitForAssertion(() => widget.Find("img.pd-widget-img").GetAttribute("src").Should().Be("data:image/gif;base64,AQID"));
		widget.Find("img.pd-widget-img").GetAttribute("alt").Should().Be("Sales");
	}

	/// <summary>
	/// Verifies that an image URL is used when there are no bytes, and nothing is shown when there is neither.
	/// </summary>
	[Theory]
	[InlineData("https://example.test/logo.png", 1)]
	[InlineData(null, 0)]
	public void Image_FromAUrl_OrNothing(string? url, int images)
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Image)
			.Add(x => x.Content, url));

		widget.WaitForAssertion(() => widget.FindAll("img").Should().HaveCount(images));
		if (url is not null)
		{
			widget.Find("img").GetAttribute("src").Should().Be(url);
		}
	}
}
