using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Date, number, ranking, acknowledgement and text question tests for <see cref="PDFormMessage"/>.
/// </summary>
public partial class PDFormMessageTests
{
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
}
