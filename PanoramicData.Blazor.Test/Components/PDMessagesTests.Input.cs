using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Input gating, sending and clearing tests for <see cref="PDMessages"/>.
/// </summary>
public partial class PDMessagesTests
{
	/// <summary>
	/// Verifies that the input is disabled while the stream is not live, and the send button while there
	/// is nothing to send.
	/// </summary>
	[Fact]
	public async Task Input_is_disabled_when_not_live_and_send_when_empty()
	{
		var component = Render<PDMessages>(parameters => parameters.Add(p => p.IsLive, false));
		component.Find("textarea").HasAttribute("disabled").Should().BeTrue();
		component.Find("button").HasAttribute("disabled").Should().BeTrue();

		component.Render(parameters => parameters.Add(p => p.IsLive, true));
		component.Find("textarea").HasAttribute("disabled").Should().BeFalse();
		component.Find("button").HasAttribute("disabled").Should().BeTrue();

		await component.InvokeAsync(() => component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "hello" }));
		component.WaitForAssertion(() => component.Find("button").HasAttribute("disabled").Should().BeFalse(), TimeSpan.FromSeconds(10));
	}

	/// <summary>
	/// Verifies that whitespace-only input cannot be sent.
	/// </summary>
	[Fact]
	public async Task Whitespace_input_cannot_be_sent()
	{
		var component = Render<PDMessages>(parameters => parameters.Add(p => p.IsLive, true));

		await component.InvokeAsync(() => component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "   " }));

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
	public async Task Clicking_send_pushes_the_input_then_raises_send()
	{
		var events = new List<string>();
		var component = RenderLive(events);

		await component.InvokeAsync(() => component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "hello there" }));
		await component.InvokeAsync(() => component.Find("button").ClickAsync(new MouseEventArgs()));

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
		await component.InvokeAsync(() => component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "by keyboard" }));

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
		await component.InvokeAsync(() => component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "hello" }));

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
		await component.InvokeAsync(() => component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "typed" }));

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
}
