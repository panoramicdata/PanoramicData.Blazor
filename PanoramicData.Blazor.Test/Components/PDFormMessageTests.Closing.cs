using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDFormMessage"/> reports a submit or a dismiss and then closes.
/// </summary>
public partial class PDFormMessageTests
{
	/// <summary>Submitting with nothing answered reports every question as skipped and closes the form.</summary>
	[Fact]
	public void Submitting_unanswered_reports_skips_and_closes()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Text), Q("q2", ChatFormAnswerKind.Ranking, Colours));

		Submit(component);

		_submission!.Answers.Should().AllSatisfy(a => a.WasSkipped.Should().BeTrue());
		_submission.Answers.Select(a => a.QuestionId).Should().Equal("q1", "q2");
		component.Find(".pdchat-form-done").TextContent.Should().Be("Thanks - your answers have been sent.");
		component.FindAll(".pdchat-form-tab").Should().AllSatisfy(t => t.HasAttribute("disabled").Should().BeTrue());
		component.FindAll(".pdchat-form-dismiss").Should().BeEmpty();
	}

	/// <summary>Dismissing reports the form id, sends no answers and closes the form.</summary>
	[Fact]
	public void Dismissing_reports_the_form_and_closes()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Text));

		component.Find(".pdchat-form-dismiss").Click();

		_dismissed.Should().NotBeNull();
		_submission.Should().BeNull();
		component.Find(".pdchat-form-done").TextContent.Should().Be("No problem - these questions were dismissed.");
	}
}
