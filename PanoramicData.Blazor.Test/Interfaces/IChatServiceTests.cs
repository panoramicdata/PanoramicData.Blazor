using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Interfaces;

/// <summary>
/// Tests for the default interface implementations on <see cref="IChatService"/>, as seen by a service that
/// implements only the required members. The conversation-addressed defaults are covered by
/// <c>ChatServiceConversationAddressingTests</c>.
/// </summary>
public class IChatServiceTests
{
	private readonly MinimalChatService _concrete = new();

	private IChatService Service => _concrete;

	/// <summary>Input is permitted by default, and a value set is remembered.</summary>
	[Fact]
	public void IsInputPermitted_DefaultsToTrueAndRemembersSetter()
	{
		Service.IsInputPermitted.Should().BeTrue();

		Service.IsInputPermitted = false;

		Service.IsInputPermitted.Should().BeFalse();
	}

	/// <summary>There is no input-disabled message by default, and a value set is remembered.</summary>
	[Fact]
	public void InputDisabledMessage_DefaultsToNullAndRemembersSetter()
	{
		Service.InputDisabledMessage.Should().BeNull();

		Service.InputDisabledMessage = "Read only";

		Service.InputDisabledMessage.Should().Be("Read only");
	}

	/// <summary>The toast defaults describe a growing, auto-dismissing, titled toast anchored bottom right.</summary>
	[Fact]
	public void ToastDefaults_HaveDocumentedValues()
	{
		Service.ToastEntryAnimation.Should().Be(PDChatToastAnimation.Grow);
		Service.ToastExitAnimation.Should().Be(PDChatToastAnimation.Shrink);
		Service.ToastAnimationDurationMs.Should().Be(250d);
		Service.ToastAutoDismiss.Should().BeTrue();
		Service.ToastShowTitle.Should().BeTrue();
		Service.ToastMinWidth.Should().Be("200px");
		Service.ToastMaxWidth.Should().Be("300px");
		Service.ToastMinHeight.Should().BeEmpty();
		Service.ToastMaxHeight.Should().BeEmpty();
		Service.ToastMaxVisible.Should().Be(5);
		Service.ToastAnchor.Should().Be(PDChatButtonPosition.BottomRight);
	}

	/// <summary>The toast defaults remember a value set on them.</summary>
	[Fact]
	public void ToastDefaults_RememberSetters()
	{
		Service.ToastEntryAnimation = PDChatToastAnimation.Fade;
		Service.ToastExitAnimation = PDChatToastAnimation.Slide;
		Service.ToastAnimationDurationMs = 1;
		Service.ToastAutoDismiss = false;
		Service.ToastShowTitle = false;
		Service.ToastMinWidth = "1px";
		Service.ToastMaxWidth = "2px";
		Service.ToastMinHeight = "3px";
		Service.ToastMaxHeight = "4px";
		Service.ToastMaxVisible = 1;
		Service.ToastAnchor = PDChatButtonPosition.TopLeft;

		Service.ToastEntryAnimation.Should().Be(PDChatToastAnimation.Fade);
		Service.ToastExitAnimation.Should().Be(PDChatToastAnimation.Slide);
		Service.ToastAnimationDurationMs.Should().Be(1);
		Service.ToastAutoDismiss.Should().BeFalse();
		Service.ToastShowTitle.Should().BeFalse();
		Service.ToastMinWidth.Should().Be("1px");
		Service.ToastMaxWidth.Should().Be("2px");
		Service.ToastMinHeight.Should().Be("3px");
		Service.ToastMaxHeight.Should().Be("4px");
		Service.ToastMaxVisible.Should().Be(1);
		Service.ToastAnchor.Should().Be(PDChatButtonPosition.TopLeft);
	}

	/// <summary>A value set through the defaults belongs to that service instance, not to every service.</summary>
	[Fact]
	public void DefaultSetters_AreRememberedPerInstance()
	{
		IChatService other = new MinimalChatService();

		Service.ToastMaxVisible = 2;
		Service.IsInputPermitted = false;

		other.ToastMaxVisible.Should().Be(5);
		other.IsInputPermitted.Should().BeTrue();
	}

	/// <summary>Toast enabling and duration read and write the legacy last-message members.</summary>
	[Fact]
	public void ToastEnabledAndDuration_MapOntoLegacyMembers()
	{
		Service.ToastEnabled = false;
		Service.ToastDisplayDurationSeconds = 9;

#pragma warning disable CS0618 // Verifying the backing legacy members.
		_concrete.ShowLastMessage.Should().BeFalse();
		_concrete.ShowLastMessageDurationSeconds.Should().Be(9);
#pragma warning restore CS0618
		Service.ToastEnabled.Should().BeFalse();
		Service.ToastDisplayDurationSeconds.Should().Be(9);
	}

	/// <summary>Selecting the implicit conversation is accepted by a service that does not support conversations.</summary>
	[Fact]
	public void ActiveConversationId_AcceptsTheImplicitConversation()
	{
		Service.ActiveConversationId = ChatConversation.ImplicitConversationId;

		Service.ActiveConversationId.Should().Be(ChatConversation.ImplicitConversationId);
	}

	/// <summary>
	/// Selecting any other conversation is refused, rather than leaving the caller believing it is now looking at
	/// a conversation the service does not have.
	/// </summary>
	[Fact]
	public void ActiveConversationId_RefusesAnUnknownConversation()
	{
		var unknown = Guid.NewGuid();

		var act = () => Service.ActiveConversationId = unknown;

		act.Should().Throw<InvalidOperationException>().WithMessage($"*{unknown}*");
		Service.ActiveConversationId.Should().Be(ChatConversation.ImplicitConversationId);
	}

	/// <summary>The conversation event accepts subscriptions and their removal, and never fires.</summary>
	[Fact]
	public void OnConversationMessageReceived_AcceptsSubscriptionsAndNeverFires()
	{
		var fired = 0;
		Action<Guid, ChatMessage> handler = (_, _) => fired++;

		Service.OnConversationMessageReceived += handler;
		Service.SendMessage(new ChatMessage { Id = Guid.NewGuid(), Sender = new ChatMessageSender { Name = "u", IsUser = true }, Message = "hi", Type = MessageType.Normal });
		Service.OnConversationMessageReceived -= handler;

		fired.Should().Be(0);
		_concrete.Sent.Should().Be(1);
	}

	private sealed class MinimalChatService : TestChatServiceBase
	{
		private readonly List<ChatMessage> _sent = [];

		public int Sent => _sent.Count;

		public override IReadOnlyList<ChatMessage> Messages => [];

		public override void SendMessage(ChatMessage chatMessage) => _sent.Add(chatMessage);

		public override void ClearMessages() => _sent.Clear();
	}
}
