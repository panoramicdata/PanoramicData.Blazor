using System.Globalization;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDChatConversationSidebar"/> lists, searches, filters and pages conversations, and
/// degrades to a visible message when the store fails.
/// </summary>
public class PDChatConversationSidebarTests : BunitContext
{
	private readonly FakeConversationService _service = new();

	/// <summary>Sets up the rendering context.</summary>
	public PDChatConversationSidebarTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Each conversation renders a row with its name, relative time and archived marker.</summary>
	[Fact]
	public void Rows_show_name_time_selection_and_archived_state()
	{
		var selected = Add("Selected", TimeSpan.Zero);
		Add("Old", TimeSpan.FromMinutes(5), archived: true);
		var component = RenderSidebar(p => p.Add(x => x.SelectedConversationId, selected.Id));
		component.FindAll(".pdchat-conversation-row").Should().ContainSingle("archived conversations are excluded by default");

		component.Find(".pdchat-conversation-include-archived input").Change(true);

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
	public void Clicking_a_row_selects_the_conversation()
	{
		var target = Add("Target", TimeSpan.Zero);
		ChatConversation? picked = null;
		var component = RenderSidebar(p => p.Add(x => x.OnConversationSelected, c => picked = c));

		component.Find(".pdchat-conversation-row").Click();

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

	/// <summary>While the first page is loading the list says so.</summary>
	[Fact]
	public void The_first_load_shows_loading()
	{
		_service.Gate = new TaskCompletionSource();
		var component = RenderSidebar();

		component.Find(".pdchat-conversation-empty").TextContent.Should().Be("Loading…");

		_service.Gate.SetResult();
		component.WaitForAssertion(() => component.Find(".pdchat-conversation-empty").TextContent.Trim().Should().Be("No conversations yet."));
	}

	/// <summary>Typing searches after the debounce and passes the text; no match says so.</summary>
	[Fact]
	public void Typing_searches_after_a_debounce()
	{
		Add("Alpha", TimeSpan.Zero);
		var component = RenderSidebar();

		component.Find("input[type=search]").Input("zzz");

		component.WaitForAssertion(() => component.Find(".pdchat-conversation-empty").TextContent.Trim()
			.Should().Be("No conversations match your search."));
		_service.Queries.Last().SearchText.Should().Be("zzz");
	}

	/// <summary>A keystroke that supersedes another within the debounce means only the last text is searched.</summary>
	[Fact]
	public void A_superseded_keystroke_is_not_searched()
	{
		Add("Alpha", TimeSpan.Zero);
		var component = RenderSidebar();

		component.Find("input[type=search]").Input("Al");
		component.Find("input[type=search]").Input("Alp");

		component.WaitForAssertion(() => _service.Queries.Should().HaveCount(2));
		_service.Queries.Last().SearchText.Should().Be("Alp");
		component.FindAll(".pdchat-conversation-row").Should().ContainSingle();
	}

	/// <summary>An input event with no value searches for empty text.</summary>
	[Fact]
	public void A_null_input_value_searches_for_everything()
	{
		Add("Alpha", TimeSpan.Zero);
		var component = RenderSidebar();

		component.Find("input[type=search]").Input((object?)null!);

		component.WaitForAssertion(() => _service.Queries.Should().HaveCount(2));
		_service.Queries.Last().SearchText.Should().BeEmpty();
	}

	/// <summary>The search mode buttons are hidden unless the store supports semantic search.</summary>
	[Fact]
	public void Search_modes_are_hidden_without_semantic_support()
	{
		var component = RenderSidebar();

		component.FindAll(".pdchat-conversation-search-mode").Should().BeEmpty();
	}

	/// <summary>Choosing semantic search re-queries in that mode; choosing the current mode again does nothing.</summary>
	[Fact]
	public void Switching_search_mode_requeries()
	{
		_service.Semantic = true;
		var component = RenderSidebar();
		var modes = component.FindAll(".pdchat-conversation-search-mode");
		modes[0].ClassList.Should().Contain("selected");

		modes[0].Click();
		_service.Queries.Should().ContainSingle();

		component.FindAll(".pdchat-conversation-search-mode")[1].Click();

		_service.Queries.Should().HaveCount(2);
		_service.Queries.Last().SearchMode.Should().Be(ChatConversationSearchMode.Semantic);
		component.FindAll(".pdchat-conversation-search-mode")[1].ClassList.Should().Contain("selected");
	}

	/// <summary>Ticking include archived re-queries with archived conversations included.</summary>
	[Fact]
	public void Include_archived_requeries()
	{
		var component = RenderSidebar();

		component.Find(".pdchat-conversation-include-archived input").Change(true);
		_service.Queries.Last().IncludeArchived.Should().BeTrue();

		component.Find(".pdchat-conversation-include-archived input").Change(false);
		_service.Queries.Last().IncludeArchived.Should().BeFalse();
	}

	/// <summary>Show more loads the next page after the rows already listed.</summary>
	[Fact]
	public void Show_more_loads_the_next_page()
	{
		for (var i = 0; i < 3; i++)
		{
			Add($"C{i}", TimeSpan.FromMinutes(i));
		}

		_service.PageSize = 2;
		var component = RenderSidebar();
		component.FindAll(".pdchat-conversation-row").Should().HaveCount(2);

		component.Find(".pdchat-conversation-more").Click();

		component.WaitForAssertion(() => component.FindAll(".pdchat-conversation-row").Should().HaveCount(3));
		_service.Queries.Last().Skip.Should().Be(2);
		component.FindAll(".pdchat-conversation-more").Should().BeEmpty();
	}

	/// <summary>While a further page loads, the more button is disabled and says it is loading.</summary>
	[Fact]
	public void Show_more_is_disabled_while_loading()
	{
		for (var i = 0; i < 3; i++)
		{
			Add($"C{i}", TimeSpan.Zero);
		}

		_service.PageSize = 2;
		var component = RenderSidebar();
		_service.Gate = new TaskCompletionSource();

		component.Find(".pdchat-conversation-more").Click();

		var more = component.Find(".pdchat-conversation-more");
		more.HasAttribute("disabled").Should().BeTrue();
		more.TextContent.Trim().Should().Be("Loading…");
		_service.Gate.SetResult();
	}

	/// <summary>A store failure shows the message, and Try again reloads the list.</summary>
	[Fact]
	public void A_failure_shows_a_message_and_retry_reloads()
	{
		_service.Failure = new InvalidOperationException("store down");
		var component = RenderSidebar();

		component.Find(".pdchat-conversation-error-text").TextContent.Should().Be("Could not load conversations: store down");

		_service.Failure = null;
		Add("Back", TimeSpan.Zero);
		component.Find(".pdchat-conversation-retry").Click();

		component.WaitForAssertion(() => component.FindAll(".pdchat-conversation-row").Should().ContainSingle());
		component.FindAll(".pdchat-conversation-error").Should().BeEmpty();
	}

	/// <summary>A store that throws a cancellation is treated as superseded, not as a failure.</summary>
	[Fact]
	public void A_cancelled_load_is_not_reported_as_a_failure()
	{
		_service.Failure = new OperationCanceledException();
		var component = RenderSidebar();

		component.FindAll(".pdchat-conversation-error").Should().BeEmpty();
	}

	/// <summary>ReloadAsync lists again from the first page.</summary>
	[Fact]
	public async Task ReloadAsync_lists_again_from_the_start()
	{
		var component = RenderSidebar();
		Add("Later", TimeSpan.Zero);

		await component.InvokeAsync(component.Instance.ReloadAsync);

		component.FindAll(".pdchat-conversation-row").Should().ContainSingle();
		_service.Queries.Last().Skip.Should().Be(0);
	}

	/// <summary>A load that completes after the sidebar is disposed does not throw or report anything.</summary>
	[Fact]
	public void Disposing_during_a_load_is_harmless()
	{
		_service.Gate = new TaskCompletionSource();
		var loads = 0;
		var component = RenderSidebar(p => p.Add(x => x.OnConversationsLoaded, _ => loads++));

		component.Instance.Dispose();
		_service.Gate.SetResult();

		loads.Should().Be(0);
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

	/// <summary>An in-memory conversation store that records the queries it is given.</summary>
	private sealed class FakeConversationService : IChatConversationService
	{
		public List<ChatConversation> Conversations { get; } = [];

		public List<ChatConversationQuery> Queries { get; } = [];

		public bool Semantic { get; set; }

		public int PageSize { get; set; } = 50;

		public Exception? Failure { get; set; }

		public TaskCompletionSource? Gate { get; set; }

		public bool SupportsSemanticSearch => Semantic;

		public async Task<ChatConversationPage> ListAsync(ChatConversationQuery query, CancellationToken cancellationToken)
		{
			Queries.Add(query);
			if (Gate is { } gate)
			{
				await gate.Task;
			}

			cancellationToken.ThrowIfCancellationRequested();
			if (Failure is { } failure)
			{
				throw failure;
			}

			var matches = Conversations
				.Where(c => query.IncludeArchived || !c.IsArchived)
				.Where(c => !query.HasSearchText || c.DisplayName.Contains(query.SearchText!, StringComparison.OrdinalIgnoreCase))
				.ToList();
			return new ChatConversationPage
			{
				Conversations = [.. matches.Skip(query.Skip).Take(PageSize)],
				HasMore = query.Skip + PageSize < matches.Count
			};
		}

		public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid id, CancellationToken cancellationToken)
			=> throw NotUsed(cancellationToken, id);

		public Task<ChatConversation> CreateAsync(CancellationToken cancellationToken)
			=> throw NotUsed(cancellationToken);

		public Task RenameAsync(Guid id, string title, CancellationToken cancellationToken)
			=> throw NotUsed(cancellationToken, id, title);

		public Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
			=> throw NotUsed(cancellationToken, id);

		public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken)
			=> throw NotUsed(cancellationToken, id);

		/// <summary>The sidebar only lists, so any other call is a test failure worth naming.</summary>
		private static NotSupportedException NotUsed(CancellationToken cancellationToken, params object[] arguments)
			=> new($"Not used by the sidebar (arguments: {string.Join(", ", arguments)}; cancellable: {cancellationToken.CanBeCanceled})");
	}
}
