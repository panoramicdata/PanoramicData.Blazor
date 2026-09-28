using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDMessages"/> lists its messages, gates its input on being live and permitted,
/// sends the typed text through its callbacks (by button or by Enter from JavaScript), and wires up and
/// tears down its JavaScript helpers.
/// </summary>
public class PDMessagesTests : BunitContext
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
	/// Verifies that the input is disabled while the stream is not live, and the send button while there
	/// is nothing to send.
	/// </summary>
	[Fact]
	public void Input_is_disabled_when_not_live_and_send_when_empty()
	{
		var component = Render<PDMessages>(parameters => parameters.Add(p => p.IsLive, false));
		component.Find("textarea").HasAttribute("disabled").Should().BeTrue();
		component.Find("button").HasAttribute("disabled").Should().BeTrue();

		component.Render(parameters => parameters.Add(p => p.IsLive, true));
		component.Find("textarea").HasAttribute("disabled").Should().BeFalse();
		component.Find("button").HasAttribute("disabled").Should().BeTrue();

		component.Find("textarea").Input("hello");
		component.WaitForAssertion(() => component.Find("button").HasAttribute("disabled").Should().BeFalse());
	}

	/// <summary>
	/// Verifies that whitespace-only input cannot be sent.
	/// </summary>
	[Fact]
	public void Whitespace_input_cannot_be_sent()
	{
		var component = Render<PDMessages>(parameters => parameters.Add(p => p.IsLive, true));

		component.Find("textarea").Input("   ");

		component.Find("button").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>
	/// Verifies that when input is not permitted the input is replaced by the disabled message.
	/// </summary>
	[Fact]
	public void When_input_is_not_permitted_the_disabled_message_is_shown()
	{
		var component = Render<PDMessages>(parameters => parameters
			.Add(p => p.IsInputPermitted, false)
			.Add(p => p.InputDisabledMessage, "Read only"));

		component.FindAll("textarea").Should().BeEmpty();
		var note = component.Find(".chat-input-disabled");
		note.TextContent.Should().Be("Read only");
		note.GetAttribute("role").Should().Be("note");
	}

	/// <summary>
	/// Verifies that nothing is rendered in place of the input when no disabled message is given.
	/// </summary>
	[Fact]
	public void When_input_is_not_permitted_and_there_is_no_message_nothing_is_shown()
	{
		var component = Render<PDMessages>(parameters => parameters
			.Add(p => p.IsInputPermitted, false)
			.Add(p => p.InputDisabledMessage, " "));

		component.FindAll("textarea").Should().BeEmpty();
		component.FindAll(".chat-input-disabled").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that clicking Send pushes the typed text to the parent before raising OnSendClicked.
	/// </summary>
	[Fact]
	public void Clicking_send_pushes_the_input_then_raises_send()
	{
		var events = new List<string>();
		var component = RenderLive(events);

		component.Find("textarea").Input("hello there");
		component.Find("button").Click();

		events.Should().Equal("input:hello there", "send");
	}

	/// <summary>
	/// Verifies that Enter, reported from JavaScript, sends in the same way as the button.
	/// </summary>
	[Fact]
	public async Task Enter_from_javascript_sends_the_input()
	{
		var events = new List<string>();
		var component = RenderLive(events);
		component.Find("textarea").Input("by keyboard");

		await component.InvokeAsync(component.Instance.OnEnterPressed);

		events.Should().Equal("input:by keyboard", "send");
	}

	/// <summary>
	/// Verifies that Enter with nothing typed sends nothing.
	/// </summary>
	[Fact]
	public async Task Enter_with_no_input_sends_nothing()
	{
		var events = new List<string>();
		var component = RenderLive(events);

		await component.InvokeAsync(component.Instance.OnEnterPressed);

		events.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that without an OnSendClicked handler nothing is pushed to the parent.
	/// </summary>
	[Fact]
	public async Task Without_a_send_handler_nothing_is_pushed()
	{
		var inputs = new List<string>();
		var component = Render<PDMessages>(parameters => parameters
			.Add(p => p.IsLive, true)
			.Add(p => p.CurrentInputChanged, v => inputs.Add(v)));
		component.Find("textarea").Input("hello");

		await component.InvokeAsync(component.Instance.OnEnterPressed);

		inputs.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that ClearInput empties the text area, so there is nothing left to send.
	/// </summary>
	[Fact]
	public async Task ClearInput_empties_the_text_area()
	{
		var events = new List<string>();
		var component = RenderLive(events);
		component.Find("textarea").Input("typed");

		await component.InvokeAsync(component.Instance.ClearInput);

		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		component.Find("button").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the Enter handler is attached to the text area, and re-attached after the text area
	/// is recreated by ClearInput.
	/// </summary>
	/// <remarks>
	/// The handler is attached after a short scroll delay with no render following it, so the test waits
	/// on the interop call itself rather than on a render.
	/// </remarks>
	[Fact]
	public async Task The_enter_handler_is_attached_and_reattached_after_clearing()
	{
		var attached = SignalOn("attachEnterHandler");
		var component = RenderLive([]);
		await attached.WaitForCountAsync(1);
		_module.Invocations["attachEnterHandler"].Should().ContainSingle()
			.Which.Arguments[0].Should().BeOfType<ElementReference>();

		await component.InvokeAsync(component.Instance.ClearInput);

		await attached.WaitForCountAsync(2);
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

		component.Find("textarea").Input("still works");
		component.Find("button").Click();
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

				await called.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
				Interlocked.CompareExchange(ref _called, new(TaskCreationOptions.RunContinuationsAsynchronously), called);
			}
		}
	}
}