using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>
/// Tests for the simulated replies of <see cref="DumbChatService"/>. The service answers after a random
/// delay of up to five seconds (fifteen when it "goes offline"), so each test waits for the announcement of
/// the reply it expects rather than for a fixed time.
/// </summary>
public sealed class DumbChatServiceReplyTests : IDisposable
{
	private static readonly TimeSpan _replyTimeout = TimeSpan.FromSeconds(30);
	private static readonly ChatMessageSender _user = new() { Name = "Me", IsUser = true, IsHuman = true };

	private readonly DumbChatService _service = new();

	/// <inheritdoc />
	public void Dispose() => _service.Dispose();

	private static ChatMessage From(ChatMessageSender sender, string text)
		=> new() { Id = Guid.NewGuid(), Sender = sender, Message = text, Type = MessageType.Normal };

	private Task<ChatMessage> NextFromBotAsync(Guid conversation, Func<ChatMessage, bool> predicate)
	{
		var reply = new TaskCompletionSource<ChatMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
		_service.OnConversationMessageReceived += (id, message) =>
		{
			if (id == conversation && message.Sender == DumbChatService.DumbBot && predicate(message))
			{
				reply.TrySetResult(message);
			}
		};
		return reply.Task.WaitAsync(_replyTimeout, TestContext.Current.CancellationToken);
	}

	/// <summary>A form submission is acknowledged at once with a count of the questions answered.</summary>
	[Fact]
	public void FormSubmission_IsAcknowledgedImmediately()
	{
		var submission = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Sender = _user,
			Message = "answers",
			Type = MessageType.Normal,
			FormSubmission = new ChatFormSubmission
			{
				FormId = Guid.NewGuid(),
				Answers =
				[
					new ChatFormAnswer { QuestionId = "a", Question = "A", Value = "1" },
					new ChatFormAnswer { QuestionId = "b", Question = "B", WasSkipped = true }
				]
			}
		};

		_service.SendMessage(submission);

		var acknowledgement = _service.Messages[^1];
		acknowledgement.Message.Should().Be("Thanks - I got 1 of 2 answers.");
		acknowledgement.Type.Should().Be(MessageType.Success);
	}

	/// <summary>A message from someone other than the user is recorded but not answered.</summary>
	[Fact]
	public void BotMessage_IsNotAnswered()
	{
		_service.SendMessage(From(DumbChatService.TimeBot, "tick"));

		_service.Messages.Should().ContainSingle();
	}

	/// <summary>An ordinary message is echoed back, replacing the typing placeholder that shares its id.</summary>
	[Fact]
	public async Task OrdinaryMessage_IsEchoedInPlaceOfTypingPlaceholder()
	{
		var conversation = _service.CreateConversation();
		var typing = NextFromBotAsync(conversation, m => m.Type == MessageType.Typing);
		var echo = NextFromBotAsync(conversation, m => m.Type != MessageType.Typing);

		_service.SendMessage(conversation, From(_user, "hello there"));

		var placeholder = await typing;
		var reply = await echo;
		reply.Message.Should().Be("You said: \"hello there\"");
		reply.Id.Should().Be(placeholder.Id);
		_service.GetMessages(conversation).Should().HaveCount(2);
	}

	/// <summary>Asking for help gets an HTML list of commands.</summary>
	[Fact]
	public async Task HelpMessage_ListsCommands()
	{
		var conversation = _service.CreateConversation();
		var help = NextFromBotAsync(conversation, m => m.Type != MessageType.Typing);

		_service.SendMessage(conversation, From(_user, "HELP me"));

		var reply = await help;
		reply.Title.Should().Be("<b>Help</b>");
		reply.IsTitleHtml.Should().BeTrue();
		reply.IsMessageHtml.Should().BeTrue();
		reply.Message.Should().Contain("<li>help</li>");
	}

	/// <summary>Mentioning a form or question produces the demonstration form.</summary>
	[Fact]
	public async Task FormRequest_ProducesForm()
	{
		var conversation = _service.CreateConversation();
		var form = NextFromBotAsync(conversation, m => m.Type == MessageType.Form);

		_service.SendMessage(conversation, From(_user, "I have a question"));

		var reply = await form;
		reply.Form.Should().NotBeNull();
		reply.Form!.Questions.Should().NotBeEmpty();
	}

	/// <summary>Asking the bot to go away takes it offline for a while and then brings it back.</summary>
	[Fact]
	public async Task AwayMessage_GoesOfflineThenReturns()
	{
		_service.Initialize();
		var conversation = _service.CreateConversation();
		var liveStates = new List<bool>();
		_service.OnLiveStatusChanged += liveStates.Add;
		var offline = NextFromBotAsync(conversation, m => m.Title == "Going Offline");
		var back = NextFromBotAsync(conversation, m => m.Title == "Back Online");

		_service.SendMessage(conversation, From(_user, "go away"));

		(await offline).Type.Should().Be(MessageType.Warning);
		_service.IsLive.Should().BeFalse();
		await back;
		_service.IsLive.Should().BeTrue();
		liveStates.Should().Equal(false, true);
	}
}
