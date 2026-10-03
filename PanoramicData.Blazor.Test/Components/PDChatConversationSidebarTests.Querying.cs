using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Search, filter and paging tests for <see cref="PDChatConversationSidebar"/>.
/// </summary>
public partial class PDChatConversationSidebarTests
{
	/// <summary>Typing searches after the debounce and passes the text; no match says so.</summary>
	/// <remarks>
	/// The input handler awaits the debounce and then the search, so awaiting the event is awaiting the
	/// search itself: nothing here depends on how long the debounce timer takes to fire. The explicit wait
	/// is a backstop for the render; it once failed on a loaded CI runner at bUnit's default of one second.
	/// </remarks>
	[Fact]
	public async Task Typing_searches_after_a_debounce()
	{
		Add("Alpha", TimeSpan.Zero);
		var component = RenderSidebar();

		await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "zzz" });

		component.WaitForAssertion(() => component.Find(".pdchat-conversation-empty").TextContent.Trim()
			.Should().Be("No conversations match your search."), DebounceTimeout);
		_service.Queries.Last().SearchText.Should().Be("zzz");
	}

	/// <summary>A keystroke that supersedes another within the debounce means only the last text is searched.</summary>
	/// <remarks>
	/// Both keystrokes are dispatched in one turn of the renderer's dispatcher, so the first debounce's
	/// continuation, which needs that dispatcher, cannot run until the second keystroke has cancelled it.
	/// Whether or not the first timer has elapsed by then, its search is abandoned (issue #218), so exactly
	/// two searches reach the store; the elapsed case is pinned by the manual-clock test in the Debounce file.
	/// </remarks>
	[Fact]
	public async Task A_superseded_keystroke_is_not_searched()
	{
		Add("Alpha", TimeSpan.Zero);
		var component = RenderSidebar();

		await component.InvokeAsync(() =>
		{
			var first = component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "Al" });
			var second = component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "Alp" });
			return Task.WhenAll(first, second);
		});

		component.WaitForAssertion(() => component.FindAll(".pdchat-conversation-row").Should().ContainSingle(), DebounceTimeout);
		_service.Queries.Select(q => q.SearchText).Should().Equal(string.Empty, "Alp");
	}

	/// <summary>An input event with no value searches for empty text.</summary>
	[Fact]
	public async Task A_null_input_value_searches_for_everything()
	{
		Add("Alpha", TimeSpan.Zero);
		var component = RenderSidebar();

		await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = null });

		component.WaitForAssertion(() => _service.Queries.Should().HaveCount(2), DebounceTimeout);
		_service.Queries.Last().SearchText.Should().BeEmpty();
	}

	/// <summary>The search mode buttons are hidden unless the store supports semantic search.</summary>
	[Fact]
	public void Search_modes_are_hidden_without_semantic_support()
	{
		var component = RenderSidebar();

		component.FindAll(".pdchat-conversation-search-mode").Should().BeEmpty();
	}

	/// <summary>Ticking include archived re-queries with archived conversations included.</summary>
	[Fact]
	public async Task Include_archived_requeries()
	{
		var component = RenderSidebar();

		await component.Find(".pdchat-conversation-include-archived input").ChangeAsync(new ChangeEventArgs { Value = true });
		_service.Queries.Last().IncludeArchived.Should().BeTrue();

		await component.Find(".pdchat-conversation-include-archived input").ChangeAsync(new ChangeEventArgs { Value = false });
		_service.Queries.Last().IncludeArchived.Should().BeFalse();
	}

	/// <summary>Show more loads the next page after the rows already listed.</summary>
	[Fact]
	public async Task Show_more_loads_the_next_page()
	{
		for (var i = 0; i < 3; i++)
		{
			Add($"C{i}", TimeSpan.FromMinutes(i));
		}

		_service.PageSize = 2;
		var component = RenderSidebar();
		component.FindAll(".pdchat-conversation-row").Should().HaveCount(2);

		await component.Find(".pdchat-conversation-more").ClickAsync(new MouseEventArgs());

		component.WaitForAssertion(() => component.FindAll(".pdchat-conversation-row").Should().HaveCount(3), DebounceTimeout);
		_service.Queries.Last().Skip.Should().Be(2);
		component.FindAll(".pdchat-conversation-more").Should().BeEmpty();
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
}
