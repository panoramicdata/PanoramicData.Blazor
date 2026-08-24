namespace PanoramicData.Blazor.Models;

/// <summary>
/// A short series of questions asked inline in a chat, and answered without leaving it (issue #106).
/// </summary>
/// <remarks>
/// <para>
/// Deliberately free of any knowledge of who is asking. The first consumer is an AI assistant
/// composing questions on the fly, but nothing here assumes that: a form is a form, and a human, a
/// workflow or a support macro could produce one just as well.
/// </para>
/// <para>
/// <b>Every question is individually optional, and the form as a whole is optional.</b> That is a
/// deliberate stance rather than a default: a form that interrupts a conversation and then refuses
/// to go away until it is filled in is worse than no form. Skipping is recorded explicitly in
/// <see cref="ChatFormAnswer.WasSkipped"/> rather than being inferred from a missing entry, so the
/// consumer can tell "declined to answer" from "never asked".
/// </para>
/// </remarks>
public class ChatForm
{
	/// <summary>
	/// Identifies this form, so a submission can be correlated back to the questions that were asked.
	/// </summary>
	/// <remarks>
	/// Correlation matters because the answers arrive as a separate message, potentially after other
	/// traffic. Without an id the consumer would have to match answers to questions by position and
	/// hope, which fails the moment two forms are in flight.
	/// </remarks>
	public required Guid Id { get; init; }

	/// <summary>
	/// Optional heading shown above the tab strip.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	/// The questions, in the order their tabs appear.
	/// </summary>
	public required IReadOnlyList<ChatFormQuestion> Questions { get; init; }

	/// <summary>
	/// Label for the submit control.
	/// </summary>
	public string SubmitLabel { get; set; } = "Submit answers";
}

/// <summary>
/// One question, occupying one tab.
/// </summary>
public class ChatFormQuestion
{
	/// <summary>
	/// Identifies the question within its form, so an answer names what it is answering.
	/// </summary>
	public required string Id { get; init; }

	/// <summary>
	/// The short label on this question's tab.
	/// </summary>
	/// <remarks>
	/// Separate from <see cref="Question"/> because a tab strip has very little room: the full
	/// question is a sentence, the tab is two or three words. Keeping them distinct means neither
	/// has to be a compromise, and the renderer never has to guess where to truncate.
	/// </remarks>
	public required string Header { get; init; }

	/// <summary>
	/// The question itself, shown when its tab is active.
	/// </summary>
	public required string Question { get; init; }

	/// <summary>
	/// What kind of answer is expected.
	/// </summary>
	public required ChatFormAnswerKind Kind { get; init; }

	/// <summary>
	/// The choices, for <see cref="ChatFormAnswerKind.SingleChoice"/> and
	/// <see cref="ChatFormAnswerKind.MultipleChoice"/>.
	/// </summary>
	public IReadOnlyList<ChatFormOption> Options { get; init; } = [];

	/// <summary>
	/// Whether a final "Other" choice is offered, revealing a free-text box.
	/// </summary>
	/// <remarks>
	/// Worth having on most choice questions: a fixed list is an assumption about the answer space,
	/// and "Other" is how the asker finds out the assumption was wrong.
	/// </remarks>
	public bool AllowOther { get; init; }

	/// <summary>
	/// The range and its end labels, for <see cref="ChatFormAnswerKind.Scale"/>.
	/// </summary>
	public ChatFormScale? Scale { get; init; }

	/// <summary>
	/// Whether a text answer gets a multi-line box.
	/// </summary>
	public bool IsMultiline { get; init; }

	/// <summary>
	/// Text to pre-fill a text answer with, for the user to edit.
	/// </summary>
	/// <remarks>
	/// "Here is a draft, change what is wrong" asks far less of someone than "write this", and it
	/// gets a better answer. Submitting the suggestion unchanged still counts as answered - accepting
	/// a draft is a decision, not an absence of one.
	/// </remarks>
	public string? SuggestedValue { get; init; }
}

/// <summary>
/// The kinds of answer a question can take.
/// </summary>
public enum ChatFormAnswerKind
{
	/// <summary>
	/// Exactly one of a fixed set of choices, optionally with "Other".
	/// </summary>
	/// <remarks>
	/// "Which is your favourite?" - the choices are alternatives and picking one rules out the rest.
	/// </remarks>
	SingleChoice = 0,

	/// <summary>
	/// An integer on a labelled range.
	/// </summary>
	Scale = 1,

	/// <summary>
	/// Free text, single or multi-line, optionally pre-filled.
	/// </summary>
	Text = 2,

	/// <summary>
	/// Any number of a fixed set of choices, optionally with "Other".
	/// </summary>
	/// <remarks>
	/// "Which do you like?" - a genuinely different question from the single-choice form, and
	/// conflating the two is how a survey ends up unable to distinguish "no strong preference" from
	/// "likes several". Kept as a separate kind rather than a flag on SingleChoice so a renderer
	/// cannot accidentally show radios where checkboxes were meant.
	/// </remarks>
	MultipleChoice = 3
}

/// <summary>
/// One choice on a single- or multiple-choice question.
/// </summary>
public class ChatFormOption
{
	/// <summary>
	/// The choice itself - short, and what gets recorded as the answer.
	/// </summary>
	public required string Label { get; init; }

