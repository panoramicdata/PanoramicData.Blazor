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
/// <b>Skipping is a first-class outcome.</b> Unanswered questions are reported as skipped rather
/// than omitted, and the form can be dismissed entirely without sending anything. A form that will
/// not let a conversation continue until it is filled in is worse than no form.
/// </para>
/// <para>
/// <b>Submit appears only on the last tab; every other tab offers Next.</b> With a submit button on
/// every question most people press it on the first one - they have done exactly what the button
/// told them to - and the asker receives a form that is one-sixth answered. Advancing is the
/// expected path, so that is what the primary button does. Submitting early is still one click on
/// the last tab, which makes it a deliberate act rather than an accident.
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

	/// <summary>
	/// The order the user has put a ranking question's options into.
	/// </summary>
	/// <remarks>
	/// Seeded from the question's own order the first time it is touched, so an untouched ranking is
	/// recorded as skipped rather than as an endorsement of whatever order the asker happened to
	/// list.
	/// </remarks>
	private readonly Dictionary<string, List<string>> _rankings = [];

	private int _activeIndex;
	private bool _isClosed;
	private string _closedMessage = string.Empty;

	/// <summary>
	/// The options in their current ranked order.
	/// </summary>
	private List<string> RankedOptions(ChatFormQuestion question)
		=> _rankings.TryGetValue(question.Id, out var ranked)
			? ranked
			: [.. question.Options.Select(option => option.Label)];

	/// <summary>
	/// Moves one option up or down a ranking.
	/// </summary>
	private void Move(ChatFormQuestion question, int index, int offset)
	{
		var ranked = RankedOptions(question);
		var target = index + offset;

		if (target < 0 || target >= ranked.Count)
		{
			return;
		}

		(ranked[index], ranked[target]) = (ranked[target], ranked[index]);

		// Stored on first move, which is also what marks the question answered.
		_rankings[question.Id] = ranked;
	}

	/// <summary>
	/// Records or clears an acknowledgement.
	/// </summary>
	/// <remarks>
	/// Unticking removes the entry entirely rather than storing "false": an acknowledgement is either
	/// given or it is not, and "not given" is a skip, not a negative answer.
	/// </remarks>
	private void Acknowledge(string questionId, bool isAcknowledged)
	{
		if (isAcknowledged)
		{
			_values[questionId] = "Acknowledged";

			return;
		}

		_ = _values.Remove(questionId);
	}

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

		if (question.Kind == ChatFormAnswerKind.Ranking)
		{
			return _rankings.ContainsKey(question.Id);
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

	/// <summary>
	/// Whether the active tab is the last one, and so the one that submits.
	/// </summary>
	/// <remarks>
	/// The primary button is "Next" everywhere else. With a Submit on every tab, most people press it
	/// on the first question and send a form that is one-sixth answered - they have done what the
	/// button told them to, and the asker gets far less than they asked for. Moving through the
	/// questions is the expected path, so that is what the button does; jumping straight to the end
	/// and submitting early is still one click on the last tab, which is a deliberate act rather than
	/// an accidental one.
	/// </remarks>
	private bool IsLastQuestion => Form is null || _activeIndex >= Form.Questions.Count - 1;

	/// <summary>
	/// Where the user is, shown while there are still questions ahead.
	/// </summary>
	/// <remarks>
	/// Position rather than the answered count: before the end, what matters is how much is left,
	/// and the consequence of skipping is only worth stating at the point of submitting.
	/// </remarks>
	private string ProgressSummary()
		=> Form is null
			? string.Empty
			: string.Create(
				CultureInfo.InvariantCulture,
				$"Question {_activeIndex + 1} of {Form.Questions.Count} - or pick a tab to jump");

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

	/// <summary>
	/// What to show under the slider: the point's label where there is one, else the number.
	/// </summary>
	private string ScaleValueText(ChatFormQuestion question)
	{
		if (!_values.TryGetValue(question.Id, out var raw) || string.IsNullOrWhiteSpace(raw))
		{
			return "not answered";
		}

		if (question.Scale is not null
			&& int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
		{
			var label = question.Scale.LabelFor(value);

			if (!string.IsNullOrWhiteSpace(label))
			{
				return label;
			}
		}

		return raw;
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

		if (question.Kind == ChatFormAnswerKind.Ranking)
		{
			// Only an order the user actually arranged is reported. An untouched ranking is a skip:
			// the asker's own listing order is not an answer.
			var ranked = _rankings.TryGetValue(question.Id, out var order) ? order : null;

			return new ChatFormAnswer
			{
				QuestionId = question.Id,
				Question = question.Question,
				Value = ranked is null
					? null
					: string.Join(
						", ",
						ranked.Select((label, position) => string.Create(
							CultureInfo.InvariantCulture,
							$"{position + 1}. {label}"))),
				Values = ranked,
				WasSkipped = ranked is null
			};
		}

		if (question.Kind == ChatFormAnswerKind.MultipleChoice)
		{
			// Ordered as the question offered them, not alphabetically. The asker chose that order -
			// often most to least likely - and re-sorting throws that away and reads oddly besides.
			var selected = _selections.TryGetValue(question.Id, out var set)
				? question.Options
					.Select(option => option.Label)
					.Where(set.Contains)
					.ToList()
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

		// A labelled scale records "Agree" rather than "2". The number is not lost - it stays in
		// ScaleDescription - but the answer itself should be readable without a key.
		if (question.Kind == ChatFormAnswerKind.Scale
			&& question.Scale is not null
			&& int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var point))
		{
			var label = question.Scale.LabelFor(point);

			if (!string.IsNullOrWhiteSpace(label))
			{
				value = label;
			}
		}

		// The unit travels with the number, so "20" is never left needing a key.
		if (question.Kind == ChatFormAnswerKind.Number
			&& !string.IsNullOrWhiteSpace(value)
			&& !string.IsNullOrWhiteSpace(question.Number?.Unit))
		{
			value = string.Create(CultureInfo.InvariantCulture, $"{value} {question.Number.Unit}");
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
				? DescribeScale(question.Scale, value)
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

	/// <summary>
	/// Records what the scale meant, so the answer is interpretable without the form.
	/// </summary>
	/// <remarks>
	/// Always includes the number actually chosen, even when the answer itself is a label: the label
	/// is what a person reads, the number is what anything counting or averaging needs.
	/// </remarks>
	internal static string DescribeScale(ChatFormScale scale, string? chosen)
	{
		var points = scale.HasUsablePointLabels && scale.PointLabels is not null
			? string.Join(
				", ",
				scale.PointLabels.Select((label, offset) => string.Create(
					CultureInfo.InvariantCulture,
					$"{scale.Minimum + offset} = {label}")))
			: string.Create(
				CultureInfo.InvariantCulture,
				$"{scale.Minimum} = {scale.MinimumLabel}, {scale.Maximum} = {scale.MaximumLabel}");

		return string.IsNullOrWhiteSpace(chosen)
			? points
			: string.Create(CultureInfo.InvariantCulture, $"{points} (chosen: {chosen})");
	}
}
