using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ToolbarButton"/>.</summary>
public class ToolbarButtonTests
{
	/// <summary>A new button is a light, visible, enabled button with no text, icon or size.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var button = new ToolbarButton();

		button.CssClass.Should().Be("btn-light");
		button.IconCssClass.Should().BeEmpty();
		button.Text.Should().BeEmpty();
		button.TextCssClass.Should().BeEmpty();
		button.Size.Should().BeNull();
		button.Key.Should().BeEmpty();
		button.IsVisible.Should().BeTrue();
		button.IsEnabled.Should().BeTrue();
	}

	/// <summary>Without an explicit key, the key follows the button text.</summary>
	[Fact]
	public void Key_DefaultsToText()
	{
		var button = new ToolbarButton { Text = "Save" };
		button.Key.Should().Be("Save");

		button.Text = "Save As";

		button.Key.Should().Be("Save As");
	}

	/// <summary>An explicit key takes precedence over the text.</summary>
	[Fact]
	public void Key_ExplicitKeyWins()
	{
		var button = new ToolbarButton { Text = "Save", Key = "save-btn" };

		button.Key.Should().Be("save-btn");
	}

	/// <summary>The styling members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var button = new ToolbarButton
		{
			CssClass = "btn-primary",
			IconCssClass = "fa-save",
			TextCssClass = "bold",
			Size = ButtonSizes.Small
		};

		button.CssClass.Should().Be("btn-primary");
		button.IconCssClass.Should().Be("fa-save");
		button.TextCssClass.Should().Be("bold");
		button.Size.Should().Be(ButtonSizes.Small);
	}
}
