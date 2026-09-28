using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDToolbarPlaceholder"/>: a toolbar slot that shows its content only while visible.
/// </summary>
public class PDToolbarPlaceholderTests : BunitContext
{
	/// <summary>
	/// Verifies that visible content is rendered inside the toolbar item with the extra item classes.
	/// </summary>
	[Fact]
	public void Visible_RendersTheContentWithTheItemClasses()
	{
		var component = Render<PDToolbarPlaceholder>(parameters => parameters
			.Add(p => p.ItemCssClass, "my-item")
			.AddChildContent("<span class=\"inner\">Hello</span>"));

		var item = component.Find("div.pdtoolbaritem.pdtoolbarplaceholder");
		item.ClassList.Should().Contain("my-item").And.NotContain("align-right");
		component.Find(".inner").TextContent.Should().Be("Hello");
	}

	/// <summary>
	/// Verifies that a hidden placeholder keeps its slot but renders none of its content.
	/// </summary>
	[Fact]
	public void Hidden_KeepsTheSlotButHidesTheContent()
	{
		var component = Render<PDToolbarPlaceholder>(parameters => parameters
			.Add(p => p.IsVisible, false)
			.AddChildContent("<span class=\"inner\">Hello</span>"));

		component.FindAll("div.pdtoolbarplaceholder").Should().ContainSingle();
		component.FindAll(".inner").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that shifting right adds the alignment class.
	/// </summary>
	[Fact]
	public void ShiftRight_AddsTheAlignRightClass()
	{
		var component = Render<PDToolbarPlaceholder>(parameters => parameters.Add(p => p.ShiftRight, true));

		component.Find("div.pdtoolbarplaceholder").ClassList.Should().Contain("align-right");
	}
}
