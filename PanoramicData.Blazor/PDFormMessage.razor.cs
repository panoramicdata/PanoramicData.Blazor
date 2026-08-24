using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;
using System.Globalization;

namespace PanoramicData.Blazor;

/// <summary>
/// Renders a <see cref="ChatForm"/> inline: one question per tab, one submit (issue #106).
/// </summary>
/// <remarks>
/// <para>
/// Tabbed rather than a wizard or a long scroll. A wizard hides how much is being asked and leaves a
/// half-finished thing in the transcript; a long scroll becomes unusable past two or three
/// questions. Tabs show the whole commitment up front and let the questions be answered in any
/// order.
/// </para>
/// <para>
/// <b>Skipping is a first-class outcome.</b> Submit is always enabled, unanswered questions are
/// reported as skipped rather than omitted, and the form can be dismissed entirely without sending
/// anything. A form that will not let a conversation continue until it is filled in is worse than no
/// form.
/// </para>
/// <para>
/// State lives here and is intentionally not persisted. If the page reloads the form is gone, which
/// is the honest behaviour while chat state itself is not persisted: a form reappearing with half
/// its answers, detached from the conversation that prompted it, would be worse than one that does
/// not come back.
/// </para>
/// </remarks>
public partial class PDFormMessage
{
	/// <summary>
	/// The questions to ask. Nothing renders when null or empty.
	/// </summary>
	[Parameter]
	public ChatForm? Form { get; set; }

	/// <summary>
	/// Raised when the user submits, with one answer per question including the skipped ones.
	/// </summary>
	[Parameter]
	public EventCallback<ChatFormSubmission> OnSubmitted { get; set; }

	/// <summary>
	/// Raised when the user dismisses the form without answering.
	/// </summary>
	/// <remarks>
	/// Separate from submitting with nothing answered, because they mean different things: "I read
	/// this and declined" is not "I read this and answered nothing".
	/// </remarks>
	[Parameter]
	public EventCallback<Guid> OnDismissed { get; set; }

	/// <summary>
	/// Single-value answers: the chosen label, the scale value, or the text typed.
	/// </summary>
	private readonly Dictionary<string, string> _values = [];

	/// <summary>
	/// Multiple-choice selections, by question.
	/// </summary>
	private readonly Dictionary<string, HashSet<string>> _selections = [];

	/// <summary>
	/// Questions where "Other" is currently chosen.
	/// </summary>
	private readonly HashSet<string> _otherChosen = [];

	/// <summary>
	/// The free text typed against "Other", by question.
	/// </summary>
	/// <remarks>
	/// Held apart from <see cref="_values"/> because on a multiple-choice question "Other" sits
	/// alongside real selections rather than replacing them.
	/// </remarks>
	private readonly Dictionary<string, string> _otherText = [];

	private int _activeIndex;
	private bool _isClosed;
	private string _closedMessage = string.Empty;

	private bool IsChosen(string questionId, string label)
		=> _selections.TryGetValue(questionId, out var set)
			? set.Contains(label)
			: _values.GetValueOrDefault(questionId) == label;

	/// <summary>
	/// Picks exactly one choice, clearing "Other".
	/// </summary>
	private void ChooseOne(string questionId, string label)
	{
		_ = _otherChosen.Remove(questionId);
		_values[questionId] = label;
	}

	/// <summary>
	/// Adds or removes one choice on a multiple-choice question.
	/// </summary>
	private void ToggleMany(string questionId, string label, bool isChosen)
	{
		if (!_selections.TryGetValue(questionId, out var set))
		{
			set = [];
			_selections[questionId] = set;
		}

		_ = isChosen ? set.Add(label) : set.Remove(label);
	}

	/// <summary>
	/// Chooses "Other" on a single-choice question, which rules out the listed options.
	/// </summary>
	private void ChooseOther(string questionId)
	{
		_ = _otherChosen.Add(questionId);

		// The previously chosen label is cleared: the user has just said it was not right.
		_ = _values.Remove(questionId);
	}

	/// <summary>
	/// Adds or removes "Other" on a multiple-choice question, leaving other selections alone.
	/// </summary>
	private void ToggleOther(string questionId, bool isChosen)
	{
		if (isChosen)
		{
			_ = _otherChosen.Add(questionId);

			return;
		}

		_ = _otherChosen.Remove(questionId);
		_ = _otherText.Remove(questionId);
	}

	/// <summary>
	/// The value shown in a text box, falling back to the suggested text.
	/// </summary>
	private string TextValue(ChatFormQuestion question)
		=> _values.TryGetValue(question.Id, out var typed)
			? typed
			: question.SuggestedValue ?? string.Empty;

