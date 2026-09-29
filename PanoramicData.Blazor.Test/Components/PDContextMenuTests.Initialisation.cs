using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Exceptions;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that the menu element carries its id from the first render, so the first right-click opens it,
/// and that a missing popper.js is reported rather than swallowed (#186).
/// </summary>
public partial class PDContextMenuTests
{
	/// <summary>
	/// Verifies that the menu element carries the component's id from the first render, with no further render.
	/// </summary>
	[Fact]
	public void The_menu_element_carries_its_id_from_the_first_render()
	{
		var component = RenderMenu(null);

		component.Instance.Id.Should().MatchRegex("^pdcm[0-9]+$");
		component.Find(".pdcontextmenu").GetAttribute("id").Should().Be(component.Instance.Id);
	}

	/// <summary>
	/// Verifies that two menus get different ids.
	/// </summary>
	[Fact]
	public void Each_menu_gets_its_own_id()
	{
		var first = RenderMenu(null);
		var second = RenderMenu(null);

		second.Instance.Id.Should().NotBe(first.Instance.Id);
	}

	/// <summary>
	/// Verifies that when popper.js is missing the problem is logged as an error with a
	/// <see cref="PDContextMenuException"/>, and a right-click does not try to show the menu.
	/// </summary>
	[Fact]
	public async Task A_missing_popper_is_logged_and_the_menu_is_not_shown()
	{
		_module.Setup<bool>("hasPopperJs").SetResult(false);
		var logger = new ListLogger<PDContextMenu>();
		Services.AddSingleton<ILogger<PDContextMenu>>(logger);
		var component = RenderMenu(null);

		await component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseDownAsync(new MouseEventArgs { Button = 2 }));

		logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
			.Which.Exception.Should().BeOfType<PDContextMenuException>()
			.Which.Message.Should().Contain("popper.js");
		_module.Invocations["showMenu"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that when popper.js was missing at start-up but has been loaded since, the menu is shown.
	/// </summary>
	[Fact]
	public async Task A_popper_loaded_after_start_up_is_used()
	{
		var popper = _module.Setup<bool>("hasPopperJs");
		popper.SetResult(false);
		var component = RenderMenu(null);

		popper.SetResult(true);
		await component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseDownAsync(new MouseEventArgs { Button = 2, ClientX = 3, ClientY = 4 }));

		_module.VerifyInvoke("showMenu").Arguments.Should().Equal(component.Instance.Id, 3d, 4d);
	}
}
