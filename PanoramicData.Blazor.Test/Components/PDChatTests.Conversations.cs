using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Canvas, form and multi-conversation tests for <see cref="PDChat"/>.
/// </summary>
public partial class PDChatTests
{
	// ------------------------------------------------------------------------------------------
	// Canvas layout
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that full screen with canvas permitted splits the window into a canvas and the transcript.</summary>
	[Fact]
	public void Full_screen_with_canvas_shows_the_splitter()
	{
		var service = new FakeChatService { DockMode = PDChatDockMode.FullScreen };
		service.Store.Add(Message("In the split"));

		var component = RenderChat(service);

		component.Find(".pdchat-splitter").Should().NotBeNull();
		component.Find(".pdchat-canvas-flex .pdtabset").Should().NotBeNull();
		component.Find(".pdchat-text").TextContent.Should().Contain("In the split");
	}

	// ------------------------------------------------------------------------------------------
	// Inline forms
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that a submitted form is sent as an ordinary message describing the answers.</summary>
	[Fact]
	public async Task A_submitted_form_is_sent_as_a_message()
	{
		var reported = new List<ChatMessage>();
		var service = new FakeChatService();
		var component = RenderChat(service, p => p.Add(x => x.OnMessageSent, (ChatMessage m) => reported.Add(m)));
		var formContext = GetCascadedFormContext(component);
		var submission = Submission(("Colour", "Blue", false));

		await component.InvokeAsync(() => formContext.OnSubmitted!(submission));

		var sent = service.Sent.Should().ContainSingle().Subject;
		sent.Message.Should().Be("Colour - Blue");
		sent.FormSubmission.Should().BeSameAs(submission);
		reported.Should().ContainSingle();
	}

