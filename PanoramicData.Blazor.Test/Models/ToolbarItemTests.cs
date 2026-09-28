using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ToolbarItem"/>.</summary>
public class ToolbarItemTests
{
	/// <summary>A new item is visible, enabled, not shifted right and has no key or tooltip.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var item = new Item();

		item.Key.Should().BeEmpty();
		item.ToolTip.Should().BeEmpty();
		item.IsVisible.Should().BeTrue();
		item.IsEnabled.Should().BeTrue();
		item.ShiftRight.Should().BeFalse();
		item.Should().BeAssignableTo<IToolbarItem>();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var item = new Item { Key = "k", ToolTip = "tip", IsVisible = false, IsEnabled = false, ShiftRight = true };

		item.Key.Should().Be("k");
		item.ToolTip.Should().Be("tip");
		item.IsVisible.Should().BeFalse();
		item.IsEnabled.Should().BeFalse();
		item.ShiftRight.Should().BeTrue();
	}

	private sealed class Item : ToolbarItem
	{
	}
}
