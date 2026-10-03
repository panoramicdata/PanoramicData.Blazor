using System;
using System.Reflection;
using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for <see cref="IChatConversationService"/> and the query and page types it is expressed in
/// (issue #108, MS-25786).
/// </summary>
/// <remarks>
/// Most of what matters about this contract is what it does <i>not</i> allow. The no-delete guarantee is the
/// commercial point of the feature - a transcript is a record of what our product told a customer - and it is
/// enforced by the absence of a method rather than by a UI that hides a button, because a UI-only guarantee
/// lasts until the next person adds a convenience call. The reflection test below is what stops that person,
/// so it asserts over the whole surface rather than over a list of members somebody would have to remember
/// to extend.
/// </remarks>
public class ChatConversationServiceContractTests
{
	/// <summary>
	/// Verifies that a query which says nothing about archived conversations excludes them.
	/// </summary>
	/// <remarks>
	/// The default is the load-bearing half of the property. A caller who has not thought about archiving is
	/// asking for the conversations a user would expect to see, and archived means hidden from the default
	/// list. Defaulting the other way would make archiving do nothing until every call site opted out.
	/// </remarks>
	[Fact]
	public void A_query_excludes_archived_conversations_unless_asked()
	{
		var query = new ChatConversationQuery();

		query.IncludeArchived.Should().BeFalse();
	}

	/// <summary>Verifies that search defaults to keyword, which every implementer can support.</summary>
	/// <remarks>
	/// Semantic search needs an embedding model. Defaulting to it would mean a host that has not got one
	/// fails on a query it never asked to be semantic.
	/// </remarks>
	[Fact]
	public void A_query_searches_by_keyword_unless_asked_otherwise()
	{
		var query = new ChatConversationQuery();

		query.SearchMode.Should().Be(ChatConversationSearchMode.Keyword);
	}

	/// <summary>Verifies that a query asks for a bounded page rather than everything.</summary>
	/// <remarks>
	/// A zero default would be read by a careful implementer as "take none" and by a careless one as
	/// "take all"; the first renders an empty sidebar and the second fetches every transcript a user has
	/// ever had. Neither is what a caller who left it alone meant.
	/// </remarks>
	[Fact]
	public void A_query_asks_for_a_bounded_page_by_default()
	{
		var query = new ChatConversationQuery();

		query.Take.Should().Be(ChatConversationQuery.DefaultTake);
		query.Take.Should().BeGreaterThan(0);
		query.Skip.Should().Be(0);
	}