	/// <summary>Verifies that a dismissed form sends nothing.</summary>
	[Fact]
	public async Task A_dismissed_form_sends_nothing()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);
		var formContext = GetCascadedFormContext(component);

		await component.InvokeAsync(() => formContext.OnDismissed!(Guid.NewGuid()));

		service.Sent.Should().BeEmpty();
	}

	/// <summary>Verifies that a submission is described line by line, with skipped questions listed.</summary>
	[Fact]
	public void A_submission_is_described_line_by_line()
	{
		var text = PDChat.DescribeSubmission(Submission(("Colour", "Blue", false), ("Size", null, true)));

		text.Should().Be($"Colour - Blue{Environment.NewLine}Size - skipped");
	}

	/// <summary>Verifies that a submission with no answers says so.</summary>
	[Fact]
	public void A_submission_with_no_answers_says_so()
	{
		PDChat.DescribeSubmission(Submission()).Should().Be("(no answers)");
	}

	// ------------------------------------------------------------------------------------------
	// Conversation tabs
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that entering full screen opens the conversation the service is already on.</summary>
	[Fact]
	public void Full_screen_opens_the_active_conversation()
	{
		var (component, store, _) = RenderConversations();

		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);
		component.Find(".pdchat-text").TextContent.Should().Contain("First message");
		store.MessageRequests.Should().Contain(store.First.Id);
	}

	/// <summary>Verifies that opening a second conversation from the sidebar adds and selects its tab.</summary>
	[Fact]
	public async Task Opening_another_conversation_adds_and_selects_its_tab()
	{
		var (component, store, service) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await SidebarRow(component, "Second").ClickAsync(new());

		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"), Patience);
		TabTitles(component).Should().Equal("First", "Second");
		service.ActiveConversationId.Should().Be(store.Second.Id);
	}

	/// <summary>Verifies that opening an already open conversation selects its tab rather than adding another.</summary>
	[Fact]
	public async Task Opening_an_open_conversation_does_not_duplicate_it()
	{
		var (component, _, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await SidebarRow(component, "First").ClickAsync(new());

		TabTitles(component).Should().Equal("First");
	}

	/// <summary>Verifies that clicking back to a tab shows that conversation's transcript.</summary>
	[Fact]
	public async Task Selecting_a_tab_shows_its_transcript()
	{
		var (component, _, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);
		await SidebarRow(component, "Second").ClickAsync(new());
		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"), Patience);

		await Tab(component, "First").ClickAsync(new());

		component.WaitForAssertion(() => component.Find(".pdchat-text").TextContent.Should().Contain("First message"), Patience);
	}

	/// <summary>Verifies that a reply for a conversation in the background marks its tab unread until selected.</summary>
	[Fact]
	public async Task A_background_reply_marks_its_tab_unread()
	{
		var (component, store, service) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);
		await SidebarRow(component, "Second").ClickAsync(new());
		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"), Patience);

		await component.InvokeAsync(() => service.ReceiveFor(store.First.Id, Message("Answer")));

		// The tab strip is drawn by PDTabSet before the PDTab beneath it receives its new CssClass, so the
		// marker only shows on the render after the one the reply caused (reported separately as a defect).
		component.Render();
		component.WaitForAssertion(() => Tab(component, "First").ClassList.Should().Contain("pdchat-conversation-tab-unread"), Patience);

		await Tab(component, "First").ClickAsync(new());
		component.WaitForAssertion(() => Tab(component, "First").ClassList.Should().NotContain("pdchat-conversation-tab-unread"), Patience);
	}

	/// <summary>Verifies that replies for the conversation on screen, or for one not open, mark nothing.</summary>
	[Fact]
	public async Task Replies_for_the_current_or_an_unopened_conversation_mark_nothing()
	{
		var (component, store, service) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await component.InvokeAsync(() => service.ReceiveFor(store.First.Id, Message("Here")));
		await component.InvokeAsync(() => service.ReceiveFor(store.Second.Id, Message("Elsewhere")));

		component.FindAll(".pdchat-conversation-tab-unread").Should().BeEmpty();
	}

	/// <summary>Verifies that closing the selected tab falls back to another open one, and then to the empty state.</summary>
	[Fact]
	public async Task Closing_tabs_falls_back_then_shows_the_empty_state()
	{
		var (component, _, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);
		await SidebarRow(component, "Second").ClickAsync(new());
		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"), Patience);

		await Tab(component, "Second").QuerySelector(".pdtabset-tab-close")!.ClickAsync(new());
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);
		await Tab(component, "First").QuerySelector(".pdtabset-tab-close")!.ClickAsync(new());

		component.WaitForAssertion(() => component.Find(".pdchat-conversation-none-open").Should().NotBeNull(), Patience);
		component.FindAll(".pdchat-message").Should().BeEmpty();
	}

	/// <summary>Verifies that closing a tab in the background leaves the selected conversation on screen.</summary>
	[Fact]
	public async Task Closing_a_background_tab_keeps_the_selection()
	{
		var (component, _, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);
		await SidebarRow(component, "Second").ClickAsync(new());
		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"), Patience);

		await Tab(component, "First").QuerySelector(".pdtabset-tab-close")!.ClickAsync(new());

		component.WaitForAssertion(() => TabTitles(component).Should().Equal("Second"), Patience);
		ActiveTabTitle(component).Should().Be("Second");
	}

	/// <summary>Verifies that the empty state's button starts a new conversation in the store.</summary>
	[Fact]
	public async Task The_empty_state_starts_a_new_conversation()
	{
		var (component, store, service) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);
		await Tab(component, "First").QuerySelector(".pdtabset-tab-close")!.ClickAsync(new());

		await component.Find(".pdchat-conversation-none-open button").ClickAsync(new());

		component.WaitForAssertion(() => TabTitles(component).Should().Equal(ChatConversation.UntitledDisplayName), Patience);
		store.Created.Should().ContainSingle();
		service.ActiveConversationId.Should().Be(store.Created[0].Id);
	}

	/// <summary>Verifies that the tab set's add button also starts a new conversation.</summary>
	[Fact]
	public async Task The_add_tab_button_starts_a_new_conversation()
	{
		var (component, store, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await component.Find(".pdchat-conversation-tabset .pdtabset-addtab").ClickAsync(new());

		component.WaitForAssertion(() => store.Created.Should().ContainSingle(), Patience);
	}

	/// <summary>Verifies that renaming a tab writes the title through to the store.</summary>
	[Fact]
	public async Task Renaming_a_tab_writes_through_to_the_store()
	{
		var (component, store, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await Tab(component, "First").DoubleClickAsync(new MouseEventArgs());
		await component.Find(".pdtabset-tab-rename-input").InputAsync(new ChangeEventArgs { Value = "Renamed" });
		await component.Find(".pdtabset-tab-rename-input").BlurAsync(new FocusEventArgs());

		component.WaitForAssertion(() => store.Renamed.Should().ContainSingle().Which.Should().Be((store.First.Id, "Renamed")), Patience);
		store.First.Title.Should().Be("Renamed");
	}

	/// <summary>Verifies that archiving the selected conversation archives it in the store and closes its tab.</summary>
	[Fact]
	public async Task Archiving_archives_and_closes_the_tab()
	{
		var (component, store, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await ToolbarButton(component, "Archive").ClickAsync(new());

		store.Archived.Should().Equal(store.First.Id);
		component.WaitForAssertion(() => TabTitles(component).Should().BeEmpty(), Patience);
		ToolbarButton(component, "Archive").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Verifies that the sidebar can be collapsed and shown again.</summary>
	[Fact]
	public async Task The_sidebar_can_be_collapsed_and_shown()
	{
		var (component, _, _) = RenderConversations();

		await component.Find(".pdchat-conversation-toolbar-btn[title='Hide conversations']").ClickAsync(new());
		component.FindAll(".pdchat-conversation-sidebar-pane").Should().BeEmpty();

		await component.Find(".pdchat-conversation-toolbar-btn[title='Show conversations']").ClickAsync(new());
		component.FindAll(".pdchat-conversation-sidebar-pane").Should().ContainSingle();
	}

	/// <summary>Verifies that a transcript the store cannot supply falls back to the chat service's own copy.</summary>
	[Fact]
	public void A_failing_store_transcript_falls_back_to_the_service()
	{
		var (component, _, _) = RenderConversations(storeFails: true);

		component.WaitForAssertion(() => component.Find(".pdchat-text").TextContent.Should().Contain("From the service"), Patience);
	}
}
