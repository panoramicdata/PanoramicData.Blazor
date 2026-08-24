using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for <see cref="ChatConversation"/>, the model behind PDChat's conversation history (issue #102).
/// </summary>
/// <remarks>
/// Two things here are worth more than they look. The first is that <see cref="ChatConversation.Title"/>
/// distinguishes null from empty: a host that generates titles in the background finds its work by looking
/// for untitled conversations, and if blank and absent are the same value it either re-titles everything
/// forever or never re-titles anything.
///
/// The second is <see cref="ChatConversation.DisplayName"/>. The fallback rule lives on the model precisely
/// so that a conversation list and a conversation tab cannot disagree about what the same conversation is
/// called - two independently-written fallbacks would drift, and the symptom would be a user renaming
/// something in one place and seeing the old name in the other.
/// </remarks>
public class ChatConversationTests
{
	/// <summary>Verifies that a conversation needs only an id, and that everything else has a sane default.</summary>
	[Fact]
	public void A_new_conversation_needs_only_an_id()
	{
		var id = Guid.NewGuid();

		var conversation = new ChatConversation { Id = id };

		conversation.Id.Should().Be(id);
		conversation.Title.Should().BeNull();
		conversation.Preview.Should().BeNull();
		conversation.IsArchived.Should().BeFalse();
		conversation.MessageCount.Should().Be(0);
	}

	/// <summary>Verifies that an untitled conversation is distinguishable from one deliberately titled as blank.</summary>
	[Fact]
	public void An_absent_title_is_not_the_same_as_a_blank_one()
	{
		var untitled = new ChatConversation { Id = Guid.NewGuid() };
		var blankTitled = new ChatConversation { Id = Guid.NewGuid(), Title = string.Empty };

		untitled.IsTitled.Should().BeFalse();
		blankTitled.IsTitled.Should().BeTrue();
	}

	/// <summary>Verifies that a title, when there is one, is what the conversation is called.</summary>
	[Fact]
	public void The_title_is_the_display_name_when_there_is_one()
	{
		var conversation = new ChatConversation
		{
			Id = Guid.NewGuid(),
			Title = "Guest network SSID",
			Preview = "why can nobody connect to the guest network"
		};

		conversation.DisplayName.Should().Be("Guest network SSID");
	}

	/// <summary>Verifies that an untitled conversation is still identifiable, by its opening message.</summary>
	[Fact]
	public void An_untitled_conversation_falls_back_to_its_preview()
	{
		var conversation = new ChatConversation
		{
			Id = Guid.NewGuid(),
			Preview = "why can nobody connect to the guest network"
		};

		conversation.DisplayName.Should().Be("why can nobody connect to the guest network");
	}

	/// <summary>
	/// Verifies that a title of only whitespace falls back too. It is distinguishable from absent for the
	/// titling job, but it is not something a user can read off a list.
	/// </summary>
	[Fact]
	public void A_whitespace_title_falls_back_to_the_preview()
	{
		var conversation = new ChatConversation
		{
			Id = Guid.NewGuid(),
			Title = "   ",
			Preview = "why can nobody connect to the guest network"
		};

		conversation.DisplayName.Should().Be("why can nobody connect to the guest network");
	}

	/// <summary>
	/// Verifies that a conversation with nothing to show still has a name. A brand new conversation has no
	/// title and no messages, so this is the ordinary case at the moment a tab is opened, not an edge case.
	/// </summary>
	[Fact]
	public void A_conversation_with_neither_title_nor_preview_still_has_a_name()
	{
		var conversation = new ChatConversation { Id = Guid.NewGuid() };

		conversation.DisplayName.Should().Be(ChatConversation.UntitledDisplayName);
		conversation.DisplayName.Should().NotBeNullOrWhiteSpace();
	}
}
