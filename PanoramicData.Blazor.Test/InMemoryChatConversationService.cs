using System;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The smallest honest <see cref="IChatConversationService"/>: an in-memory store that implements every required
/// operation and none of the optional ones.
/// </summary>
/// <remarks>
/// <para>
/// It holds conversations and their transcripts, honours cancellation and refuses ids it does not hold, so a
/// test that hands it to a component gets a store that behaves like one rather than a stub that agrees with
/// whatever it is asked.
/// </para>
/// <para>
/// It deliberately supplies nothing that carries a default implementation (such as
/// <see cref="IChatConversationService.SupportsSemanticSearch"/>), so it also stands for a host that stores
/// conversations and nothing more. If it ever has to grow to keep compiling, the contract has acquired a
/// requirement it should not have.
/// </para>
/// </remarks>
public sealed class InMemoryChatConversationService : IChatConversationService
{
	private readonly List<ChatConversation> _conversations = [];
	private readonly Dictionary<Guid, List<ChatMessage>> _transcripts = [];

	/// <summary>Gets the conversations held, oldest first.</summary>
	public IReadOnlyList<ChatConversation> Conversations => _conversations;

	/// <inheritdoc />
	public Task<ChatConversationPage> ListAsync(ChatConversationQuery query, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var matching = _conversations
			.Where(conversation => query.IncludeArchived || !conversation.IsArchived)
			.Where(conversation => !query.HasSearchText || conversation.DisplayName.Contains(query.SearchText!, StringComparison.OrdinalIgnoreCase))
			.ToList();

		return Task.FromResult(new ChatConversationPage
		{
			Conversations = [.. matching.Skip(query.Skip).Take(query.Take)],
			HasMore = matching.Count > query.Skip + query.Take,
			TotalCount = matching.Count
		});
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid id, CancellationToken cancellationToken)
	{
		Find(id, cancellationToken);
		return Task.FromResult<IReadOnlyList<ChatMessage>>([.. _transcripts[id]]);
	}

	/// <inheritdoc />
	public Task<ChatConversation> CreateAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var conversation = new ChatConversation { Id = Guid.NewGuid() };
		_conversations.Add(conversation);
		_transcripts[conversation.Id] = [];
		return Task.FromResult(conversation);
	}

	/// <inheritdoc />
	public Task RenameAsync(Guid id, string title, CancellationToken cancellationToken)
	{
		Find(id, cancellationToken).Title = title;
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
	{
		Find(id, cancellationToken).IsArchived = true;
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken)
	{
		Find(id, cancellationToken).IsArchived = false;
		return Task.CompletedTask;
	}

	private ChatConversation Find(Guid id, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return _conversations.Find(conversation => conversation.Id == id)
			?? throw new InvalidOperationException($"This store does not hold conversation {id}.");
	}
}