	/// <summary>Verifies that whitespace is not a search.</summary>
	/// <remarks>
	/// A debounced search box hands over whatever is in it, which after a backspace is often a single space.
	/// Treating that as a search term costs an embedding call and returns nothing.
	/// </remarks>
	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("   ", false)]
	[InlineData("network", true)]
	public void Only_meaningful_text_counts_as_a_search(string? searchText, bool expected)
	{
		var query = new ChatConversationQuery { SearchText = searchText };

		query.HasSearchText.Should().Be(expected);
	}

	/// <summary>Verifies that a search returning nothing is representable as a page, not an error.</summary>
	[Fact]
	public void A_result_set_with_nothing_in_it_is_an_ordinary_page()
	{
		var page = ChatConversationPage.Empty;

		page.Conversations.Should().BeEmpty();
		page.HasMore.Should().BeFalse();
		page.TotalCount.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a store which cannot count cheaply can say so, rather than being made to claim zero.
	/// </summary>
	/// <remarks>
	/// <c>null</c> means "not counted" and is deliberately distinct from <c>0</c>, which means "counted, and
	/// there are none". Collapsing the two would let an implementer that skipped the count render as an empty
	/// history, which is exactly the reassurance a user of a no-delete feature must never be given falsely.
	/// </remarks>
	[Fact]
	public void An_uncounted_result_set_is_distinguishable_from_an_empty_one()
	{
		var uncounted = new ChatConversationPage
		{
			Conversations = [new ChatConversation { Id = Guid.NewGuid() }],
			TotalCount = null
		};

		uncounted.TotalCount.Should().BeNull();
		uncounted.Conversations.Should().ContainSingle();
	}

	/// <summary>Verifies that a page defaults to holding nothing rather than throwing on an unset list.</summary>
	[Fact]
	public void A_page_holds_an_empty_list_rather_than_null()
	{
		var page = new ChatConversationPage();

		page.Conversations.Should().NotBeNull();
		page.Conversations.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that nothing on the contract, or on any type it is expressed in, can remove a conversation.
	/// </summary>
	/// <remarks>
	/// This is the FAIL criterion on MS-25786 turned into something that runs. It asserts by reflection over
	/// the whole feature surface so that adding a <c>DeleteAsync</c>, a <c>Remove</c>, or a <c>Purge</c> to any
	/// of these types breaks the build rather than passing review on the grounds that the UI does not call it.
	/// </remarks>
	[Fact]
	public void No_member_anywhere_in_the_contract_can_remove_a_conversation()
	{
		string[] removalVerbs = ["delete", "remove", "purge", "destroy", "drop", "erase", "clear"];

		Type[] featureTypes =
		[
			typeof(IChatConversationService),
			typeof(ChatConversationQuery),
			typeof(ChatConversationPage),
			typeof(ChatConversation)
		];

		var offendingMembers = featureTypes
			.SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
			.Where(member => removalVerbs.Any(verb => member.Name.Contains(verb, StringComparison.OrdinalIgnoreCase)))
			.Select(member => $"{member.DeclaringType!.Name}.{member.Name}")
			.ToList();

		offendingMembers.Should().BeEmpty(
			"a conversation is archived, never removed - the guarantee is the absence of the method, not a hidden button");
	}

	/// <summary>
	/// Verifies that the contract offers archiving in both directions, so that archiving is reversible.
	/// </summary>
	/// <remarks>
	/// Archive without un-archive would be delete wearing a different name: a user who archived the wrong
	/// conversation could still read it but never get it back into their list.
	/// </remarks>
	[Fact]
	public void Archiving_a_conversation_can_be_undone()
	{
		var memberNames = typeof(IChatConversationService)
			.GetMembers()
			.Select(member => member.Name)
			.ToList();

		memberNames.Should().Contain(nameof(IChatConversationService.ArchiveAsync));
		memberNames.Should().Contain(nameof(IChatConversationService.UnarchiveAsync));
	}

	/// <summary>Verifies that every operation on the contract can be cancelled.</summary>
	/// <remarks>
	/// The sidebar's search is debounced and superseded as the user keeps typing, so an in-flight list has to
	/// be abandonable. A semantic search is a network call plus an embedding; one per keystroke that nobody
	/// cancels is work queued behind a result the user has already moved past.
	///
	/// Property accessors are excluded: <see cref="IChatConversationService.SupportsSemanticSearch"/> is a
	/// capability flag read synchronously off the implementation, not an operation there is anything to
	/// cancel.
	/// </remarks>
	[Fact]
	public void Every_operation_can_be_cancelled()
	{
		var methodsWithoutCancellation = typeof(IChatConversationService)
			.GetMethods()
			.Where(method => !method.IsSpecialName)
			.Where(method => !method.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken)))
			.Select(method => method.Name)
			.ToList();

		methodsWithoutCancellation.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that an implementation is not assumed to have an embedding model.
	/// </summary>
	/// <remarks>
	/// Defaulting the capability to true would mean the sidebar offers semantic search against every store
	/// that has not said otherwise, and the user discovers the truth as a failed search rather than as an
	/// absent option.
	/// </remarks>
	[Fact]
	public void Semantic_search_is_absent_unless_an_implementation_claims_it()
	{
		IChatConversationService service = new InMemoryChatConversationService();

		service.SupportsSemanticSearch.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that the required operations alone are enough to keep a history: create, rename, archive out of
	/// the default list and back again, with nothing deleted along the way.
	/// </summary>
	[Fact]
	public async Task The_required_operations_alone_keep_a_complete_history()
	{
		IChatConversationService service = new InMemoryChatConversationService();
		var cancellationToken = TestContext.Current.CancellationToken;

		var conversation = await service.CreateAsync(cancellationToken);
		await service.RenameAsync(conversation.Id, "Ice cream", cancellationToken);
		await service.ArchiveAsync(conversation.Id, cancellationToken);
		var hidden = await service.ListAsync(new ChatConversationQuery(), cancellationToken);
		var archived = await service.ListAsync(new ChatConversationQuery { IncludeArchived = true, SearchText = "ice" }, cancellationToken);
		await service.UnarchiveAsync(conversation.Id, cancellationToken);
		var restored = await service.ListAsync(new ChatConversationQuery(), cancellationToken);

		hidden.Conversations.Should().BeEmpty();
		archived.Conversations.Should().ContainSingle().Which.Title.Should().Be("Ice cream");
		restored.Conversations.Should().ContainSingle().Which.IsArchived.Should().BeFalse();
		(await service.GetMessagesAsync(conversation.Id, cancellationToken)).Should().BeEmpty();
	}

	/// <summary>Verifies that a store refuses a conversation it does not hold rather than inventing one.</summary>
	[Fact]
	public async Task A_store_refuses_a_conversation_it_does_not_hold()
	{
		IChatConversationService service = new InMemoryChatConversationService();

		var act = () => service.GetMessagesAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	/// <summary>Verifies that a cancelled request is not carried out.</summary>
	[Fact]
	public async Task A_cancelled_request_is_not_carried_out()
	{
		var service = new InMemoryChatConversationService();

		var act = () => service.CreateAsync(new CancellationToken(canceled: true));

		await act.Should().ThrowAsync<OperationCanceledException>();
		service.Conversations.Should().BeEmpty();
	}
}
