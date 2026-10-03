using System;
using System.Security.Cryptography;

namespace PanoramicData.Blazor.Services;

/// <summary>
/// The simulated replies of <see cref="DumbChatService"/>: a "typing" pause, then an answer chosen by keyword,
/// including the demonstration question form (issue #106).
/// </summary>
public partial class DumbChatService
{
	private static readonly MessageType[] _messageTypes = [.. Enum.GetValues<MessageType>().Except([MessageType.Typing])];

	/// <inheritdoc />
	public event Action<bool>? OnLiveStatusChanged;

	private async Task RespondAsync(Guid conversationId, ChatMessage userMessage)
	{
		// Issue #106: answers to a form are not a fresh request, so they are acknowledged rather than
		// run through the keyword dispatch below. Echoing them back is merely noisy, but the dispatch
		// is worse: a question whose text happened to contain "form" or "question" would spawn another
		// form on submission, and then another. A submission must never be re-read as a new request.
		if (userMessage.FormSubmission is not null)
		{
			AcknowledgeFormSubmission(conversationId, userMessage.FormSubmission);
			return;
		}

		// Ignore messages not from the user
		if (!userMessage.Sender.IsUser)
		{
			return;
		}

		// Create a shared GUID for both typing and final messages
		var responseId = Guid.NewGuid();

		// Wait 500-3000ms to simulate "delayed response", then show a "typing" placeholder for 1-2 seconds.
		// The delays are random only to look natural; nothing depends on them being unpredictable.
		await Task.Delay(RandomNumberGenerator.GetInt32(500, 3000));
		Deliver(conversationId, new ChatMessage
		{
			Id = responseId,
			Sender = DumbBot,
			Title = "Typing...",
			Message = "...",
			Type = MessageType.Typing,
		});
		await Task.Delay(RandomNumberGenerator.GetInt32(1000, 2000));

		await ReplyAsync(conversationId, responseId, userMessage.Message);
	}

	/// <summary>
	/// Answers a user's message by keyword:
	/// "away", "offline" or "bio-break" simulate going offline for 10 seconds; "form" or "question" produce a
	/// question form; "help" lists the commands; anything else is echoed back.
	/// </summary>
	private async Task ReplyAsync(Guid conversationId, Guid responseId, string text)
	{
		if (ContainsAny(text, "away", "offline", "bio-break"))
		{
			await TakeABreakAsync(conversationId, responseId);
			return;
		}

		// Issue #106: "form" always produces a question form, so the inline form can be exercised
		// without an AI behind the chat.
		if (ContainsAny(text, "form", "question"))
		{
			Deliver(conversationId, new ChatMessage
			{
				Id = Guid.NewGuid(),
				Sender = DumbBot,
				Message = "A few quick questions - answer what you like and skip the rest.",
				Type = MessageType.Form,
				Timestamp = DateTime.UtcNow,
				Form = BuildDemonstrationForm()
			});
			return;
		}

		Deliver(conversationId, ContainsAny(text, "help") ? HelpReply(responseId) : EchoReply(responseId, text));
	}

