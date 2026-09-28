using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="IconInfo"/>.</summary>
public class IconInfoTests
{
	/// <summary>A new icon has no css class or tooltip, and both round-trip.</summary>
	[Fact]
	public void Members_DefaultEmptyAndRoundTrip()
	{
		var icon = new IconInfo();
		icon.CssCls.Should().BeEmpty();
		icon.ToolTip.Should().BeEmpty();

		icon.CssCls = "fa-star";
		icon.ToolTip = "Favourite";

		icon.CssCls.Should().Be("fa-star");
		icon.ToolTip.Should().Be("Favourite");
	}
}
