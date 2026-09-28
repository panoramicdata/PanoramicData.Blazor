using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDMenuItem"/>: a declared menu item registers itself, with all its settings, on the
/// enclosing <see cref="PDToolbarDropdown"/>.
/// </summary>
public class PDMenuItemTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDMenuItemTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>Each declared item is added to the dropdown's items, carrying every parameter.</summary>
	[Fact]
	public void DeclaredItems_AreRegisteredWithTheDropdown()
	{
		var shortcut = ShortcutKey.Create("ctrl-s");
		var cut = Render<PDToolbarDropdown>(p => p
			.Add(x => x.Text, "File")
			.Add(x => x.ChildContent, builder =>
			{
				builder.OpenComponent<PDMenuItem>(0);
				builder.AddAttribute(1, nameof(PDMenuItem.Key), "save");
				builder.AddAttribute(2, nameof(PDMenuItem.Text), "Save");
				builder.AddAttribute(3, nameof(PDMenuItem.IconCssClass), "fas fa-save");
				builder.AddAttribute(4, nameof(PDMenuItem.IsDisabled), true);
				builder.AddAttribute(5, nameof(PDMenuItem.ShortcutKey), shortcut);
				builder.CloseComponent();
				builder.OpenComponent<PDMenuItem>(6);
				builder.AddAttribute(7, nameof(PDMenuItem.IsSeparator), true);
				builder.AddAttribute(8, nameof(PDMenuItem.IsVisible), false);
				builder.AddAttribute(9, nameof(PDMenuItem.Content), "<b>custom</b>");
				builder.CloseComponent();
			}));

		var items = cut.Instance.Items;
		items.Should().HaveCount(2);

		var save = items[0];
		save.Key.Should().Be("save");
		save.Text.Should().Be("Save");
		save.IconCssClass.Should().Be("fas fa-save");
		save.IsDisabled.Should().BeTrue();
		save.IsVisible.Should().BeTrue();
		save.IsSeparator.Should().BeFalse();
		save.ShortcutKey.Should().BeSameAs(shortcut);

		var separator = items[1];
		separator.IsSeparator.Should().BeTrue();
		separator.IsVisible.Should().BeFalse();
		separator.Content.Should().Be("<b>custom</b>");
	}

	/// <summary>A registered item is rendered as a row of the dropdown menu.</summary>
	[Fact]
	public void RegisteredItem_IsRenderedInTheMenu()
	{
		var cut = Render<PDToolbarDropdown>(p => p
			.Add(x => x.ChildContent, builder =>
			{
				builder.OpenComponent<PDMenuItem>(0);
				builder.AddAttribute(1, nameof(PDMenuItem.Text), "Open");
				builder.CloseComponent();
			}));

		cut.FindAll("tr.pddropdownmenuitem").Should().ContainSingle()
			.Which.TextContent.Should().Contain("Open");
	}

	/// <summary>An item rendered outside any dropdown has nothing to register with and still initialises.</summary>
	[Fact]
	public void OutsideADropdown_InitialisesWithDefaults()
	{
		var cut = Render<PDMenuItem>();

		cut.Instance.ToolbarDropdown.Should().BeNull();
		cut.Instance.IsVisible.Should().BeTrue();
		cut.Instance.IsDisabled.Should().BeFalse();
		cut.Markup.Trim().Should().BeEmpty();
	}
}
