using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ChatForm"/> and the question, option, scale, answer and number types it uses.</summary>
public class ChatFormTests
{
	private static ChatFormScale Scale(int min, int max, IReadOnlyList<string>? labels = null) => new()
	{
		Minimum = min,
		Maximum = max,
		MinimumLabel = "Low",
		MaximumLabel = "High",
		PointLabels = labels
	};

	/// <summary>A form has a default submit label, and its required members round-trip.</summary>
	[Fact]
	public void ChatForm_HasDefaultSubmitLabel()
	{
		var id = Guid.NewGuid();
		var question = new ChatFormQuestion { Id = "q", Header = "H", Question = "Why?", Kind = ChatFormAnswerKind.Text };

		var form = new ChatForm { Id = id, Questions = [question] };

		form.Id.Should().Be(id);
		form.Title.Should().BeNull();
		form.Questions.Should().ContainSingle().Which.Should().BeSameAs(question);
		form.SubmitLabel.Should().Be("Submit answers");

		form.Title = "Survey";
		form.SubmitLabel = "Send";
		form.Title.Should().Be("Survey");
		form.SubmitLabel.Should().Be("Send");
	}

	/// <summary>A question defaults to no options, no free text, a single line and no suggestion.</summary>
	[Fact]
	public void ChatFormQuestion_HasDocumentedDefaults()
	{
		var question = new ChatFormQuestion { Id = "q", Header = "H", Question = "Q", Kind = ChatFormAnswerKind.SingleChoice };

		question.Options.Should().BeEmpty();
		question.AllowOther.Should().BeFalse();
		question.Scale.Should().BeNull();
		question.IsMultiline.Should().BeFalse();
		question.Number.Should().BeNull();
		question.IncludeTime.Should().BeFalse();
		question.SuggestedValue.Should().BeNull();
	}

	/// <summary>A question's optional members are initialisable.</summary>
	[Fact]
	public void ChatFormQuestion_OptionalMembersRoundTrip()
	{
		var question = new ChatFormQuestion
		{
			Id = "when",
			Header = "When",
			Question = "When?",
			Kind = ChatFormAnswerKind.DateTime,
			Options = [new ChatFormOption { Label = "Now", Description = "Right now" }],
			AllowOther = true,
			Scale = Scale(1, 3),
			IsMultiline = true,
			Number = new ChatFormNumber { Minimum = 0 },
			IncludeTime = true,
			SuggestedValue = "today"
		};

		question.Id.Should().Be("when");
		question.Header.Should().Be("When");
		question.Question.Should().Be("When?");
		question.Kind.Should().Be(ChatFormAnswerKind.DateTime);
		question.Options.Should().ContainSingle().Which.Description.Should().Be("Right now");
		question.AllowOther.Should().BeTrue();
		question.Scale.Should().NotBeNull();
		question.IsMultiline.Should().BeTrue();
		question.Number!.Minimum.Should().Be(0);
		question.IncludeTime.Should().BeTrue();
		question.SuggestedValue.Should().Be("today");
	}

	/// <summary>An option's description is optional.</summary>
	[Fact]
	public void ChatFormOption_DescriptionIsOptional()
	{
		new ChatFormOption { Label = "Yes" }.Description.Should().BeNull();
	}

	/// <summary>A scale is valid with two to eleven points and an ascending range.</summary>
	[Theory]
	[InlineData(0, 1, true)]
	[InlineData(1, 11, true)]
	[InlineData(0, 11, false)]
	[InlineData(3, 3, false)]
	[InlineData(5, 1, false)]
	public void ChatFormScale_IsValid(int min, int max, bool expected)
	{
		Scale(min, max).IsValid.Should().Be(expected);
	}

	/// <summary>Point labels are usable only when there is exactly one per point.</summary>
	[Fact]
	public void ChatFormScale_HasUsablePointLabels_RequiresOneLabelPerPoint()
	{
		Scale(1, 3).HasUsablePointLabels.Should().BeFalse();
		Scale(1, 3, ["a", "b"]).HasUsablePointLabels.Should().BeFalse();
		Scale(1, 3, ["a", "b", "c"]).HasUsablePointLabels.Should().BeTrue();
	}

	/// <summary>LabelFor names each point on the scale, offset from the minimum.</summary>
	[Fact]
	public void ChatFormScale_LabelFor_ReturnsPointLabel()
	{
		var scale = Scale(1, 3, ["Bad", "OK", "Good"]);

		scale.LabelFor(1).Should().Be("Bad");
		scale.LabelFor(2).Should().Be("OK");
		scale.LabelFor(3).Should().Be("Good");
		scale.MinimumLabel.Should().Be("Low");
		scale.MaximumLabel.Should().Be("High");
	}

	/// <summary>LabelFor returns null outside the scale or when the labels cannot be used.</summary>
	[Fact]
	public void ChatFormScale_LabelFor_ReturnsNullWhenUnavailable()
	{
		var scale = Scale(1, 3, ["Bad", "OK", "Good"]);

		scale.LabelFor(0).Should().BeNull();
		scale.LabelFor(4).Should().BeNull();
		Scale(1, 3).LabelFor(2).Should().BeNull();
		Scale(1, 3, ["only one"]).LabelFor(1).Should().BeNull();
	}

	/// <summary>An answer defaults to answered, not other, with no value, and its members round-trip.</summary>
	[Fact]
	public void ChatFormAnswer_DefaultsAndRoundTrip()
	{
		var answer = new ChatFormAnswer { QuestionId = "q", Question = "Why?" };
		answer.Value.Should().BeNull();
		answer.Values.Should().BeNull();
		answer.OtherText.Should().BeNull();
		answer.ScaleDescription.Should().BeNull();
		answer.WasOther.Should().BeFalse();
		answer.WasSkipped.Should().BeFalse();

		var full = new ChatFormAnswer
		{
			QuestionId = "q",
			Question = "Why?",
			Value = "Because",
			Values = ["a", "b"],
			OtherText = "other",
			ScaleDescription = "Agree",
			WasOther = true,
			WasSkipped = true
		};

		full.QuestionId.Should().Be("q");
		full.Question.Should().Be("Why?");
		full.Value.Should().Be("Because");
		full.Values.Should().Equal("a", "b");
		full.OtherText.Should().Be("other");
		full.ScaleDescription.Should().Be("Agree");
		full.WasOther.Should().BeTrue();
		full.WasSkipped.Should().BeTrue();
	}

	/// <summary>A submission carries the form id and its answers.</summary>
	[Fact]
	public void ChatFormSubmission_CarriesFormIdAndAnswers()
	{
		var id = Guid.NewGuid();

		var submission = new ChatFormSubmission { FormId = id, Answers = [new ChatFormAnswer { QuestionId = "q", Question = "Q" }] };

		submission.FormId.Should().Be(id);
		submission.Answers.Should().ContainSingle();
	}

	/// <summary>A number question is unbounded with a step of one by default, and its members round-trip.</summary>
	[Fact]
	public void ChatFormNumber_DefaultsAndRoundTrip()
	{
		var number = new ChatFormNumber();
		number.Minimum.Should().BeNull();
		number.Maximum.Should().BeNull();
		number.Step.Should().Be(1);
		number.Unit.Should().BeNull();

		var bounded = new ChatFormNumber { Minimum = 1, Maximum = 10, Step = 0.5, Unit = "kg" };
		bounded.Minimum.Should().Be(1);
		bounded.Maximum.Should().Be(10);
		bounded.Step.Should().Be(0.5);
		bounded.Unit.Should().Be("kg");
	}
}
