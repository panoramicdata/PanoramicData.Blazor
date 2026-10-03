using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The conversation service double used by the <see cref="PDChatConversationSidebar"/> tests, and the tests that
/// make it wait, fail or offer semantic search.
/// </summary>
public partial class PDChatConversationSidebarTests
{
	private readonly FakeConversationService _service = new();

	/// <summary>While the first page is loading the list says so.</summary>
	[Fact]
	public void The_first_load_shows_loading()
	{
		_service.Gate = new TaskCompletionSource();
		var component = RenderSidebar();

		component.Find(".pdchat-conversation-empty").TextContent.Should().Be("Loading…");

		_service.Gate.SetResult();
		component.WaitForAssertion(() => component.Find(".pdchat-conversation-empty").TextContent.Trim().Should().Be("No conversations yet."), DebounceTimeout);
	}

	/// <summary>Choosing semantic search re-queries in that mode; choosing the current mode again does nothing.</summary>
	[Fact]
	public async Task Switching_search_mode_requeries()
	{
		_service.Semantic = true;
		var component = RenderSidebar();
		var modes = component.FindAll(".pdchat-conversation-search-mode");
		modes[0].ClassList.Should().Contain("selected");

		await modes[0].ClickAsync(new MouseEventArgs());
		_service.Queries.Should().ContainSingle();

		await component.FindAll(".pdchat-conversation-search-mode")[1].ClickAsync(new MouseEventArgs());

		_service.Queries.Should().HaveCount(2);
		_service.Queries.Last().SearchMode.Should().Be(ChatConversationSearchMode.Semantic);
		component.FindAll(".pdchat-conversation-search-mode")[1].ClassList.Should().Contain("selected");
	}

	/// <summary>While a further page loads, the more button is disabled and says it is loading.</summary>
	[Fact]
	public async Task Show_more_is_disabled_while_loading()
	{
		for (var i = 0; i < 3; i++)
		{
			Add($"C{i}", TimeSpan.Zero);
		}

		_service.PageSize = 2;
		var component = RenderSidebar();
		_service.Gate = new TaskCompletionSource();

		// Not awaited yet: the click completes only when the gated store returns.
		var click = component.Find(".pdchat-conversation-more").ClickAsync(new MouseEventArgs());

		component.WaitForAssertion(() =>
		{
			var more = component.Find(".pdchat-conversation-more");
			more.HasAttribute("disabled").Should().BeTrue();
			more.TextContent.Trim().Should().Be("Loading…");
		}, DebounceTimeout);
		_service.Gate.SetResult();
		await click;
	}

	/// <summary>A store failure shows the message, and Try again reloads the list.</summary>
	[Fact]
	public async Task A_failure_shows_a_message_and_retry_reloads()
	{
		_service.Failure = new InvalidOperationException("store down");
		var component = RenderSidebar();

		component.Find(".pdchat-conversation-error-text").TextContent.Should().Be("Could not load conversations: store down");

		_service.Failure = null;
		Add("Back", TimeSpan.Zero);
		await component.Find(".pdchat-conversation-retry").ClickAsync(new MouseEventArgs());

		component.WaitForAssertion(() => component.FindAll(".pdchat-conversation-row").Should().ContainSingle(), DebounceTimeout);
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