	/// <summary>
	/// What picking this choice means, shown under the label.
	/// </summary>
	/// <remarks>
	/// Optional, but the reason the choices are worth reading: a bare list of labels makes the reader
	/// guess at the trade-off, which is the thing they are actually being asked about.
	/// </remarks>
	public string? Description { get; init; }
}

/// <summary>
/// An integer range with its ends named.
/// </summary>
/// <remarks>
/// The labels are not decoration. A bare "3" is meaningless later - three out of what, and which end
/// is good? Recording the ends alongside the value is what makes the answer readable a month later.
/// </remarks>
public class ChatFormScale
{
	/// <summary>
	/// Lowest selectable value. Typically 0 or 1.
	/// </summary>
	public required int Minimum { get; init; }

	/// <summary>
	/// Highest selectable value. Typically 1, 4 or 5.
	/// </summary>
	public required int Maximum { get; init; }

	/// <summary>
	/// What the low end means - "Strongly disagree", "No".
	/// </summary>
	public required string MinimumLabel { get; init; }

	/// <summary>
	/// What the high end means - "Strongly agree", "Yes".
	/// </summary>
	public required string MaximumLabel { get; init; }

	/// <summary>
	/// A label for every point on the scale, low to high.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Optional. With it, a 1..4 agreement scale reads "Strongly agree, Agree, Disagree, Strongly
	/// disagree" rather than "1, 2, 3, 4" - and, more importantly, the recorded answer reads
	/// "Agree" instead of "2", which is the difference between a transcript that means something
	/// later and one that does not.
	/// </para>
	/// <para>
	/// Must contain exactly one entry per point, or it is ignored: a mislabelled scale is worse than
	/// an unlabelled one, because the reader has no way to tell it is wrong.
	/// </para>
	/// </remarks>
	public IReadOnlyList<string>? PointLabels { get; init; }

	/// <summary>
	/// The label for one point, or null when the scale is not labelled point by point.
	/// </summary>
	public string? LabelFor(int value)
	{
		if (PointLabels is null || !HasUsablePointLabels || value < Minimum || value > Maximum)
		{
			return null;
		}

		return PointLabels[value - Minimum];
	}

	/// <summary>
	/// Whether there is exactly one label per point.
	/// </summary>
	public bool HasUsablePointLabels
		=> PointLabels is not null && PointLabels.Count == Maximum - Minimum + 1;

	/// <summary>
	/// Whether the range is usable.
	/// </summary>
	/// <remarks>
	/// A renderer checks this rather than trusting the values: an inverted or single-point range
	/// would otherwise produce either an empty control or an enormous one, and a form that asks a
	/// question nobody can answer is worse than one that quietly omits it.
	/// </remarks>
	public bool IsValid => Maximum > Minimum && Maximum - Minimum <= 10;
}

/// <summary>
/// One answer, or an explicit record that the question was skipped.
/// </summary>
public class ChatFormAnswer
{
	/// <summary>
	/// The <see cref="ChatFormQuestion.Id"/> this answers.
	/// </summary>
	public required string QuestionId { get; init; }

	/// <summary>
	/// The question as asked, carried alongside the answer.
	/// </summary>
	/// <remarks>
	/// Redundant against the form, and deliberately so: a submission travels separately from the
	/// questions, and an answer that cannot be read without fetching something else is a trap for
	/// whatever consumes it later.
	/// </remarks>
	public required string Question { get; init; }

	/// <summary>
	/// The answer in readable form - the chosen label, the scale value, the text entered, or the
	/// selected labels joined together.
	/// </summary>
	/// <remarks>
	/// Always populated when the question was answered, so a consumer that only wants to read the
	/// answer never has to branch on the question kind.
	/// </remarks>
	public string? Value { get; init; }

	/// <summary>
	/// The individually selected labels, for <see cref="ChatFormAnswerKind.MultipleChoice"/>.
	/// </summary>
	/// <remarks>
	/// Null for every other kind. Present alongside <see cref="Value"/> rather than instead of it
	/// because the two serve different readers: the joined string is for a human or a language
	/// model, the list is for anything that needs to count or group the selections.
	/// </remarks>
	public IReadOnlyList<string>? Values { get; init; }

	/// <summary>
	/// What the user typed when they chose "Other".
	/// </summary>
	/// <remarks>
	/// Held separately from <see cref="Value"/> because on a multiple-choice question "Other" sits
	/// alongside real selections rather than replacing them - someone can like vanilla, pistachio
	/// and one more the list did not offer.
	/// </remarks>
	public string? OtherText { get; init; }

	/// <summary>
	/// For a scale, the end labels, so the value can be read without the form.
	/// </summary>
	public string? ScaleDescription { get; init; }

	/// <summary>
	/// Whether the user chose "Other" and typed their own answer.
	/// </summary>
	/// <remarks>
	/// Distinguished from an ordinary choice because it means something different: the offered
	/// options did not fit, which is a signal about the question rather than only about the answer.
	/// </remarks>
	public bool WasOther { get; init; }

	/// <summary>
	/// Whether the user left this question unanswered.
	/// </summary>
	public bool WasSkipped { get; init; }
}

/// <summary>
/// A completed form on its way back to whoever asked.
/// </summary>
public class ChatFormSubmission
{
	/// <summary>
	/// The <see cref="ChatForm.Id"/> being answered.
	/// </summary>
	public required Guid FormId { get; init; }

	/// <summary>
	/// One entry per question, including the skipped ones.
	/// </summary>
	public required IReadOnlyList<ChatFormAnswer> Answers { get; init; }
}
