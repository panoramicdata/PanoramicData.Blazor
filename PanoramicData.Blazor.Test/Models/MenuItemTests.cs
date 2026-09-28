using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="MenuItem"/>.</summary>
public class MenuItemTests
{
	/// <summary>A new menu item is visible, enabled, not a separator and has no shortcut.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var item = new MenuItem();

		item.Key.Should().BeEmpty();
		item.Text.Should().BeEmpty();
		item.IconCssClass.Should().BeEmpty();
		item.Content.Should().BeEmpty();
		item.IsVisible.Should().BeTrue();
		item.IsDisabled.Should().BeFalse();
		item.IsSeparator.Should().BeFalse();
		item.ShortcutKey.HasValue.Should().BeFalse();
	}

	/// <summary>The convenience constructor defaults to enabled and visible.</summary>
	[Fact]
	public void Constructor_DefaultsToEnabledAndVisible()
	{
		var item = new MenuItem("open", "Open", "fa-folder");

		item.Key.Should().Be("open");
		item.Text.Should().Be("Open");
		item.IconCssClass.Should().Be("fa-folder");
		item.IsDisabled.Should().BeFalse();
		item.IsVisible.Should().BeTrue();
	}

	/// <summary>The convenience constructor maps enabled onto its inverse, disabled, and passes visibility through.</summary>
	[Fact]
	public void Constructor_MapsEnabledAndVisible()
	{
		var item = new MenuItem("open", "Open", "fa-folder", enabled: false, visible: false);

		item.IsDisabled.Should().BeTrue();
		item.IsVisible.Should().BeFalse();
	}

	/// <summary>The key is used when set.</summary>
	[Fact]
	public void GetKeyOrText_ReturnsKeyWhenSet()
	{
		new MenuItem { Key = "save", Text = "&&Save" }.GetKeyOrText().Should().Be("save");
	}

	/// <summary>Without a key, the text is used with its shortcut markers removed.</summary>
	[Theory]
	[InlineData("", "&&Save", "Save")]
	[InlineData(" ", "Save &&As", "Save As")]
	[InlineData("", "Plain", "Plain")]
	public void GetKeyOrText_FallsBackToTextWithoutMarkers(string key, string text, string expected)
	{
		new MenuItem { Key = key, Text = text }.GetKeyOrText().Should().Be(expected);
	}

	/// <summary>The remaining members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var shortcut = new ShortcutKey { CtrlKey = true, Key = "s" };

		var item = new MenuItem { Content = "<b>x</b>", IsSeparator = true, ShortcutKey = shortcut };

		item.Content.Should().Be("<b>x</b>");
		item.IsSeparator.Should().BeTrue();
		item.ShortcutKey.Should().BeSameAs(shortcut);
	}
}
