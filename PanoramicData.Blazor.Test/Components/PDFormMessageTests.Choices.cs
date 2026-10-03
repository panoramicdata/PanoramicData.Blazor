using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Single and multiple choice question tests for <see cref="PDFormMessage"/>.
/// </summary>
public partial class PDFormMessageTests
{
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
}
