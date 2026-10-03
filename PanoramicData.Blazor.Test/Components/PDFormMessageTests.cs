using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDFormMessage"/> shows one question per tab, records each kind of answer, reports
/// unanswered questions as skipped, and closes after a submit or a dismiss.
/// </summary>
public class PDFormMessageTests : BunitContext
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

	/// <summary>A single choice records the chosen label, and choosing another replaces it.</summary>
	[Fact]
	public void Single_choice_records_one_label()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.SingleChoice, Colours));

		component.FindAll("input[type=radio]")[0].Change(true);
		component.FindAll("input[type=radio]")[1].Change(true);

		component.FindAll(".pdchat-form-option")[1].ClassList.Should().Contain("selected");
		component.Find(".pdchat-form-option-description").TextContent.Should().Be("Warm");
		component.Find(".pdchat-form-tab").ClassList.Should().Contain("answered");
		component.Find(".pdchat-form-hint").TextContent.Should().Be("All answered");
		Submit(component).Value.Should().Be("Blue");
	}

	/// <summary>"Other" on a single choice replaces the chosen label with the text typed.</summary>
	[Fact]
	public void Single_choice_other_uses_the_typed_text()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.SingleChoice, Colours, new() { AllowOther = true }));

		component.FindAll("input[type=radio]")[0].Change(true);
		component.FindAll("input[type=radio]")[3].Change(true);
		component.Find("input[placeholder='Tell us more']").Input("Purple");

		var answer = Submit(component);
		answer.Value.Should().Be("Purple");
		answer.WasOther.Should().BeTrue();
		answer.OtherText.Should().Be("Purple");
	}

	/// <summary>"Other" chosen with nothing typed is not an answer.</summary>
	[Fact]
	public void Other_without_text_is_a_skip()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.SingleChoice, Colours, new() { AllowOther = true }));

		component.FindAll("input[type=radio]")[3].Change(true);

		Submit(component).WasSkipped.Should().BeTrue();
	}

	/// <summary>Multiple choices are reported in the question's order, with "Other" appended.</summary>
	[Fact]
	public void Multiple_choice_reports_in_question_order_with_other()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.MultipleChoice, Colours, new() { AllowOther = true }));
		var boxes = component.FindAll("input[type=checkbox]");

		boxes[2].Change(true);
		component.FindAll("input[type=checkbox]")[0].Change(true);
		component.FindAll("input[type=checkbox]")[1].Change(true);
		component.FindAll("input[type=checkbox]")[1].Change(false);
		component.FindAll("input[type=checkbox]")[3].Change(true);
		component.Find("input[placeholder='Tell us more']").Input("Pink");

		var answer = Submit(component);
		answer.Value.Should().Be("Red, Green, Other: Pink");
		answer.Values.Should().Equal("Red", "Green");
		answer.WasOther.Should().BeTrue();
	}

	/// <summary>Unticking "Other" on a multiple choice drops its text, and nothing chosen is a skip.</summary>
	[Fact]
	public void Multiple_choice_unticking_other_drops_it()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.MultipleChoice, Colours, new() { AllowOther = true }));

		component.FindAll("input[type=checkbox]")[3].Change(true);
		component.Find("input[placeholder='Tell us more']").Input("Pink");
		component.FindAll("input[type=checkbox]")[3].Change(false);

		component.FindAll("input[placeholder='Tell us more']").Should().BeEmpty();
		var answer = Submit(component);
		answer.WasSkipped.Should().BeTrue();
		answer.Values.Should().BeNull();
	}

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

	/// <summary>A date question uses a date input, or a date-time input when time is included.</summary>
	[Theory]
	[InlineData(false, "date")]
	[InlineData(true, "datetime-local")]
	public void Date_questions_use_the_right_input(bool includeTime, string type)
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.DateTime, extras: new() { IncludeTime = includeTime }));
		var input = component.Find("input.pdchat-form-text");
		input.GetAttribute("type").Should().Be(type);

		input.Input("2026-09-28");

		Submit(component).Value.Should().Be("2026-09-28");
	}

	/// <summary>A number question shows its unit and reports the number with it.</summary>
	[Fact]
	public void A_number_is_reported_with_its_unit()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Number, extras: new() { Number = new ChatFormNumber { Minimum = 0, Maximum = 100, Step = 5, Unit = "kg" } }));
		var input = component.Find("input[type=number]");
		input.GetAttribute("step").Should().Be("5");
		component.Find(".pdchat-form-unit").TextContent.Should().Be("kg");

		input.Input("20");

		Submit(component).Value.Should().Be("20 kg");
	}

	/// <summary>A number with no unit is reported as typed.</summary>
	[Fact]
	public void A_number_without_a_unit_is_reported_as_typed()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Number));

		component.FindAll(".pdchat-form-unit").Should().BeEmpty();
		component.Find("input[type=number]").Input("3");

		Submit(component).Value.Should().Be("3");
	}

	/// <summary>Moving options in a ranking records the order, with the end buttons disabled.</summary>
	[Fact]
	public void A_ranking_records_the_arranged_order()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Ranking, Colours));
		var buttons = component.FindAll(".pdchat-form-rank-buttons button");
		buttons[0].HasAttribute("disabled").Should().BeTrue();
		buttons[^1].HasAttribute("disabled").Should().BeTrue();

		component.FindAll("button[title='Move down']")[0].Click();
		component.FindAll("button[title='Move up']")[2].Click();

		component.FindAll(".pdchat-form-ranking .pdchat-form-option-label").Select(e => e.TextContent)
			.Should().Equal("Blue", "Green", "Red");
		var answer = Submit(component);
		answer.Value.Should().Be("1. Blue, 2. Green, 3. Red");
		answer.Values.Should().Equal("Blue", "Green", "Red");
	}

	/// <summary>Moving the first option up, or the last down, changes nothing and leaves the ranking unanswered.</summary>
	[Fact]
	public void A_ranking_cannot_move_past_its_ends()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Ranking, Colours));

		component.FindAll("button[title='Move up']")[0].Click();
		component.FindAll("button[title='Move down']")[^1].Click();

		component.FindAll(".pdchat-form-ranking .pdchat-form-option-label").Select(e => e.TextContent)
			.Should().Equal("Red", "Blue", "Green");
		Submit(component).WasSkipped.Should().BeTrue();
	}

	/// <summary>An acknowledgement uses its first option as the label, and unticking it makes it a skip.</summary>
	[Fact]
	public void An_acknowledgement_can_be_given_and_withdrawn()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Acknowledgement, [new() { Label = "I agree" }]), Q("q2", ChatFormAnswerKind.Acknowledgement));
		component.Find(".pdchat-form-option-label").TextContent.Should().Be("I agree");

		component.Find("input[type=checkbox]").Change(true);
		component.Find(".pdchat-form-option").ClassList.Should().Contain("selected");
		component.Find("input[type=checkbox]").Change(false);
		component.Find("input[type=checkbox]").Change(true);
		component.FindAll(".pdchat-form-tab")[1].Click();
		component.Find(".pdchat-form-option-label").TextContent.Should().Be("I understand");
		component.Find("input[type=checkbox]").Change(true);
		component.Find("input[type=checkbox]").Change(false);

		Submit(component);
		_submission!.Answers[0].Value.Should().Be("Acknowledged");
		_submission.Answers[1].WasSkipped.Should().BeTrue();
	}

	/// <summary>A suggested text answer is shown and, untouched, is reported as the answer.</summary>
	[Fact]
	public void A_suggested_text_is_the_answer_when_untouched()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Text, extras: new() { Suggested = "Draft reply" }));
		component.Find("input.pdchat-form-text").GetAttribute("value").Should().Be("Draft reply");
		component.Find(".pdchat-form-tab").ClassList.Should().Contain("answered");

		Submit(component).Value.Should().Be("Draft reply");
	}

	/// <summary>A multiline text question uses a text area and reports what was typed.</summary>
	[Fact]
	public void Multiline_text_uses_a_text_area()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Text, extras: new() { Multiline = true }));

		component.Find("textarea").Input("Line one");

		Submit(component).Value.Should().Be("Line one");
	}

	/// <summary>Clearing a typed answer makes it a skip.</summary>
	[Fact]
	public void Clearing_typed_text_is_a_skip()
	{
		var component = RenderForm(null, Q("q1", ChatFormAnswerKind.Text));

		component.Find("input.pdchat-form-text").Input("x");
		component.Find("input.pdchat-form-text").Input(string.Empty);

		Submit(component).WasSkipped.Should().BeTrue();
	}

	/// <summary>DescribeScale falls back to the end labels when the point labels do not fit the range.</summary>
	[Fact]
	public void DescribeScale_needs_one_label_per_point()
	{
		var scale = new ChatFormScale { Minimum = 1, Maximum = 3, MinimumLabel = "Low", MaximumLabel = "High", PointLabels = ["Only one"] };

		PDFormMessage.DescribeScale(scale, null).Should().Be("1 = Low, 3 = High");
		PDFormMessage.DescribeScale(scale, "2").Should().Be("1 = Low, 3 = High (chosen: 2)");
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
	private sealed record QuestionExtras
	{
		/// <summary>Gets whether a final "Other" choice is offered.</summary>
		public bool AllowOther { get; init; }

		/// <summary>Gets the scale, for a scale question.</summary>
		public ChatFormScale? Scale { get; init; }

		/// <summary>Gets the number settings, for a number question.</summary>
		public ChatFormNumber? Number { get; init; }

		/// <summary>Gets whether a date question also asks for a time.</summary>
		public bool IncludeTime { get; init; }

		/// <summary>Gets whether a text answer is multi-line.</summary>
		public bool Multiline { get; init; }

		/// <summary>Gets the suggested answer.</summary>
		public string? Suggested { get; init; }
	}
}
