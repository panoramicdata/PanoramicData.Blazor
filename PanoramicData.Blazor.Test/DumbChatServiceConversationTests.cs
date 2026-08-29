using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="DumbChatService"/> genuinely holds several conversations at once
/// (issue #110, MS-25918).
/// </summary>
/// <remarks>
/// Issue #104 put the conversation-addressed API on <see cref="IChatService"/> with default implementations
/// that map onto a single implicit conversation, and nothing production-side ever opted in - so until this
/// work, <c>SupportsConversations</c> was <c>true</c> in exactly one place in the repository, a test double.
///
/// The test that earns its place here is
/// <see cref="A_reply_to_a_background_conversation_leaves_the_selected_one_alone"/>. That is the misdelivery
/// the conversation-addressed API exists to prevent, it is the case conversation tabs make visible, and it is
/// invisible to any test that only ever has one conversation open.
/// </remarks>
public class DumbChatServiceConversationTests
{
	/// <summary>Verifies that the demo service now advertises the capability.</summary>
	/// <remarks>
	/// A consumer is required to check this before relying on
	/// <see cref="IChatService.OnConversationMessageReceived"/>, whose default implementation accepts
	/// subscriptions and never fires - so a service that supported conversations while reporting otherwise
	/// would present as a chat that silently never updated.
	/// </remarks>
	[Fact]
	public void The_demo_service_supports_conversations()
	{
		IChatService service = new DumbChatService();

		service.SupportsConversations.Should().BeTrue();
	}

	/// <summary>Verifies that two conversations each accumulate their own transcript.</summary>
	[Fact]
	public void Two_conversations_hold_separate_transcripts()
	{
		var service = new DumbChatService();
		var first = service.CreateConversation();
		var second = service.CreateConversation();

		service.SendMessage(first, BotMessage("in the first"));
		service.SendMessage(second, BotMessage("in the second"));

		service.GetMessages(first).Should().ContainSingle().Which.Message.Should().Be("in the first");
		service.GetMessages(second).Should().ContainSingle().Which.Message.Should().Be("in the second");
	}

	/// <summary>
	/// Verifies that a message arriving for a conversation the user is not looking at does not disturb the
	/// one they are.
	/// </summary>
	/// <remarks>
	/// This is the whole point of the change. Merlin answers take between forty seconds and three minutes, so
	/// the working pattern tabs exist to support is: ask, switch away, come back. If a reply lands in the
	/// selected transcript rather than the one it was asked for, that pattern silently corrupts both
	/// conversations - and the symptom depends on how long the answer took, which is why it reads as a
	/// rendering bug rather than a delivery one.
	/// </remarks>
	[Fact]
	public void A_reply_to_a_background_conversation_leaves_the_selected_one_alone()
	{
		var service = new DumbChatService();
		var selected = service.CreateConversation();
		var background = service.CreateConversation();
		service.ActiveConversationId = selected;

		service.SendMessage(selected, BotMessage("visible"));
		service.SendMessage(background, BotMessage("arrived while you were away"));

		service.Messages.Should().ContainSingle()
			.Which.Message.Should().Be("visible", "the selected transcript must not acquire another conversation's reply");
		service.GetMessages(background).Should().ContainSingle()
			.Which.Message.Should().Be("arrived while you were away");
	}

	/// <summary>
	/// Verifies that a consumer is told which conversation a message belongs to, rather than having to guess
	/// at "the current one".
	/// </summary>
	[Fact]
	public void Every_message_is_announced_against_its_own_conversation()
	{
		var service = new DumbChatService();
		var first = service.CreateConversation();
		var second = service.CreateConversation();
		service.ActiveConversationId = first;

		var announced = new List<(Guid ConversationId, string Message)>();
		service.OnConversationMessageReceived += (id, message) => announced.Add((id, message.Message));

		service.SendMessage(first, BotMessage("one"));
		service.SendMessage(second, BotMessage("two"));

		announced.Should().Equal(
			(first, "one"),
			(second, "two"));
	}

