using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDToolbarDropdown"/> renders its menu items, raises clicks for them and honours
/// their shortcut keys.
/// </summary>
public class PDToolbarDropdownTests : BunitContext
{
	private readonly List<string> _clicks = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDToolbarDropdownTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IGlobalEventService Events => Services.GetRequiredService<IGlobalEventService>();

	private IRenderedComponent<PDToolbarDropdown> RenderDropdown(List<MenuItem> items, Action<ComponentParameterCollectionBuilder<PDToolbarDropdown>>? configure = null)
		=> Render<PDToolbarDropdown>(parameters =>
		{
			parameters
				.Add(p => p.Items, items)
				.Add(p => p.Text, "Actions")
				.Add(p => p.Click, key => _clicks.Add(key));
			configure?.Invoke(parameters);
		});

	private static List<MenuItem> StandardItems() =>
	[
		new MenuItem { Key = "open", Text = "Open", IconCssClass = "fas fa-folder", ShortcutKey = ShortcutKey.Create("ctrl-o") },
		new MenuItem { IsSeparator = true },
		new MenuItem { Text = "Save &&As" },
		new MenuItem { Key = "hidden", Text = "Hidden", IsVisible = false },
		new MenuItem { Key = "off", Text = "Off", IsDisabled = true },
		new MenuItem { Key = "custom", Content = "<b class=\"custom\">Custom</b>" }
	];

	/// <summary>
	/// Verifies that visible items render as rows, separators as separator rows, hidden items not at all,
	/// and custom content as raw markup.
	/// </summary>
	[Fact]
	public void Items_RenderAsMenuRows()
	{
		var component = RenderDropdown(StandardItems());

		component.FindAll("tr.pddropdownmenuitem").Should().HaveCount(4);
		component.FindAll("tr.pddropdownseparator").Should().ContainSingle();
		component.Markup.Should().NotContain("Hidden");
		component.Find("tr.pddropdownmenuitem.disabled").TextContent.Should().Contain("Off");
		component.Find("b.custom").TextContent.Should().Be("Custom");
		component.Find("span.fas.fa-folder").Should().NotBeNull();
		component.Markup.Should().Contain("Ctrl-O");
	}

	/// <summary>
	/// Verifies that clicking an item raises its key, or its text without accelerator markers when it has no key.
	/// </summary>
	[Fact]
	public void ClickingAnItem_RaisesItsKeyOrText()
	{
		var component = RenderDropdown(StandardItems());
		var rows = component.FindAll("tr.pddropdownmenuitem");

		rows[0].Click();
		component.FindAll("tr.pddropdownmenuitem")[1].Click();

		_clicks.Should().Equal("open", "Save As");
	}

