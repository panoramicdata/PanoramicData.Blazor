using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Options;

namespace PanoramicData.Blazor.Test.Options;

/// <summary>Tests for <see cref="PDToggleSwitchOptions"/>.</summary>
public class PDToggleSwitchOptionsTests
{
	/// <summary>A new instance is a medium, square switch with a two pixel border and no text or fixed size.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var options = new PDToggleSwitchOptions();

		options.BorderWidth.Should().Be(2);
		options.CssClass.Should().BeEmpty();
		options.TextCssClass.Should().BeEmpty();
		options.Height.Should().BeNull();
		options.Width.Should().BeNull();
		options.LabelBefore.Should().BeFalse();
		options.OffText.Should().BeEmpty();
		options.OnText.Should().BeEmpty();
		options.Rounded.Should().BeFalse();
		options.Size.Should().Be(ButtonSizes.Medium);
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var options = new PDToggleSwitchOptions
		{
			BorderWidth = 1,
			CssClass = "switch",
			TextCssClass = "text",
			Height = 20,
			Width = 40,
			LabelBefore = true,
			OffText = "Off",
			OnText = "On",
			Rounded = true,
			Size = ButtonSizes.Large
		};

		options.BorderWidth.Should().Be(1);
		options.CssClass.Should().Be("switch");
		options.TextCssClass.Should().Be("text");
		options.Height.Should().Be(20);
		options.Width.Should().Be(40);
		options.LabelBefore.Should().BeTrue();
		options.OffText.Should().Be("Off");
		options.OnText.Should().Be("On");
		options.Rounded.Should().BeTrue();
		options.Size.Should().Be(ButtonSizes.Large);
	}
}
