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
}
