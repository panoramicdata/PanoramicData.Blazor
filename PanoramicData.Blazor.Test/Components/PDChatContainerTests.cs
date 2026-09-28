using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDChatContainer"/> lays the chat and content panels out for the chat's dock mode.
/// </summary>
public class PDChatContainerTests : BunitContext
{
	private readonly FakeChatService _chat = new();

	/// <summary>Sets up the rendering context.</summary>
	public PDChatContainerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDChatContainer> RenderContainer(Action<PDChatDockMode>? dockModeChanged = null)
		=> Render<PDChatContainer>(parameters =>
		{
			parameters
				.Add(p => p.ChatService, _chat)
				.Add(p => p.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p class=\"main\">Main</p>")))
				.Add(p => p.ChatContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p class=\"chat\">Chat</p>")));
			if (dockModeChanged != null)
			{
				parameters.Add(p => p.DockModeChanged, dockModeChanged);
			}
		});

	/// <summary>Docked left, the chat panel comes first and the container is in split mode.</summary>
	[Fact]
	public void DockedLeft_PutsTheChatPanelFirst()
	{
		_chat.DockMode = PDChatDockMode.Left;

		var component = RenderContainer();

		var panels = component.FindAll(".pdsplitpanel");
		panels.Should().HaveCount(2);
		panels[0].ClassList.Should().Contain("pdchat-chat-panel");
		panels[0].QuerySelector("p.chat").Should().NotBeNull();
		panels[1].ClassList.Should().Contain("pdchat-content-panel");
		panels[1].QuerySelector("p.main").Should().NotBeNull();
		component.Find(".pdchat-container-wrapper").ClassList.Should().NotContain("pdchat-split-inactive");
		component.Instance.IsSplitMode.Should().BeTrue();
	}

	/// <summary>Docked right, the content panel comes first and the container is in split mode.</summary>
	[Fact]
	public void DockedRight_PutsTheContentPanelFirst()
	{
		_chat.DockMode = PDChatDockMode.Right;

		var component = RenderContainer();

		var panels = component.FindAll(".pdsplitpanel");
		panels[0].ClassList.Should().Contain("pdchat-content-panel");
		panels[1].ClassList.Should().Contain("pdchat-chat-panel");
		component.Instance.IsSplitMode.Should().BeTrue();
	}

	/// <summary>A floating dock mode keeps both panels mounted but marks the split inactive.</summary>
	[Theory]
	[InlineData(PDChatDockMode.Minimized)]
	[InlineData(PDChatDockMode.BottomRight)]
	[InlineData(PDChatDockMode.FullScreen)]
	public void NonSplitMode_MarksTheSplitInactive_ButKeepsBothPanels(PDChatDockMode mode)
	{
		_chat.DockMode = mode;

		var component = RenderContainer();

		component.Find(".pdchat-container-wrapper").ClassList.Should().Contain("pdchat-split-inactive");
		component.FindAll(".pdsplitpanel").Should().HaveCount(2);
		component.Instance.IsSplitMode.Should().BeFalse();
	}

	/// <summary>An internal dock mode change updates the service, re-renders and notifies the listener.</summary>
	[Fact]
	public async Task InternalDockModeChange_UpdatesTheService_AndRaisesDockModeChanged()
	{
		_chat.DockMode = PDChatDockMode.Minimized;
		var raised = new List<PDChatDockMode>();
		var component = RenderContainer(raised.Add);

		await component.InvokeAsync(() => component.Instance.OnInternalDockModeChanged(PDChatDockMode.Right));

		_chat.DockMode.Should().Be(PDChatDockMode.Right);
		raised.Should().Equal(PDChatDockMode.Right);
		component.Find(".pdchat-container-wrapper").ClassList.Should().NotContain("pdchat-split-inactive");
	}

	/// <summary>With no listener an internal dock mode change still updates the service.</summary>
	[Fact]
	public async Task InternalDockModeChange_WithoutAListener_StillUpdatesTheService()
	{
		var component = RenderContainer();

		await component.InvokeAsync(() => component.Instance.OnInternalDockModeChanged(PDChatDockMode.Left));

		_chat.DockMode.Should().Be(PDChatDockMode.Left);
	}

	/// <summary>The chat content receives the container as a named cascading value.</summary>
	[Fact]
	public void ChatContent_ReceivesTheContainerAsACascadingValue()
	{
		_chat.DockMode = PDChatDockMode.Left;

		var component = Render<PDChatContainer>(parameters => parameters
			.Add(p => p.ChatService, _chat)
			.Add(p => p.ChatContent, (RenderFragment)(b =>
			{
				b.OpenComponent<ContainerProbe>(0);
				b.CloseComponent();
			})));

		component.FindComponent<ContainerProbe>().Instance.Container.Should().BeSameAs(component.Instance);
	}

	/// <summary>A component that captures the cascading chat container.</summary>
	private sealed class ContainerProbe : ComponentBase
	{
		/// <summary>The captured container.</summary>
		[CascadingParameter(Name = "ChatContainer")]
		public PDChatContainer? Container { get; set; }
	}

	/// <summary>A chat service double with no messages.</summary>
	private sealed class FakeChatService : TestChatServiceBase
	{
		public override IReadOnlyList<ChatMessage> Messages { get; } = [];

		public override void SendMessage(ChatMessage chatMessage) => RaiseMessageReceived(chatMessage);

		public override void ClearMessages()
		{
			// nothing to clear: the double holds no messages
		}
	}
}
