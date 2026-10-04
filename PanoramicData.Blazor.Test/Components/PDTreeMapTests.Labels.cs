using AwesomeAssertions;
using Bunit;
using System.Globalization;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Label, tooltip and text tests for <see cref="PDTreeMap{TItem}"/>.
/// </summary>
public partial class PDTreeMapTests
{
	/// <summary>
	/// Verifies that the text and tooltip selectors are used for labels and hover text.
	/// </summary>
	[Fact]
	public void Text_and_tooltip_selectors_are_used()
	{
		var component = RenderMap(
			p => p.Add(x => x.TooltipSelector, n => $"tip {n.Name}"),
			textSelector: n => $"[{n.Name}]");

		var node = component.FindAll("g.pdtm-node")[IndexOf(component, _b)];
		node.QuerySelector("title")!.TextContent.Should().Be("tip B");
		component.FindAll(".pdtm-label").Select(l => l.TextContent).Should().Contain("[B]");
	}

	/// <summary>
	/// Verifies that labels are positioned over their rectangles in percentages of the canvas.
	/// </summary>
	[Fact]
	public void Labels_are_positioned_in_percentages_of_the_canvas()
	{
		var component = RenderMap(p => p.Add(x => x.MaxRenderDepth, 1));
		var rect = component.Instance.Rectangles.Single(r => r.Item == _b);

		var label = component.FindAll(".pdtm-label").Single(l => l.TextContent == "B");

		var expectedLeft = (rect.X / 400 * 100).ToString("0.###", CultureInfo.InvariantCulture);
		label.GetAttribute("style").Should().StartWith($"left:{expectedLeft}%;")
			.And.EndWith("padding:4px;font-size:11px");
	}

	/// <summary>
	/// Verifies that no label is drawn on a rectangle narrower than the minimum label size.
	/// </summary>
	[Fact]
	public void Rectangles_below_the_minimum_label_size_have_no_label()
	{
		var component = RenderMap(minLabelPx: 1000);

		component.FindAll(".pdtm-label").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a throwing text selector is reported and an empty label used.
	/// </summary>
	[Fact]
	public void A_throwing_text_selector_is_reported()
	{
		var component = RenderMap(textSelector: _ => throw new InvalidOperationException("text"));

		component.FindAll(".pdtm-label").Should().OnlyContain(l => l.TextContent.Length == 0);
		_errors.Should().NotBeEmpty().And.OnlyContain(e => e.Message == "text");
	}
}
