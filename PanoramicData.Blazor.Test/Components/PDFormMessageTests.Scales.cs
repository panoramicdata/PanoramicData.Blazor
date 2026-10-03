using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Scale question tests for <see cref="PDFormMessage"/>.
/// </summary>
public partial class PDFormMessageTests
{
	/// <summary>A labelled scale shows the label for the value and reports it, with the scale described.</summary>
	[Fact]
	public void A_labelled_scale_reports_the_label()
	{
		var scale = new ChatFormScale { Minimum = 1, Maximum = 3, MinimumLabel = "Low", MaximumLabel = "High", PointLabels = ["Disagree", "Neutral", "Agree"] };
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Scale, extras: new() { Scale = scale }));
		var slider = component.Find("input[type=range]");
		slider.GetAttribute("value").Should().Be("2");
		component.Find(".pdchat-form-scale-value").TextContent.Trim().Should().Be("not answered");

		slider.Input("3");

		component.Find(".pdchat-form-scale-value").TextContent.Trim().Should().Be("Agree");
		var answer = Submit(component);
		answer.Value.Should().Be("Agree");
		answer.ScaleDescription.Should().Be("1 = Disagree, 2 = Neutral, 3 = Agree (chosen: 3)", "the description records the number chosen, not its label (#185)");
	}

	/// <summary>An unlabelled scale reports the number and describes the ends of the scale.</summary>
	[Fact]
	public void An_unlabelled_scale_reports_the_number()
	{
		var scale = new ChatFormScale { Minimum = 0, Maximum = 10, MinimumLabel = "Never", MaximumLabel = "Always" };
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Scale, extras: new() { Scale = scale }));

		component.Find("input[type=range]").Input("7");

		component.Find(".pdchat-form-scale-value").TextContent.Trim().Should().Be("7");
		var answer = Submit(component);
		answer.Value.Should().Be("7");
		answer.ScaleDescription.Should().Be("0 = Never, 10 = Always (chosen: 7)");
	}

	/// <summary>A scale left untouched is skipped, and its description has no chosen value.</summary>
	[Fact]
	public void An_untouched_scale_is_skipped()
	{
		var scale = new ChatFormScale { Minimum = 1, Maximum = 5, MinimumLabel = "Low", MaximumLabel = "High" };
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Scale, extras: new() { Scale = scale }));

		var answer = Submit(component);

		answer.WasSkipped.Should().BeTrue();
		answer.ScaleDescription.Should().Be("1 = Low, 5 = High");
	}

	/// <summary>A scale with an unusable range explains that instead of showing a slider.</summary>
	[Fact]
	public void An_invalid_scale_is_explained()
	{
		var scale = new ChatFormScale { Minimum = 5, Maximum = 5, MinimumLabel = "a", MaximumLabel = "b" };
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Scale, extras: new() { Scale = scale }));

		component.FindAll("input[type=range]").Should().BeEmpty();
		component.Find(".pdchat-form-invalid").TextContent.Should().Contain("not a usable range");
	}

	/// <summary>DescribeScale falls back to the end labels when the point labels do not fit the range.</summary>
	[Fact]
	public void DescribeScale_needs_one_label_per_point()
	{
		var scale = new ChatFormScale { Minimum = 1, Maximum = 3, MinimumLabel = "Low", MaximumLabel = "High", PointLabels = ["Only one"] };

		PDFormMessage.DescribeScale(scale, null).Should().Be("1 = Low, 3 = High");
		PDFormMessage.DescribeScale(scale, "2").Should().Be("1 = Low, 3 = High (chosen: 2)");
	}
}
