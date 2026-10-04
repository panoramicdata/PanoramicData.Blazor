using AngleSharp.Dom;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Helpers that render a <see cref="PDChat"/> with conversations and find its conversation toolbar, sidebar rows
/// and tabs.
/// </summary>
public partial class PDChatTests
{
	private (IRenderedComponent<PDChat> Component, FakeConversationStore Store, FakeChatService Service) RenderConversations(bool storeFails = false)
	{
		var store = new FakeConversationStore(failTranscripts: storeFails);
		var service = new FakeChatService
		{
			DockMode = PDChatDockMode.FullScreen,
			SupportsConversations = true,
			ActiveConversationId = store.First.Id
		};
		service.ConversationStore[store.First.Id] = [Message("From the service")];
		var component = RenderChat(service, p => p.Add(x => x.ConversationService, store));
		return (component, store, service);
	}

	private static IElement ToolbarButton(IRenderedComponent<PDChat> component, string text)
		=> component.FindAll(".pdchat-conversation-toolbar-btn").Single(b => b.TextContent.Trim() == text);

	private static IElement SidebarRow(IRenderedComponent<PDChat> component, string title)
		=> component.FindAll(".pdchat-conversation-row").First(r => r.TextContent.Contains(title, StringComparison.Ordinal));

	private static IElement Tab(IRenderedComponent<PDChat> component, string title)
		=> component.FindAll(".pdchat-conversation-tabset button.pdtabset-tab")
			.Single(t => t.QuerySelector(".pdtabset-tab-title")?.TextContent == title);

	private static List<string> TabTitles(IRenderedComponent<PDChat> component)
		=> [.. component.FindAll(".pdchat-conversation-tabset .pdtabset-tab-title").Select(t => t.TextContent)];

	private static string ActiveTabTitle(IRenderedComponent<PDChat> component)
		=> component.Find(".pdchat-conversation-tabset .pdtabset-tab.active .pdtabset-tab-title").TextContent;
}
