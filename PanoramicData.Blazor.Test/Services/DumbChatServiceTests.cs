using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>
/// Tests for the configuration, lifecycle and seeding members of <see cref="DumbChatService"/>. The
/// conversation routing is covered by <c>DumbChatServiceConversationTests</c> and the simulated replies by
/// <see cref="DumbChatServiceReplyTests"/>.
/// </summary>
public sealed class DumbChatServiceTests : IDisposable
{
	private readonly DumbChatService _service = new();

	/// <inheritdoc />
	public void Dispose() => _service.Dispose();

	/// <summary>The configuration properties that announce a change through <see cref="DumbChatService.OnConfigurationChanged"/>.</summary>
	public static TheoryData<string> ConfigurationProperties =>
	[
		nameof(DumbChatService.RestoreMode),
		nameof(DumbChatService.MinimizedButtonPosition),
		nameof(DumbChatService.Title),
		nameof(DumbChatService.IsMaximizePermitted),
		nameof(DumbChatService.IsCanvasUsePermitted),
		nameof(DumbChatService.IsClearPermitted),
		nameof(DumbChatService.IsInputPermitted),
		nameof(DumbChatService.InputDisabledMessage),
		nameof(DumbChatService.AutoRestoreOnNewMessage),
		nameof(DumbChatService.UseFullWidthMessages),
		nameof(DumbChatService.MessageMetadataDisplayMode),
		nameof(DumbChatService.ShowMessageUserIcon),
		nameof(DumbChatService.ShowMessageUserName),
		nameof(DumbChatService.ShowMessageTimestamp),
		nameof(DumbChatService.MessageTimestampFormat),
		nameof(DumbChatService.ToastEnabled),
		nameof(DumbChatService.ToastDisplayDurationSeconds),
		nameof(DumbChatService.ToastEntryAnimation),
		nameof(DumbChatService.ToastExitAnimation),
		nameof(DumbChatService.ToastAnimationDurationMs),
		nameof(DumbChatService.ToastAutoDismiss),
		nameof(DumbChatService.ToastShowTitle),
		nameof(DumbChatService.ToastMinWidth),
		nameof(DumbChatService.ToastMaxWidth),
		nameof(DumbChatService.ToastMinHeight),
		nameof(DumbChatService.ToastMaxHeight),
		nameof(DumbChatService.ToastMaxVisible),
		nameof(DumbChatService.ToastAnchor)
	];

	private static object DifferentValue(object? current, Type type) => current switch
	{
		bool b => !b,
		string s => s + "x",
		int i => i + 1,
		double d => d + 1,
		Enum e => Enum.GetValues(type).Cast<object>().First(v => !v.Equals(e)),
		_ => "set"
	};

	/// <summary>Changing a configuration property stores it and announces it once; re-setting the same value is silent.</summary>
	[Theory]
	[MemberData(nameof(ConfigurationProperties))]
	public void ConfigurationProperty_AnnouncesChangesOnly(string propertyName)
	{
		var property = typeof(DumbChatService).GetProperty(propertyName)!;
		var changes = 0;
		_service.OnConfigurationChanged += () => changes++;
		var value = DifferentValue(property.GetValue(_service), property.PropertyType);

		property.SetValue(_service, value);
		property.SetValue(_service, value);

		property.GetValue(_service).Should().Be(value);
		changes.Should().Be(1);
	}

	/// <summary>Changing the preferred dock mode announces the new mode once.</summary>
	[Fact]
	public void PreferredDockMode_AnnouncesChange()
	{
		var modes = new List<PDChatDockMode>();
		_service.OnDockModeChanged += modes.Add;
		_service.PreferredDockMode.Should().Be(PDChatDockMode.Right);

		_service.PreferredDockMode = PDChatDockMode.Left;
		_service.PreferredDockMode = PDChatDockMode.Left;

		_service.PreferredDockMode.Should().Be(PDChatDockMode.Left);
		modes.Should().Equal(PDChatDockMode.Left);
	}

	/// <summary>Muting announces the new state once.</summary>
	[Fact]
	public void IsMuted_AnnouncesChange()
	{
		var states = new List<bool>();
		_service.OnMuteStatusChanged += states.Add;

		_service.IsMuted = true;
		_service.IsMuted = true;
		_service.IsMuted = false;

		states.Should().Equal(true, false);
	}

