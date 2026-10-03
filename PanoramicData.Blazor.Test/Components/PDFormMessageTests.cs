using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDFormMessage"/> shows one question per tab, records each kind of answer, reports
/// unanswered questions as skipped, and closes after a submit or a dismiss.
/// </summary>
public partial class PDFormMessageTests : BunitContext
{
	private static readonly ChatFormOption[] Colours = [new() { Label = "Red", Description = "Warm" }, new() { Label = "Blue" }, new() { Label = "Green" }];

	private ChatFormSubmission? _submission;
	private Guid? _dismissed;

	/// <summary>Nothing renders without a form or with a form that asks nothing.</summary>
	[Fact]
	public void No_form_or_no_questions_renders_nothing()
	{
		Render<PDFormMessage>().Markup.Trim().Should().BeEmpty();
		Render<PDFormMessage>(p => p.Add(x => x.Form, new ChatForm { Id = Guid.NewGuid(), Questions = [] })).Markup.Trim().Should().BeEmpty();
	}

	/// <summary>Each question gets a tab, the first is active, the title shows on it and Next moves on.</summary>
	[Fact]
	public void Tabs_title_and_next()
	{
		var component = RenderForm("A title", Q("q1", ChatFormAnswerKind.Text), Q("q2", ChatFormAnswerKind.Text));

		var tabs = component.FindAll(".pdchat-form-tab");
		tabs.Select(t => t.TextContent.Trim()).Should().Equal("Header q1", "Header q2");
		tabs[0].ClassList.Should().Contain("active");
		component.Find(".pdchat-form-title").TextContent.Should().Be("A title");
		component.Find(".pdchat-form-hint").TextContent.Should().Be("Question 1 of 2 - or pick a tab to jump");
		component.Find(".pdchat-form-submit").TextContent.Trim().Should().Be("Next");

		component.Find(".pdchat-form-submit").Click();

		component.Find(".pdchat-form-question").TextContent.Should().Be("Question q2?");
		component.FindAll(".pdchat-form-title").Should().BeEmpty();
		component.Find(".pdchat-form-submit").TextContent.Trim().Should().Be("Submit answers");
		component.Find(".pdchat-form-hint").TextContent.Should().Be("0 of 2 answered - the rest will be reported as skipped");
	}

	/// <summary>Clicking a tab jumps to its question.</summary>
	[Fact]
	public void Clicking_a_tab_jumps_to_it()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Text), Q("q2", ChatFormAnswerKind.Text));

		component.FindAll(".pdchat-form-tab")[1].Click();

		component.FindAll(".pdchat-form-tab")[1].ClassList.Should().Contain("active");
		component.Find(".pdchat-form-question").TextContent.Should().Be("Question q2?");
	}

	private IRenderedComponent<PDFormMessage> RenderForm(string? title, params ChatFormQuestion[] questions)
		=> Render<PDFormMessage>(parameters => parameters
			.Add(p => p.Form, new ChatForm { Id = Guid.NewGuid(), Title = title, Questions = questions })
			.Add(p => p.OnSubmitted, (ChatFormSubmission s) => _submission = s)
			.Add(p => p.OnDismissed, (Guid id) => _dismissed = id));

	/// <summary>Moves to the last tab and submits, returning the first answer.</summary>
	private ChatFormAnswer Submit(IRenderedComponent<PDFormMessage> component)
	{
		component.FindAll(".pdchat-form-tab")[^1].Click();
		component.Find(".pdchat-form-submit").Click();
		return _submission!.Answers[0];
	}

	private static ChatFormQuestion Q(string id, ChatFormAnswerKind kind, ChatFormOption[]? options = null, QuestionExtras? extras = null)
	{
		var settings = extras ?? new QuestionExtras();
		return new()
		{
			Id = id,
			Header = $"Header {id}",
			Question = $"Question {id}?",
			Kind = kind,
			Options = options ?? [],
			AllowOther = settings.AllowOther,
			Scale = settings.Scale,
			Number = settings.Number,
			IncludeTime = settings.IncludeTime,
			IsMultiline = settings.Multiline,
			SuggestedValue = settings.Suggested
		};
	}

	/// <summary>The less common settings of a question under test, each defaulting to off or absent.</summary>
	/// <param name="AllowOther">Whether a final "Other" choice is offered.</param>
	/// <param name="Scale">The scale, for a scale question.</param>
	/// <param name="Number">The number settings, for a number question.</param>
	/// <param name="IncludeTime">Whether a date question also asks for a time.</param>
	/// <param name="Multiline">Whether a text answer is multi-line.</param>
	/// <param name="Suggested">The suggested answer.</param>
	private sealed record QuestionExtras(
		bool AllowOther = false,
		ChatFormScale? Scale = null,
		ChatFormNumber? Number = null,
		bool IncludeTime = false,
		bool Multiline = false,
		string? Suggested = null);
}
