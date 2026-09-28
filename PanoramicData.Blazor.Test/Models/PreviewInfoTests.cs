using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="PreviewInfo"/>.</summary>
public class PreviewInfoTests
{
	/// <summary>A new preview is unavailable, with no css class, url or content.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var info = new PreviewInfo();

		info.CssClass.Should().BeEmpty();
		info.PreviewAvailable.Should().BeFalse();
		info.Url.Should().BeEmpty();
		info.HtmlContent.Value.Should().BeNull();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var info = new PreviewInfo
		{
			CssClass = "md",
			PreviewAvailable = true,
			Url = "https://example.com",
			HtmlContent = new MarkupString("<p>x</p>")
		};

		info.CssClass.Should().Be("md");
		info.PreviewAvailable.Should().BeTrue();
		info.Url.Should().Be("https://example.com");
		info.HtmlContent.Value.Should().Be("<p>x</p>");
	}
}