	/// <summary>
	/// Verifies that clicking a disabled item raises no click (#180).
	/// </summary>
	[Fact]
	public async Task ClickingADisabledItem_RaisesNothing()
	{
		var component = RenderDropdown(StandardItems());

		await component.Find("tr.pddropdownmenuitem.disabled").ClickAsync(new());

		_clicks.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the shortcut key of a disabled item raises no click (#180).
	/// </summary>
	[Fact]
	public void PressingTheShortcutOfADisabledItem_RaisesNothing()
	{
		var component = RenderDropdown(
		[
			new MenuItem { Key = "off", Text = "Off", IsDisabled = true, ShortcutKey = ShortcutKey.Create("ctrl-q") },
			new MenuItem { Key = "open", Text = "Open", ShortcutKey = ShortcutKey.Create("ctrl-o") }
		]);

		Events.KeyUp(new KeyboardInfo { Key = "q", Code = "KeyQ", CtrlKey = true });
		Events.KeyUp(new KeyboardInfo { Key = "o", Code = "KeyO", CtrlKey = true });

		// The shortcuts are dispatched in order, so a click from the disabled item would arrive first.
		component.WaitForAssertion(() => _clicks.Should().Equal("open"), TimeSpan.FromSeconds(10));
	}

	/// <summary>
	/// Verifies that the button carries the configured text, classes and size, and the item container
	/// carries the positioning and visibility classes.
	/// </summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "btn-sm")]
	[InlineData(ButtonSizes.Large, "btn-lg")]
	public void Appearance_ReflectsTheParameters(ButtonSizes size, string sizeClass)
	{
		var component = RenderDropdown([], p => p
			.Add(x => x.Size, size)
			.Add(x => x.CssClass, "btn-primary")
			.Add(x => x.ItemCssClass, "extra")
			.Add(x => x.ShiftRight, true)
			.Add(x => x.IsVisible, false));

		var item = component.Find("div.pdtoolbaritem");
		item.ClassList.Should().Contain(["pd-hidden", "align-right", "extra"]);
		var button = component.Find("button.dropdown-toggle");
		button.ClassList.Should().Contain(["btn-primary", sizeClass]);
		button.TextContent.Should().Contain("Actions");
	}

	/// <summary>
	/// Verifies that a medium size adds no size class and a visible item is not hidden.
	/// </summary>
	[Fact]
	public void Appearance_Defaults_AddNoSizeOrHiddenClass()
	{
		var component = RenderDropdown([]);

		component.Find("div.pdtoolbaritem").ClassList.Should().NotContain(["pd-hidden", "align-right"]);
		component.Find("button.dropdown-toggle").ClassList.Should().NotContain(["btn-sm", "btn-lg"]);
	}

	/// <summary>
	/// Verifies that items with shortcut keys are registered with the global event service while the
	/// dropdown lives, and unregistered when it is disposed.
	/// </summary>
	[Fact]
	public void ShortcutKeys_AreRegistered_AndUnregisteredOnDispose()
	{
		var component = RenderDropdown(StandardItems());

		Events.GetRegisteredShortcuts().Select(s => s.ToString()).Should().Contain("Ctrl-O");

		component.Instance.Dispose();

		Events.GetRegisteredShortcuts().Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that pressing a registered shortcut raises the click for its item, and an unmatched key raises nothing.
	/// </summary>
	[Fact]
	public void PressingAShortcut_RaisesTheItemsClick()
	{
		var component = RenderDropdown(StandardItems());

		Events.KeyUp(new KeyboardInfo { Key = "x", Code = "KeyX", CtrlKey = true });
		_clicks.Should().BeEmpty();

		Events.KeyUp(new KeyboardInfo { Key = "o", Code = "KeyO", CtrlKey = true });

		component.WaitForAssertion(() => _clicks.Should().Equal("open"));
	}

	/// <summary>
	/// Verifies that after disposal the dropdown no longer responds to shortcut keys.
	/// </summary>
	[Fact]
	public void PressingAShortcut_AfterDispose_RaisesNothing()
	{
		var component = RenderDropdown(StandardItems());
		component.Instance.Dispose();

		Events.KeyUp(new KeyboardInfo { Key = "o", Code = "KeyO", CtrlKey = true });

		_clicks.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that <see cref="PDMenuItem"/> children add themselves as items of the dropdown.
	/// </summary>
	[Fact]
	public void PDMenuItemChildren_AddThemselvesAsItems()
	{
		var component = RenderDropdown([], p => p.Add(x => x.ChildContent, builder =>
		{
			builder.OpenComponent<PDMenuItem>(0);
			builder.AddAttribute(1, nameof(PDMenuItem.Key), "child");
			builder.AddAttribute(2, nameof(PDMenuItem.Text), "Child item");
			builder.AddAttribute(3, nameof(PDMenuItem.IconCssClass), "fas fa-star");
			builder.CloseComponent();
		}));

		component.Instance.Items.Should().ContainSingle().Which.Key.Should().Be("child");
		component.Find("tr.pddropdownmenuitem").TextContent.Should().Contain("Child item");
		component.Find("tr.pddropdownmenuitem").Click();
		_clicks.Should().Equal("child");
	}

	/// <summary>
	/// Verifies that disabling the dropdown disables its button, and enabling restores it.
	/// </summary>
	[Fact]
	public async Task DisableAndEnable_ToggleTheButton()
	{
		var component = RenderDropdown([]);

		await component.InvokeAsync(() => component.Instance.Disable());
		component.Find("button.dropdown-toggle").HasAttribute("disabled").Should().BeTrue();

		await component.InvokeAsync(() => component.Instance.Enable());
		component.Find("button.dropdown-toggle").HasAttribute("disabled").Should().BeFalse();

		await component.InvokeAsync(() => component.Instance.SetEnabled(false));
		component.Instance.IsEnabled.Should().BeFalse();
		component.Find("button.dropdown-toggle").HasAttribute("disabled").Should().BeTrue();
	}
}