	/// <summary>
	/// Whether a question has an answer worth reporting.
	/// </summary>
	/// <remarks>
	/// A pre-filled text answer counts even if never touched: accepting a suggested draft is a
	/// decision. "Other" with nothing typed does not, because the user has said the options do not
	/// fit but has not yet said what does.
	/// </remarks>
	private bool IsAnswered(ChatFormQuestion question)
	{
		if (_selections.TryGetValue(question.Id, out var set) && set.Count > 0)
		{
			return true;
		}

		if (_otherChosen.Contains(question.Id)
			&& !string.IsNullOrWhiteSpace(_otherText.GetValueOrDefault(question.Id)))
		{
			return true;
		}

		if (_values.TryGetValue(question.Id, out var value) && !string.IsNullOrWhiteSpace(value))
		{
			return true;
		}

		return question.Kind == ChatFormAnswerKind.Text
			&& !string.IsNullOrWhiteSpace(question.SuggestedValue);
	}

	private string AnsweredSummary()
	{
		if (Form is null)
		{
			return string.Empty;
		}

		var answered = Form.Questions.Count(IsAnswered);

		return answered == Form.Questions.Count
			? "All answered"
			: string.Create(
				CultureInfo.InvariantCulture,
				$"{answered} of {Form.Questions.Count} answered - the rest will be reported as skipped");
	}

	private static string Midpoint(ChatFormScale scale)
		=> ((scale.Minimum + scale.Maximum) / 2).ToString(CultureInfo.InvariantCulture);

	/// <summary>
	/// Builds the answer for one question.
	/// </summary>
	internal ChatFormAnswer BuildAnswer(ChatFormQuestion question)
	{
		var otherText = _otherChosen.Contains(question.Id)
			? _otherText.GetValueOrDefault(question.Id)
			: null;

		var hasOther = !string.IsNullOrWhiteSpace(otherText);

		if (question.Kind == ChatFormAnswerKind.MultipleChoice)
		{
			var selected = _selections.TryGetValue(question.Id, out var set)
				? set.OrderBy(label => label, StringComparer.Ordinal).ToList()
				: [];

			var readable = new List<string>(selected);

			if (hasOther)
			{
				readable.Add($"Other: {otherText}");
			}

			return new ChatFormAnswer
			{
				QuestionId = question.Id,
				Question = question.Question,
				Value = readable.Count > 0 ? string.Join(", ", readable) : null,
				Values = selected.Count > 0 ? selected : null,
				OtherText = otherText,
				WasOther = hasOther,
				WasSkipped = readable.Count == 0
			};
		}

		var value = _values.GetValueOrDefault(question.Id);

		// A suggested value the user never touched is still their answer.
		if (string.IsNullOrWhiteSpace(value)
			&& question.Kind == ChatFormAnswerKind.Text
			&& !string.IsNullOrWhiteSpace(question.SuggestedValue))
		{
			value = question.SuggestedValue;
		}

		if (hasOther)
		{
			value = otherText;
		}

		return new ChatFormAnswer
		{
			QuestionId = question.Id,
			Question = question.Question,
			Value = string.IsNullOrWhiteSpace(value) ? null : value,
			OtherText = otherText,
			WasOther = hasOther,
			WasSkipped = string.IsNullOrWhiteSpace(value),
			ScaleDescription = question.Kind == ChatFormAnswerKind.Scale && question.Scale is not null
				? string.Create(
					CultureInfo.InvariantCulture,
					$"{question.Scale.Minimum} = {question.Scale.MinimumLabel}, {question.Scale.Maximum} = {question.Scale.MaximumLabel}")
				: null
		};
	}

	private async Task SubmitAsync()
	{
		if (Form is null || _isClosed)
		{
			return;
		}

		// Closed before awaiting: the callback can take a moment, and a second click in that window
		// would send the answers twice.
		_isClosed = true;
		_closedMessage = "Thanks - your answers have been sent.";

		var answers = new List<ChatFormAnswer>(Form.Questions.Count);

		foreach (var question in Form.Questions)
		{
			answers.Add(BuildAnswer(question));
		}

		await OnSubmitted
			.InvokeAsync(new ChatFormSubmission { FormId = Form.Id, Answers = answers })
			.ConfigureAwait(true);
	}

	private async Task DismissAsync()
	{
		if (Form is null || _isClosed)
		{
			return;
		}

		_isClosed = true;
		_closedMessage = "No problem - these questions were dismissed.";

		await OnDismissed.InvokeAsync(Form.Id).ConfigureAwait(true);
	}
}
