using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The conversation store double used by the <see cref="PDChat"/> conversation tests.
/// </summary>
public partial class PDChatTests
{
	/// <summary>A conversation store holding two conversations and recording what was asked of it.</summary>
	private sealed class FakeConversationStore(bool failTranscripts = false) : IChatConversationService
	{
		public ChatConversation First { get; } = new() { Id = Guid.NewGuid(), Title = "First" };
		public ChatConversation Second { get; } = new() { Id = Guid.NewGuid(), Title = "Second" };
		public List<Guid> MessageRequests { get; } = [];
		public List<ChatConversation> Created { get; } = [];
		public List<(Guid Id, string Title)> Renamed { get; } = [];
		public List<Guid> Archived { get; } = [];

		public Task<ChatConversationPage> ListAsync(ChatConversationQuery query, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ChatConversation[] all = [First, Second, .. Created];
			var visible = all.Where(c => query.IncludeArchived || !c.IsArchived).ToList();
			return Task.FromResult(new ChatConversationPage { Conversations = visible, TotalCount = visible.Count });
		}

		public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			MessageRequests.Add(id);
			if (failTranscripts)
			{
				throw new InvalidOperationException("Store offline");
			}

			var title = id == First.Id ? "First" : id == Second.Id ? "Second" : "New";
			return Task.FromResult<IReadOnlyList<ChatMessage>>([Message($"{title} message")]);
		}

		public Task<ChatConversation> CreateAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var conversation = new ChatConversation { Id = Guid.NewGuid() };
			Created.Add(conversation);
			return Task.FromResult(conversation);
		}

		public Task RenameAsync(Guid id, string title, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Renamed.Add((id, title));
			return Task.CompletedTask;
		}

		public Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Archived.Add(id);
			SetArchived(id, true);
			return Task.CompletedTask;
		}

		public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			SetArchived(id, false);
			return Task.CompletedTask;
		}

		private void SetArchived(Guid id, bool isArchived)
		{
			foreach (var conversation in new[] { First, Second }.Concat(Created).Where(c => c.Id == id))
			{
				conversation.IsArchived = isArchived;
			}
		}
	}
}
