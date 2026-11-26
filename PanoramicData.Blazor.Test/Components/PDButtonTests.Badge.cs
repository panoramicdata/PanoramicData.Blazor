using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>Tests the optional badge shown after a <see cref="PDButton"/>'s text.</summary>
public partial class PDButtonTests
{
	/// <summary>A button given no badge text renders no badge.</summary>
	[Fact]
	public void No_badge_by_default()
	{
		var component = Render<PDButton>(parameters => parameters.Add(p => p.Text, "Save"));

		component.FindAll(".pd-button-badge").Should().BeEmpty();
	}

	/// <summary>Blank badge text renders no badge.</summary>
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Blank_badge_text_renders_no_badge(string badgeText)
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Text, "Save")
			.Add(p => p.BadgeText, badgeText));

		component.FindAll(".pd-button-badge").Should().BeEmpty();
	}

	/// <summary>The badge follows the text, with its own classes and the extra ones given.</summary>
	[Fact]
	public void Badge_follows_the_text_with_its_classes()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Text, "Variables")
			.Add(p => p.IconCssClass, "fas fa-list")
			.Add(p => p.BadgeText, "7")
			.Add(p => p.BadgeCssClass, "bg-success"));

		var badge = component.Find("button > span.pd-button-badge");
		badge.TextContent.Should().Be("7");
		badge.ClassList.Should().Contain("bg-success");
		badge.PreviousElementSibling!.TextContent.Should().Be("Variables");
		badge.NextElementSibling.Should().BeNull();
	}

	/// <summary>A button rendered as a link shows the badge too.</summary>
	[Fact]
	public void Url_anchor_shows_the_badge()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Url, "/x")
			.Add(p => p.Text, "Go")
			.Add(p => p.BadgeText, "3"));

		component.Find("a > span.pd-button-badge").TextContent.Should().Be("3");
	}

	/// <summary>Child content replaces the badge, as it does the text and icon.</summary>
	[Fact]
	public void Child_content_replaces_the_badge()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.BadgeText, "7")
			.Add(p => p.ChildContent, (RenderFragment)(b => b.AddContent(0, "Custom"))));

		component.FindAll(".pd-button-badge").Should().BeEmpty();
	}
}
