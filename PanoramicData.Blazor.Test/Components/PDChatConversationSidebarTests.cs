using System.Globalization;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDChatConversationSidebar"/> lists, searches, filters and pages conversations, and
/// degrades to a visible message when the store fails.
/// </summary>
public partial class PDChatConversationSidebarTests : BunitContext
{
	/// <summary>
	/// How long to wait for a render that follows the 300ms search debounce or a store call. Generous on
	/// purpose: a loaded CI runner can take well over bUnit's one-second default to run a timer continuation.
	/// </summary>
	private static readonly TimeSpan DebounceTimeout = TimeSpan.FromSeconds(10);

	/// <summary>Sets up the rendering context.</summary>
	public PDChatConversationSidebarTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Each conversation renders a row with its name, relative time and archived marker.</summary>
	[Fact]
	public async Task Rows_show_name_time_selection_and_archived_state()
	{
		var selected = Add("Selected", TimeSpan.Zero);
		Add("Old", TimeSpan.FromMinutes(5), archived: true);
		var component = RenderSidebar(p => p.Add(x => x.SelectedConversationId, selected.Id));
		component.FindAll(".pdchat-conversation-row").Should().ContainSingle("archived conversations are excluded by default");

		await component.Find(".pdchat-conversation-include-archived input").ChangeAsync(new ChangeEventArgs { Value = true });

		var rows = component.FindAll(".pdchat-conversation-row");
		rows.Should().HaveCount(2);
		rows[0].ClassList.Should().Contain("selected");
		rows[0].QuerySelector(".pdchat-conversation-row-name")!.TextContent.Should().Be("Selected");
		rows[0].GetAttribute("title").Should().Be("Selected");
		rows[0].QuerySelector(".pdchat-conversation-row-archived").Should().BeNull();
		rows[1].ClassList.Should().Contain("archived");
		rows[1].QuerySelector(".pdchat-conversation-row-archived")!.TextContent.Should().Be("Archived");
	}

	/// <summary>The last activity reads as a relative time, falling back to a date after a week.</summary>
	[Fact]
	public void Last_activity_is_formatted_relative_to_now()
	{
		Add("a", TimeSpan.Zero);
		Add("b", TimeSpan.FromMinutes(5));
		Add("c", TimeSpan.FromHours(3));
		Add("d", TimeSpan.FromDays(2));
		var old = Add("e", TimeSpan.FromDays(30));
		var component = RenderSidebar();

		component.FindAll(".pdchat-conversation-row-time").Select(e => e.TextContent).Should().Equal(
			"just now",
			"5m ago",
			"3h ago",
			"2d ago",
			old.LastMessageUtc.ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture));
	}

	/// <summary>Clicking a row raises OnConversationSelected with that conversation.</summary>
	[Fact]
	public async Task Clicking_a_row_selects_the_conversation()
	{
		var target = Add("Target", TimeSpan.Zero);
		ChatConversation? picked = null;
		var component = RenderSidebar(p => p.Add(x => x.OnConversationSelected, c => picked = c));

		await component.Find(".pdchat-conversation-row").ClickAsync(new MouseEventArgs());

		picked.Should().BeSameAs(target);
	}

	/// <summary>Every successful load is reported through OnConversationsLoaded.</summary>
	[Fact]
	public void Loads_are_reported()
	{
		Add("One", TimeSpan.Zero);
		var loads = new List<int>();
		RenderSidebar(p => p.Add(x => x.OnConversationsLoaded, list => loads.Add(list.Count)));

		loads.Should().Equal(1);
	}

	/// <summary>An empty store says there are no conversations yet.</summary>
	[Fact]
	public void An_empty_store_says_no_conversations_yet()
	{
		var component = RenderSidebar();

		component.Find(".pdchat-conversation-empty").TextContent.Trim().Should().Be("No conversations yet.");
	}

	private ChatConversation Add(string title, TimeSpan age, bool archived = false)
	{
		var conversation = new ChatConversation
		{
			Id = Guid.NewGuid(),
			Title = title,
			IsArchived = archived,
			LastMessageUtc = DateTimeOffset.UtcNow - age
		};
		_service.Conversations.Add(conversation);
		return conversation;
	}

	private IRenderedComponent<PDChatConversationSidebar> RenderSidebar(
		Action<ComponentParameterCollectionBuilder<PDChatConversationSidebar>>? configure = null)
		=> Render<PDChatConversationSidebar>(parameters =>
		{
			parameters.Add(p => p.ConversationService, _service);
			configure?.Invoke(parameters);
		});
}
