using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// <see cref="PDMessages"/> as an <see cref="IChatInput"/>: text appended from elsewhere, focus tracking, a virtual
/// Send, and host controls in the input row.
/// </summary>
public partial class PDMessagesTests
{
	private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

	/// <summary>Verifies that appended text joins what was typed with a space and is pushed to the parent.</summary>
	[Fact]
	public async Task Appended_text_follows_what_was_typed()
	{
		var events = new List<string>();
		var component = RenderLive(events);
		await component.InvokeAsync(() => component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "Is" }));

		await component.InvokeAsync(() => component.Instance.AppendAsync(" it done? "));
		await component.InvokeAsync(() => component.Instance.AppendAsync("  "));

		component.Instance.Text.Should().Be("Is it done?");
		component.Find("textarea").GetAttribute("value").Should().Be("Is it done?");
		events.Should().Equal("input:Is it done?");
	}

	/// <summary>Verifies that the text box reports focus and blur, and loses focus when cleared.</summary>
	[Fact]
	public async Task Focus_is_tracked_and_reported()
	{
		var reported = new List<bool>();
		var component = Render<PDMessages>(parameters => parameters
			.Add(p => p.IsLive, true)
			.Add(p => p.InputFocusChanged, isFocused => reported.Add(isFocused)));

		await component.Find("textarea").FocusAsync(new FocusEventArgs());
		component.Instance.IsFocused.Should().BeTrue();

		await component.Find("textarea").BlurAsync(new FocusEventArgs());
		component.Instance.IsFocused.Should().BeFalse();

		await component.Find("textarea").FocusAsync(new FocusEventArgs());
		await component.InvokeAsync(component.Instance.ClearInput);
		component.Instance.IsFocused.Should().BeFalse("the text box was replaced");

		reported.Should().Equal(true, false, true);
	}

	/// <summary>Verifies that a virtual Send behaves as the button does, and sends nothing when empty.</summary>
	[Fact]
	public async Task SendAsync_is_the_same_as_pressing_send()
	{
		var events = new List<string>();
		var component = RenderLive(events);

		await component.InvokeAsync(component.Instance.SendAsync);
		events.Should().BeEmpty();

		await component.InvokeAsync(() => component.Instance.AppendAsync("spoken"));
		await component.InvokeAsync(component.Instance.SendAsync);

		events.Should().Equal("input:spoken", "input:spoken", "send");
	}

	/// <summary>Verifies that host controls and Send share a toolbar above a full-width text box.</summary>
	[Fact]
	public void Input_accessories_sit_in_a_toolbar_above_the_input()
	{
		RenderLive([]).Find(".chat-input-accessories").Children.Select(child => child.ClassName).Should().Equal("btn btn-sm btn-secondary chat-send");

		var component = Render<PDMessages>(parameters => parameters
			.Add(p => p.IsLive, true)
			.Add(p => p.InputAccessories, builder => builder.AddMarkupContent(0, "<button class=\"extra\">Extra</button>")));

		var toolbar = component.Find(".chat-input-accessories");
		toolbar.GetAttribute("role").Should().Be("toolbar");
		toolbar.QuerySelector(".extra").Should().NotBeNull();
		toolbar.NextElementSibling!.ClassList.Should().Contain("chat-input-container");
		component.Find(".chat-input-container").Children.Select(child => child.LocalName).Should().Equal("textarea");
		toolbar.LastElementChild!.ClassList.Should().Contain("chat-send");
	}

	/// <summary>Verifies that the text box takes focus when shown, unless that is turned off.</summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task The_text_box_takes_focus_only_when_auto_focus_is_on(bool isAutoFocused)
	{
		var attached = SignalOn("attachEnterHandler");
		var component = Render<PDMessages>(parameters => parameters
			.Add(p => p.IsLive, true)
			.Add(p => p.IsInputAutoFocused, isAutoFocused));
		await attached.WaitForCountAsync(1);

		if (isAutoFocused)
		{
			component.WaitForAssertion(() => JSInterop.Invocations[FocusIdentifier].Should().ContainSingle(), TimeSpan.FromSeconds(10));
		}
		else
		{
			await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
			JSInterop.Invocations[FocusIdentifier].Should().BeEmpty();
		}
	}
}