	private static bool ContainsAny(string text, params string[] keywords)
		=> keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));

	private void AcknowledgeFormSubmission(Guid conversationId, ChatFormSubmission submission)
	{
		var answered = submission.Answers.Count(answer => !answer.WasSkipped);

		Deliver(conversationId, new ChatMessage
		{
			Id = Guid.NewGuid(),
			Sender = DumbBot,
			Message = $"Thanks - I got {answered} of {submission.Answers.Count} answers.",
			Type = MessageType.Success,
			Timestamp = DateTime.UtcNow
		});
	}

	/// <summary>Simulates going offline for 10 seconds, announcing both the departure and the return.</summary>
	private async Task TakeABreakAsync(Guid conversationId, Guid responseId)
	{
		_isOnline = false;
		OnLiveStatusChanged?.Invoke(false);
		Deliver(conversationId, new ChatMessage
		{
			Id = responseId,
			Sender = DumbBot,
			Title = "Going Offline",
			Message = "I'm going offline for a short break. Please wait...",
			Type = MessageType.Warning
		});

		await Task.Delay(10000);

		_isOnline = true;
		OnLiveStatusChanged?.Invoke(true);
		Deliver(conversationId, new ChatMessage
		{
			Id = Guid.NewGuid(),
			Sender = DumbBot,
			Title = "Back Online",
			Message = "I'm back online! How can I assist you?",
			Type = MessageType.Normal
		});
	}

	private static ChatMessage HelpReply(Guid responseId) => new()
	{
		Id = responseId,
		Sender = DumbBot,
		Title = "<b>Help</b>",
		IsTitleHtml = true,
		Message = "Available commands: <ul><li>help</li><li>go away</li></ul>",
		IsMessageHtml = true,
		Type = MessageType.Normal
	};

	// Sent with the typing placeholder's id so the UI can replace it, and with a random type to show them all off.
	private static ChatMessage EchoReply(Guid responseId, string text) => new()
	{
		Id = responseId,
		Sender = DumbBot,
		Message = $"You said: \"{text}\"",
		Type = _messageTypes[RandomNumberGenerator.GetInt32(_messageTypes.Length)]
	};

	/// <summary>
	/// A form exercising every answer kind, for the demo (issue #106).
	/// </summary>
	/// <remarks>
	/// Deliberately one of each: single choice with descriptions, multiple choice with "Other", two
	/// differently-shaped scales, and both text sizes including a pre-filled draft. If a change to
	/// the form renderer breaks any kind, typing "form" into the demo shows it immediately.
	/// </remarks>
	public static ChatForm BuildDemonstrationForm() => new()
	{
		Id = Guid.NewGuid(),
		Title = "Tell us about ice cream",
		Questions =
		[
			FlavoursQuestion(),
			FavouriteQuestion(),
			AgreementQuestion(),
			AgainQuestion(),
			new ChatFormQuestion
			{
				Id = "when",
				Header = "When",
				Question = "When did you last have some?",
				Kind = ChatFormAnswerKind.DateTime
			},
			ScoopsQuestion(),
			OrderQuestion(),
			new ChatFormQuestion
			{
				Id = "ack",
				Header = "Agreed",
				Question = "One last thing.",
				Kind = ChatFormAnswerKind.Acknowledgement,
				Options = [new ChatFormOption { Label = "I accept that ice cream is not a breakfast food." }]
			},
			new ChatFormQuestion
			{
				Id = "shop",
				Header = "Shop",
				Question = "Which shop do you buy it from?",
				Kind = ChatFormAnswerKind.Text
			},
			SummaryQuestion()
		]
	};

	private static ChatFormQuestion FlavoursQuestion() => new()
	{
		Id = "flavours",
		Header = "Flavours",
		Question = "Which flavours of ice cream do you like?",
		Kind = ChatFormAnswerKind.MultipleChoice,
		AllowOther = true,
		Options =
		[
			new ChatFormOption { Label = "Vanilla", Description = "The one everything else is measured against" },
			new ChatFormOption { Label = "Pistachio", Description = "Green, expensive, worth it" },
			new ChatFormOption { Label = "Rum and raisin", Description = "Divisive" }
		]
	};

	private static ChatFormQuestion FavouriteQuestion() => new()
	{
		Id = "favourite",
		Header = "Favourite",
		Question = "Which is your favourite?",
		Kind = ChatFormAnswerKind.SingleChoice,
		AllowOther = true,
		Options =
		[
			new ChatFormOption { Label = "Vanilla", Description = "Reliable" },
			new ChatFormOption { Label = "Pistachio", Description = "Green, expensive, worth it" },
			new ChatFormOption { Label = "Rum and raisin", Description = "Divisive" }
		]
	};

	private static ChatFormQuestion AgreementQuestion() => new()
	{
		Id = "agreement",
		Header = "Agreement",
		Question = "Ice cream is better than cake.",
		Kind = ChatFormAnswerKind.Scale,
		Scale = new ChatFormScale
		{
			Minimum = 1,
			Maximum = 4,
			MinimumLabel = "Strongly disagree",
			MaximumLabel = "Strongly agree",

			// Named point by point, so the answer records "Agree" rather than "2".
			PointLabels = ["Strongly disagree", "Disagree", "Agree", "Strongly agree"]
		}
	};

	private static ChatFormQuestion AgainQuestion() => new()
	{
		Id = "again",
		Header = "Yes/No",
		Question = "Would you eat ice cream again today?",
		Kind = ChatFormAnswerKind.Scale,
		Scale = new ChatFormScale
		{
			Minimum = 0,
			Maximum = 1,
			MinimumLabel = "No",
			MaximumLabel = "Yes",
			PointLabels = ["No", "Yes"]
		}
	};

	private static ChatFormQuestion OrderQuestion() => new()
	{
		Id = "order",
		Header = "Order",
		Question = "Put these in order, best first.",
		Kind = ChatFormAnswerKind.Ranking,
		Options =
		[
			new ChatFormOption { Label = "Vanilla" },
			new ChatFormOption { Label = "Pistachio" },
			new ChatFormOption { Label = "Rum and raisin" }
		]
	};

	private static ChatFormQuestion ScoopsQuestion() => new()
	{
		Id = "scoops",
		Header = "How many",
		Question = "How many scoops is the right number?",
		Kind = ChatFormAnswerKind.Number,
		Number = new ChatFormNumber { Minimum = 1, Maximum = 10, Unit = "scoops" }
	};

	private static ChatFormQuestion SummaryQuestion() => new()
	{
		Id = "summary",
		Header = "Summary",
		Question = "Here is a suggested summary - please edit it.",
		Kind = ChatFormAnswerKind.Text,
		IsMultiline = true,
		SuggestedValue = "I like several flavours, pistachio most of all, and I would happily "
			+ "eat more today."
	};
}
