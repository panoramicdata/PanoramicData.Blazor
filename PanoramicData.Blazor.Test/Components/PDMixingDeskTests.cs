using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDMixingDesk"/> wraps its child content in the mixing desk container.
/// </summary>
public class PDMixingDeskTests : BunitContext
{
	/// <summary>Child content is rendered inside the container.</summary>
	[Fact]
	public void ChildContent_IsRenderedInsideTheContainer()
	{
		var component = Render<PDMixingDesk>(parameters => parameters
			.Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<span class=\"channel\">Ch 1</span>"))));

		var desk = component.Find("div.pd-mixing-desk");
		desk.QuerySelector("span.channel")!.TextContent.Should().Be("Ch 1");
	}

	/// <summary>With no child content the container is rendered empty.</summary>
	[Fact]
	public void NoChildContent_RendersAnEmptyContainer()
	{
		var component = Render<PDMixingDesk>();

		component.Find("div.pd-mixing-desk").ChildElementCount.Should().Be(0);
	}

	/// <summary>The parameter defaults are as documented.</summary>
	[Fact]
	public void Defaults_AreAsDocumented()
	{
		var component = Render<PDMixingDesk>();

		component.Instance.CssClass.Should().BeEmpty();
		component.Instance.MinHeight.Should().Be("600px");
	}

	/// <summary>Additional CSS classes are applied to the container (#164).</summary>
	[Fact]
	public void CssClass_IsAppliedToTheContainer()
	{
		var component = Render<PDMixingDesk>(parameters => parameters.Add(p => p.CssClass, "x"));

		var desk = component.Find("div.pd-mixing-desk");
		desk.ClassList.Should().Contain("x");
	}

	/// <summary>The minimum height, default or supplied, is applied to the container (#164).</summary>
	[Theory]
	[InlineData(null, "min-height: 600px")]
	[InlineData("250px", "min-height: 250px")]
	public void MinHeight_IsAppliedToTheContainer(string? minHeight, string expectedStyle)
	{
		var component = minHeight is null
			? Render<PDMixingDesk>()
			: Render<PDMixingDesk>(parameters => parameters.Add(p => p.MinHeight, minHeight));

		component.Find("div.pd-mixing-desk").GetAttribute("style").Should().Be(expectedStyle);
	}

	/// <summary>An empty minimum height writes no style at all (#164).</summary>
	[Fact]
	public void EmptyMinHeight_WritesNoStyle()
	{
		var component = Render<PDMixingDesk>(parameters => parameters.Add(p => p.MinHeight, string.Empty));

		component.Find("div.pd-mixing-desk").HasAttribute("style").Should().BeFalse();
	}
}