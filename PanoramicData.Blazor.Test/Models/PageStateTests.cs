using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="PageState"/>.</summary>
public class PageStateTests
{
	/// <summary>A new page state is the first page of ten items, with no pages counted yet.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var state = new PageState();

		state.Page.Should().Be(1u);
		state.PageCount.Should().Be(0u);
		state.PageSize.Should().Be(10u);
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var state = new PageState { Page = 3, PageCount = 5, PageSize = 25 };

		state.Page.Should().Be(3u);
		state.PageCount.Should().Be(5u);
		state.PageSize.Should().Be(25u);
	}
}
