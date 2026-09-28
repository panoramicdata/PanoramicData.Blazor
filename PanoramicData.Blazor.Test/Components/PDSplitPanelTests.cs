using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDSplitPanel"/> registers itself with its parent <see cref="PDSplitter"/>, which then
/// renders a panel element for it.
/// </summary>
public class PDSplitPanelTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDSplitPanelTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Each panel is given a unique id and the splitter renders one element per panel, carrying the panel's
	/// id, CSS class and child content.
	/// </summary>
	[Fact]
	public void Panels_register_with_the_splitter_under_unique_ids()
	{
		var splitter = Render<PDSplitter>(parameters => parameters
			.Add(p => p.ChildContent, Panels(("left", "Left content"), ("right", "Right content"))));

		var panels = splitter.FindComponents<PDSplitPanel>().Select(c => c.Instance).ToList();
		panels.Should().HaveCount(2);
		panels.Select(p => p.Id).Should().OnlyHaveUniqueItems().And.AllSatisfy(id => id.Should().StartWith("pdsp"));

		var elements = splitter.FindAll("div.pdsplitpanel");
		elements.Select(e => e.Id).Should().Equal(panels.Select(p => p.Id));
		elements[0].ClassList.Should().Contain("left");
		elements[0].TextContent.Should().Contain("Left content");
		elements[1].ClassList.Should().Contain("right");
		elements[1].TextContent.Should().Contain("Right content");
	}

	/// <summary>The size parameters default to 1 and 100, and keep the values they are given.</summary>
	[Fact]
	public void Size_and_min_size_have_defaults_and_take_parameters()
	{
		var splitter = Render<PDSplitter>(parameters => parameters
			.Add(p => p.ChildContent, builder =>
			{
				builder.OpenComponent<PDSplitPanel>(0);
				builder.CloseComponent();
				builder.OpenComponent<PDSplitPanel>(1);
				builder.AddComponentParameter(2, nameof(PDSplitPanel.Size), 3);
				builder.AddComponentParameter(3, nameof(PDSplitPanel.MinSize), 50);
				builder.CloseComponent();
			}));

		var panels = splitter.FindComponents<PDSplitPanel>().Select(c => c.Instance).ToList();
		panels[0].Size.Should().Be(1);
		panels[0].MinSize.Should().Be(100);
		panels[0].CssClass.Should().BeEmpty();
		panels[1].Size.Should().Be(3);
		panels[1].MinSize.Should().Be(50);
	}

	private static RenderFragment Panels(params (string CssClass, string Text)[] panels) => builder =>
	{
		var sequence = 0;
		foreach (var (cssClass, text) in panels)
		{
			builder.OpenComponent<PDSplitPanel>(sequence++);
			builder.AddComponentParameter(sequence++, nameof(PDSplitPanel.CssClass), cssClass);
			builder.AddComponentParameter(sequence++, nameof(PDSplitPanel.ChildContent), (RenderFragment)(b => b.AddContent(0, text)));
			builder.CloseComponent();
		}
	};
}