	/// <summary>Changing the property values with no subscribers does nothing untoward.</summary>
	[Fact]
	public void PropertyChanges_WithoutSubscribers_DoNotThrow()
	{
		var act = () =>
		{
			_service.Title = "New";
			_service.IsMuted = true;
			_service.PreferredDockMode = PDChatDockMode.TopLeft;
		};

		act.Should().NotThrow();
	}

	/// <summary>The obsolete last-message members map onto the toast settings.</summary>
	[Fact]
	public void ObsoleteLastMessageMembers_MapOntoToastSettings()
	{
#pragma warning disable CS0618 // Exercising the members kept for backward compatibility.
		_service.ShowLastMessage = false;
		_service.ShowLastMessageDurationSeconds = 12;

		_service.ToastEnabled.Should().BeFalse();
		_service.ToastDisplayDurationSeconds.Should().Be(12);
		_service.ShowLastMessage.Should().BeFalse();
		_service.ShowLastMessageDurationSeconds.Should().Be(12);
#pragma warning restore CS0618
	}

	/// <summary>The service is not live until it is initialised, and initialising twice is harmless.</summary>
	[Fact]
	public void Initialize_MakesServiceLive()
	{
		_service.IsLive.Should().BeFalse();

		_service.Initialize();
		_service.Initialize();

		_service.IsLive.Should().BeTrue();
	}

	/// <summary>Changing the dock mode announces it; asking for the current mode changes nothing.</summary>
	[Fact]
	public async Task SetDockModeAsync_AnnouncesOnlyChanges()
	{
		var modes = new List<PDChatDockMode>();
		_service.OnDockModeChanged += modes.Add;

		await _service.SetDockModeAsync(PDChatDockMode.FullScreen);
		await _service.SetDockModeAsync(PDChatDockMode.FullScreen);

		_service.DockMode.Should().Be(PDChatDockMode.FullScreen);
		modes.Should().Equal(PDChatDockMode.FullScreen);
	}

	/// <summary>Seeding records a message in a conversation, creating it, without announcing anything.</summary>
	[Fact]
	public void SeedMessage_RecordsSilently()
	{
		var conversation = Guid.NewGuid();
		var announced = 0;
		_service.OnConversationMessageReceived += (_, _) => announced++;
		var message = new ChatMessage { Id = Guid.NewGuid(), Sender = DumbChatService.TimeBot, Message = "old", Type = MessageType.Normal };

		_service.SeedMessage(conversation, message);

		_service.GetMessages(conversation).Should().ContainSingle().Which.Should().BeSameAs(message);
		_service.ConversationIds.Should().Contain(conversation);
		announced.Should().Be(0);
	}

	/// <summary>Creating a conversation adds an empty one without selecting it; ensuring an existing one reports false.</summary>
	[Fact]
	public void CreateAndEnsureConversation_ManageTheSet()
	{
		var id = _service.CreateConversation();

		_service.GetMessages(id).Should().BeEmpty();
		_service.ActiveConversationId.Should().Be(ChatConversation.ImplicitConversationId);
		_service.EnsureConversation(id).Should().BeFalse();
		_service.EnsureConversation(Guid.NewGuid()).Should().BeTrue();
	}

	/// <summary>The bot senders are named, non-human and not the user.</summary>
	[Fact]
	public void Bots_AreNonHumanSenders()
	{
		DumbChatService.TimeBot.Name.Should().Be("TimeBot");
		DumbChatService.DumbBot.Name.Should().Be("DumbBot");
		DumbChatService.DumbBot.IsUser.Should().BeFalse();
		DumbChatService.DumbBot.IsHuman.Should().BeFalse();
		DumbChatService.TimeBot.IsSupport.Should().BeFalse();
	}

	/// <summary>The demonstration form holds a valid question of every answer kind.</summary>
	[Fact]
	public void BuildDemonstrationForm_CoversEveryAnswerKind()
	{
		var form = DumbChatService.BuildDemonstrationForm();

		form.Questions.Select(q => q.Kind).Distinct().Should().BeEquivalentTo(Enum.GetValues<ChatFormAnswerKind>());
		form.Questions.Select(q => q.Id).Should().OnlyHaveUniqueItems();
		form.Questions.Where(q => q.Scale is not null).Should().OnlyContain(q => q.Scale!.IsValid && q.Scale.HasUsablePointLabels);
		DumbChatService.BuildDemonstrationForm().Id.Should().NotBe(form.Id);
	}
}
