using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Click, mouse, operation and keyboard shortcut tests for <see cref="PDButton"/>.
/// </summary>
public partial class PDButtonTests
{
	/// <summary>Clicking raises Click with the mouse arguments.</summary>
	[Fact]
	public void Click_raises_the_click_callback()
	{
		MouseEventArgs? received = null;
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.Click, (MouseEventArgs args) => received = args));

		component.Find("button").Click(new MouseEventArgs { Button = 2 });

		received.Should().NotBeNull();
		received!.Button.Should().Be(2);
	}

	/// <summary>Mouse down and mouse enter raise their callbacks.</summary>
	[Fact]
	public void Mouse_down_and_enter_raise_their_callbacks()
	{
		var downs = 0;
		var enters = 0;
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.MouseDown, (MouseEventArgs _) => downs++)
			.Add(p => p.MouseEnter, (MouseEventArgs _) => enters++));

		component.Find("button").MouseDown();
		component.Find("button").MouseEnter();

		downs.Should().Be(1);
		enters.Should().Be(1);
	}

	/// <summary>
	/// While an operation runs the button is disabled and shows the operation icon; when it completes the
	/// button is enabled again and Click is not raised.
	/// </summary>
	[Fact]
	public async Task Operation_disables_the_button_while_it_runs()
	{
		var gate = new TaskCompletionSource();
		var clicks = 0;
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.IconCssClass, "fas fa-play")
			.Add(p => p.OperationIconCssClass, "fas fa-spinner")
			.Add(p => p.Click, () => clicks++)
			.Add(p => p.Operation, _ => gate.Task));

		var click = component.Find("button").ClickAsync(new MouseEventArgs());

		component.WaitForAssertion(() => component.Find("button").HasAttribute("disabled").Should().BeTrue());
		component.Find("span.icon").ClassList.Should().Contain("fa-spinner");

		gate.SetResult();
		await click;

		component.WaitForAssertion(() => component.Find("button").HasAttribute("disabled").Should().BeFalse());
		component.Find("span.icon").ClassList.Should().Contain("fa-play");
		clicks.Should().Be(0);
	}

	/// <summary>Without an operation icon the normal icon stays in place while an operation runs.</summary>
	[Fact]
	public void Operation_without_operation_icon_keeps_the_icon()
	{
		var gate = new TaskCompletionSource();
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.IconCssClass, "fas fa-play")
			.Add(p => p.Operation, _ => gate.Task));

		_ = component.Find("button").ClickAsync(new MouseEventArgs());

		component.WaitForAssertion(() => component.Find("button").HasAttribute("disabled").Should().BeTrue());
		component.Find("span.icon").ClassList.Should().Contain("fa-play");
		gate.SetResult();
	}

	/// <summary>A shortcut key is registered while the button lives and unregistered when disposed.</summary>
	[Fact]
	public void Shortcut_is_registered_and_unregistered()
	{
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.ShortcutKey, ShortcutKey.Create("alt-KeyQ")));

		GlobalEvents.GetRegisteredShortcuts().Select(s => s.ToString()).Should().Contain("Alt-Q");

		component.Instance.Dispose();

		GlobalEvents.GetRegisteredShortcuts().Should().BeEmpty();
	}

	/// <summary>A matching key up raises Click; a non-matching one does not.</summary>
	[Fact]
	public void Matching_key_up_raises_click()
	{
		var clicks = 0;
		var component = Render<PDButton>(parameters => parameters
			.Add(p => p.ShortcutKey, ShortcutKey.Create("ctrl-KeyS"))
			.Add(p => p.Click, () => clicks++));

		GlobalEvents.KeyUp(new KeyboardInfo { Key = "x", Code = "KeyX", CtrlKey = true });
		clicks.Should().Be(0);

		GlobalEvents.KeyUp(new KeyboardInfo { Key = "s", Code = "KeyS", CtrlKey = true });
		component.WaitForAssertion(() => clicks.Should().Be(1));
	}

	/// <summary>Without a shortcut key, key presses never raise Click and nothing is registered.</summary>
	[Fact]
	public void No_shortcut_ignores_key_up()
	{
		var clicks = 0;
		var component = Render<PDButton>(parameters => parameters.Add(p => p.Click, () => clicks++));

		GlobalEvents.GetRegisteredShortcuts().Should().BeEmpty();
		GlobalEvents.KeyUp(new KeyboardInfo { Key = "s", Code = "KeyS" });

		clicks.Should().Be(0);
		component.Instance.Dispose();
	}
}