	/// <summary>
	/// Verifies that a consumer using only the singular API sees the active conversation and nothing else.
	/// </summary>
	/// <remarks>
	/// The singular event has no conversation on it, so a consumer subscribed to it cannot route what it is
	/// handed. Raising it for a background conversation would hand such a consumer a message it would append
	/// to whatever it was showing - reintroducing the misdelivery through the compatibility path.
	/// </remarks>
	[Fact]
	public void The_singular_event_reports_only_the_selected_conversation()
	{
		var service = new DumbChatService();
		var selected = service.CreateConversation();
		var background = service.CreateConversation();
		service.ActiveConversationId = selected;

		var received = new List<string>();
		service.OnMessageReceived += message => received.Add(message.Message);

		service.SendMessage(selected, BotMessage("visible"));
		service.SendMessage(background, BotMessage("hidden"));

		received.Should().Equal("visible");
	}

	/// <summary>Verifies that an unknown conversation yields nothing rather than the active transcript.</summary>
	/// <remarks>
	/// Returning the selected transcript for an id the service has never heard of would be indistinguishable,
	/// to a caller, from that conversation genuinely containing those messages.
	/// </remarks>
	[Fact]
	public void An_unknown_conversation_has_no_messages()
	{
		var service = new DumbChatService();
		service.SendMessage(BotMessage("in the implicit conversation"));

		service.GetMessages(Guid.NewGuid()).Should().BeEmpty();
	}

	/// <summary>Verifies that addressing a conversation that does not exist fails loudly.</summary>
	/// <remarks>
	/// A caller addressing a conversation the service does not have has a bug. Silently retargeting it to the
	/// active conversation would surface several seconds later as a message in an unrelated transcript, which
	/// is far harder to trace than a throw at the call site.
	/// </remarks>
	[Fact]
	public void Sending_to_a_conversation_that_does_not_exist_throws()
	{
		var service = new DumbChatService();
		var unknown = Guid.NewGuid();

		var act = () => service.SendMessage(unknown, BotMessage("nowhere"));

		act.Should().Throw<InvalidOperationException>().WithMessage($"*{unknown}*");
	}

	/// <summary>
	/// Verifies that a consumer that never mentions a conversation still works exactly as before.
	/// </summary>
	/// <remarks>
	/// The whole point of the default interface implementations on <see cref="IChatService"/> was that
	/// existing consumers keep working. Opting this service in must not quietly break the ones that have not
	/// opted in themselves.
	/// </remarks>
	[Fact]
	public void A_consumer_that_ignores_conversations_behaves_as_before()
	{
		var service = new DumbChatService();

		var received = new List<string>();
		service.OnMessageReceived += message => received.Add(message.Message);

		service.SendMessage(BotMessage("hello"));

		received.Should().Equal("hello");
		service.Messages.Should().ContainSingle().Which.Message.Should().Be("hello");
	}

	/// <summary>Verifies that clearing empties only the conversation being shown.</summary>
	/// <remarks>
	/// The clear button lives in the chat header, which in full-screen sits above one selected tab. Clearing
	/// every conversation from there would destroy transcripts the user could see but was not pointing at -
	/// and in a feature whose central promise is that nothing is ever deleted, that is the worst available
	/// behaviour.
	/// </remarks>
	[Fact]
	public void Clearing_empties_only_the_selected_conversation()
	{
		var service = new DumbChatService();
		var selected = service.CreateConversation();
		var other = service.CreateConversation();
		service.SendMessage(selected, BotMessage("goes"));
		service.SendMessage(other, BotMessage("stays"));

		service.ActiveConversationId = selected;
		service.ClearMessages();

		service.GetMessages(selected).Should().BeEmpty();
		service.GetMessages(other).Should().ContainSingle().Which.Message.Should().Be("stays");
	}

	/// <summary>Verifies that selecting a conversation the service has not got creates it.</summary>
	/// <remarks>
	/// A consumer that has just learned of a conversation from a history service should be able to select it
	/// without a separate registration call. Throwing instead would make the obvious call order wrong.
	/// </remarks>
	[Fact]
	public void Selecting_an_unknown_conversation_creates_it()
	{
		var service = new DumbChatService();
		var fresh = Guid.NewGuid();

		service.ActiveConversationId = fresh;

		service.ConversationIds.Should().Contain(fresh);
		service.Messages.Should().BeEmpty();
	}

	/// <summary>
	/// A message from the bot rather than the user, so that the service records it without starting its
	/// simulated reply workflow - these tests are about where a message lands, not about what answers it.
	/// </summary>
	private static ChatMessage BotMessage(string text) => new()
	{
		Id = Guid.NewGuid(),
		Sender = DumbChatService.DumbBot,
		Message = text,
		Type = MessageType.Normal
	};
}
