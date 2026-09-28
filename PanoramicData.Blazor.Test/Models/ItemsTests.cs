using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="BasicItem"/> and <see cref="SelectableItem"/>.</summary>
public class ItemsTests
{
	/// <summary>A basic item starts empty and its members round-trip.</summary>
	[Fact]
	public void BasicItem_DefaultsAndRoundTrip()
	{
		var item = new BasicItem();
		item.Id.Should().BeEmpty();
		item.Text.Should().BeEmpty();
		item.IconCssClass.Should().BeEmpty();

		item.Id = "1";
		item.Text = "One";
		item.IconCssClass = "fa-one";

		item.Id.Should().Be("1");
		item.Text.Should().Be("One");
		item.IconCssClass.Should().Be("fa-one");
		item.Should().BeAssignableTo<IDisplayItem>();
	}

	/// <summary>A selectable item starts enabled and unselected, and both flags round-trip.</summary>
	[Fact]
	public void SelectableItem_DefaultsAndRoundTrip()
	{
		var item = new SelectableItem { Text = "Pick me" };
		item.IsEnabled.Should().BeTrue();
		item.IsSelected.Should().BeFalse();

		item.IsEnabled = false;
		item.IsSelected = true;

		item.IsEnabled.Should().BeFalse();
		item.IsSelected.Should().BeTrue();
		item.Text.Should().Be("Pick me");
		item.Should().BeAssignableTo<ISelectable>();
	}
}
