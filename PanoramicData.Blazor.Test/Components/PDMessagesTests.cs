using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDMessages"/> lists its messages, gates its input on being live and permitted,
/// sends the typed text through its callbacks (by button or by Enter from JavaScript), and wires up and
/// tears down its JavaScript helpers.
/// </summary>
public partial class PDMessagesTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDMessages.razor.js";

	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context and the messages module.</summary>
	public PDMessagesTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
	}

	/// <summary>
	/// Verifies that one message element is rendered per message.
	/// </summary>
	[Fact]
	public void Renders_one_message_element_per_message()
	{
		var component = Render<PDMessages>(parameters => parameters
			.Add(p => p.Messages, [CreateMessage("first"), CreateMessage("second")]));

		component.FindAll(".pdchat-messages .pdchat-message").Should().HaveCount(2);
		component.Find(".pdchat-messages").TextContent.Should().Contain("first").And.Contain("second");
	}

	/// <summary>
	/// Verifies that no messages render when the list is null.
	/// </summary>
	[Fact]
	public void Renders_no_messages_for_a_null_list()
	{
		var component = Render<PDMessages>();

		component.FindAll(".pdchat-message").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that new parameters scroll the message list element to the bottom.
	/// </summary>
	[Fact]
	public async Task New_parameters_scroll_the_list_to_the_bottom()
	{
		var scrolled = SignalOn("scrollToBottom");
		var component = RenderLive([]);
		await scrolled.WaitForCountAsync(1);
		var before = _module.Invocations["scrollToBottom"].Count;

		component.Render(parameters => parameters.Add(p => p.Messages, [CreateMessage("new")]));

		await scrolled.WaitForCountAsync(before + 1);
		component.Find(".pdchat-message").TextContent.Should().Contain("new");
		_module.Invocations["scrollToBottom"].Should().OnlyContain(i => i.Arguments.Single() is ElementReference);
	}

	/// <summary>
	/// Verifies that disposing detaches the Enter handler from the text area.
	/// </summary>
	[Fact]
	public async Task Disposing_detaches_the_enter_handler()
	{
		var component = RenderLive([]);

		await component.Instance.DisposeAsync();

		_module.VerifyInvoke("detachEnterHandler").Arguments.Should().ContainSingle()
			.Which.Should().BeOfType<ElementReference>();
	}

	/// <summary>
	/// Verifies that failing browser helpers neither break sending nor make disposal throw.
	/// </summary>
	[Fact]
	public async Task Failing_browser_helpers_do_not_break_the_component()
	{
		var failure = new JSException("element gone");
		_module.SetupVoid(i => i.Identifier == "scrollToBottom").SetException(failure);
		_module.SetupVoid(i => i.Identifier == "detachEnterHandler").SetException(failure);
		var attached = SignalOn("attachEnterHandler", failure);
		var events = new List<string>();
		var component = RenderLive(events);
		await attached.WaitForCountAsync(1);

		await component.InvokeAsync(() => component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "still works" }));
		await component.InvokeAsync(() => component.Find("button").ClickAsync(new MouseEventArgs()));
		var dispose = async () => await component.Instance.DisposeAsync();

		events.Should().Equal("input:still works", "send");
		await dispose.Should().NotThrowAsync();
	}

	private InvocationSignal SignalOn(string identifier, Exception? failure = null)
	{
		var signal = new InvocationSignal(() => _module.Invocations[identifier].Count);
		var handler = _module.SetupVoid(invocation => signal.Observe(invocation.Identifier == identifier));
		if (failure is null)
		{
			handler.SetVoidResult();
		}
		else
		{
			handler.SetException(failure);
		}

		return signal;
	}

	private IRenderedComponent<PDMessages> RenderLive(List<string> events)
		=> Render<PDMessages>(parameters => parameters
			.Add(p => p.IsLive, true)
			.Add(p => p.CurrentInputChanged, v => events.Add($"input:{v}"))
			.Add(p => p.OnSendClicked, () => events.Add("send")));

	private static ChatMessage CreateMessage(string text) => new()
	{
		Id = Guid.NewGuid(),
		Sender = new ChatMessageSender { Name = "Bot" },
		Message = text,
		Type = MessageType.Normal
	};

	/// <summary>
	/// Lets a test wait for an interop call that the component makes with no render following it.
	/// </summary>
	/// <param name="count">Reads how many matching calls have been made so far.</param>
	private sealed class InvocationSignal(Func<int> count)
	{
		private TaskCompletionSource _called = new(TaskCreationOptions.RunContinuationsAsynchronously);

		/// <summary>Records that an invocation was seen, and passes the match result through.</summary>
		/// <param name="matched">Whether the invocation is the one being waited for.</param>
		/// <returns><paramref name="matched"/>, so this can be used as an interop matcher.</returns>
		public bool Observe(bool matched)
		{
			if (matched)
			{
				_called.TrySetResult();
			}

			return matched;
		}

		/// <summary>Waits until at least the given number of matching calls have been made.</summary>
		/// <param name="expected">The number of calls to wait for.</param>
		/// <returns>A task that completes when the calls have been made.</returns>
		public async Task WaitForCountAsync(int expected)
		{
			while (count() < expected)
			{
				var called = _called;
				if (count() >= expected)
				{
					return;
				}

				await called.Task.WaitAsync(TimeSpan.FromSeconds(30), Xunit.TestContext.Current.CancellationToken);
				Interlocked.CompareExchange(ref _called, new(TaskCreationOptions.RunContinuationsAsynchronously), called);
			}
		}
	}
}
