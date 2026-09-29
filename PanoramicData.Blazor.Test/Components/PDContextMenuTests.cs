using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDContextMenu"/> renders its items, shows the menu on a right mouse button
/// press (or release), lets the application update or cancel it first, and raises item clicks.
/// </summary>
public class PDContextMenuTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDContextMenu.razor.js";

	private readonly BunitJSModuleInterop _module;
	private readonly BunitJSModuleInterop _commonModule;

	/// <summary>Sets up the rendering context with popper.js reported as present.</summary>
	public PDContextMenuTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
		_module.Setup<bool>("hasPopperJs").SetResult(true);
		_commonModule = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
	}

	/// <summary>
	/// Verifies that visible items, separators, icons, disabled state and custom markup are rendered,
	/// and that hidden items are not.
	/// </summary>
	[Fact]
	public void Renders_items_separators_and_custom_content()
	{
		var component = RenderMenu(null);

		var items = component.FindAll(".pdcontextmenuitem");
		items.Should().HaveCount(3);
		items[0].QuerySelector("i")!.ClassList.Should().Contain(["fas", "fa-cut", "me-1"]);
		items[0].QuerySelector("span")!.TextContent.Should().Be("Cut");
		items[0].ClassList.Should().NotContain("disabled");
		items[1].ClassList.Should().Contain("disabled");
		items[1].QuerySelector("i").Should().BeNull();
		items[2].QuerySelector("b")!.TextContent.Should().Be("Bold");
		component.FindAll(".pdcontextmenuseparator").Should().ContainSingle();
		component.Markup.Should().NotContain("Hidden");
	}

	/// <summary>
	/// Verifies that the wrapped content is rendered inside the host element.
	/// </summary>
	[Fact]
	public void Renders_the_wrapped_content_in_the_host()
	{
		var component = RenderMenu(null);

		component.Find(".pdcontextmenuhost .target").TextContent.Should().Be("Right click me");
	}

	/// <summary>
	/// Verifies that a right button press raises UpdateState with the items and the element under the
	/// pointer, then shows the menu at the pointer.
	/// </summary>
	[Fact]
	public async Task A_right_press_updates_state_then_shows_the_menu_at_the_pointer()
	{
		var element = new ElementInfo { Tag = "DIV", Id = "clicked" };
		_commonModule.Setup<ElementInfo>("getElementAtPoint", 15d, 25d).SetResult(element);
		MenuItemsEventArgs? raised = null;
		var component = RenderMenu(null, updateState: args => raised = args);

		await component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseDownAsync(new MouseEventArgs { Button = 2, ClientX = 15, ClientY = 25 }));

		raised.Should().NotBeNull();
		raised!.MenuItems.Should().HaveCount(5);
		raised.Sender.Should().BeSameAs(component.Instance);
		raised.SourceElement.Should().BeSameAs(element);
		var show = _module.VerifyInvoke("showMenu");
		show.Arguments.Should().Equal(component.Instance.Id, 15d, 25d);
		component.Instance.Id.Should().MatchRegex("^pdcm[0-9]+$");
	}

	/// <summary>
	/// Verifies that the left button, a release by default, and a disabled menu all leave the menu hidden.
	/// </summary>
	[Fact]
	public async Task Other_buttons_releases_and_a_disabled_menu_do_not_show_it()
	{
		var component = RenderMenu(null);

		await component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseDownAsync(new MouseEventArgs { Button = 0 }));
		await component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseUpAsync(new MouseEventArgs { Button = 2 }));
		component.Render(parameters => parameters.Add(p => p.Enabled, false));
		await component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseDownAsync(new MouseEventArgs { Button = 2 }));

		_module.Invocations["showMenu"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that with ShowOnMouseUp the menu is shown on release rather than on press.
	/// </summary>
	[Fact]
	public async Task ShowOnMouseUp_shows_the_menu_on_release_only()
	{
		var component = RenderMenu(null, showOnMouseUp: true);

		await component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseDownAsync(new MouseEventArgs { Button = 2 }));
		_module.Invocations["showMenu"].Should().BeEmpty();

		await component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseUpAsync(new MouseEventArgs { Button = 2, ClientX = 1, ClientY = 2 }));
		_module.VerifyInvoke("showMenu").Arguments.Should().Equal(component.Instance.Id, 1d, 2d);
	}

	/// <summary>
	/// Verifies that cancelling in UpdateState prevents the menu being shown.
	/// </summary>
	[Fact]
	public async Task Cancelling_in_UpdateState_prevents_the_menu()
	{
		var component = RenderMenu(null, updateState: args => args.Cancel = true);

		await component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseDownAsync(new MouseEventArgs { Button = 2 }));

		_module.Invocations["showMenu"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that clicking an enabled item hides the menu and raises ItemClick with that item.
	/// </summary>
	[Fact]
	public async Task Clicking_an_enabled_item_hides_the_menu_and_raises_ItemClick()
	{
		MenuItem? clicked = null;
		var component = RenderMenu(item => clicked = item);

		await component.InvokeAsync(() => component.FindAll(".pdcontextmenuitem")[0].ClickAsync(new MouseEventArgs()));

		clicked.Should().NotBeNull();
		clicked!.Key.Should().Be("cut");
		_module.VerifyInvoke("hideMenu").Arguments.Should().Equal(component.Instance.Id);
	}

	/// <summary>
	/// Verifies that clicking a disabled item does nothing.
	/// </summary>
	[Fact]
	public async Task Clicking_a_disabled_item_does_nothing()
	{
		MenuItem? clicked = null;
		var component = RenderMenu(item => clicked = item);

		await component.InvokeAsync(() => component.FindAll(".pdcontextmenuitem")[1].ClickAsync(new MouseEventArgs()));

		clicked.Should().BeNull();
		_module.Invocations["hideMenu"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that disposing hides the menu and releases the module.
	/// </summary>
	[Fact]
	public async Task Disposing_hides_the_menu()
	{
		var component = RenderMenu(null);

		await component.Instance.DisposeAsync();

		_module.VerifyInvoke("hideMenu").Arguments.Should().Equal(component.Instance.Id);
	}

	/// <summary>
	/// Verifies that disposing does not throw when the browser side has already gone.
	/// </summary>
	[Fact]
	public async Task Disposing_after_the_browser_has_gone_does_not_throw()
	{
		var component = RenderMenu(null);
		var hide = _module.SetupVoid(i => i.Identifier == "hideMenu");
		hide.SetException(new JSDisconnectedException("gone"));

		var dispose = async () => await component.Instance.DisposeAsync();

		await dispose.Should().NotThrowAsync();
		hide.Invocations.Should().ContainSingle("the failing hide must actually have been attempted");
	}

	private static List<MenuItem> CreateItems() =>
	[
		new MenuItem("cut", "Cut", "fas fa-cut"),
		new MenuItem("copy", "Copy", string.Empty, enabled: false),
		new MenuItem { IsSeparator = true },
		new MenuItem { Key = "bold", Content = "<b>Bold</b>" },
		new MenuItem("hidden", "Hidden", string.Empty, visible: false)
	];

	private IRenderedComponent<PDContextMenu> RenderMenu(
		Action<MenuItem>? itemClick,
		Action<MenuItemsEventArgs>? updateState = null,
		bool showOnMouseUp = false)
		=> Render<PDContextMenu>(parameters => parameters
			.Add(p => p.Items, CreateItems())
			.Add(p => p.ShowOnMouseUp, showOnMouseUp)
			.Add(p => p.ItemClick, item => itemClick?.Invoke(item))
			.Add(p => p.UpdateState, args => updateState?.Invoke(args))
			.AddChildContent("<span class=\"target\">Right click me</span>"));
}
