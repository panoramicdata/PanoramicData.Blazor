using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDToolbar"/>: the toolbar that renders application supplied items and child content.
/// </summary>
public class PDToolbarTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDToolbarTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>
	/// Verifies that child content and the extra CSS class are both rendered on the toolbar.
	/// </summary>
	[Fact]
	public void ChildContent_IsRenderedInsideTheToolbar()
	{
		var component = Render<PDToolbar>(parameters => parameters
			.Add(p => p.CssClass, "my-toolbar")
			.AddChildContent("<span class=\"custom\">Custom</span>"));

		component.Find("div.pdtoolbar").ClassList.Should().Contain("my-toolbar");
		component.Find(".pdtoolbar .custom").TextContent.Should().Be("Custom");
	}

	/// <summary>
	/// Verifies that each kind of item is rendered as its matching toolbar component, in order.
	/// </summary>
	[Fact]
	public void Items_AreRenderedAsTheirMatchingComponents()
	{
		var component = Render<PDToolbar>(parameters => parameters
			.Add(p => p.Items, [
				new ToolbarButton { Key = "save", Text = "Save" },
				new ToolbarSeparator { Key = "sep" },
				new ToolbarTextBox { Key = "search", Label = "Search" }
			]));

		component.FindComponents<PDToolbarButton>().Should().ContainSingle()
			.Which.Instance.Text.Should().Be("Save");
		component.FindComponents<PDToolbarSeparator>().Should().ContainSingle();
		component.FindComponents<PDToolbarTextbox>().Should().ContainSingle()
			.Which.Instance.Label.Should().Be("Search");
	}

	/// <summary>
	/// Verifies that a button without its own size takes the toolbar's size, and one with a size keeps it.
	/// </summary>
	[Fact]
	public void Buttons_TakeTheToolbarSizeUnlessTheyHaveTheirOwn()
	{
		var component = Render<PDToolbar>(parameters => parameters
			.Add(p => p.ButtonSize, ButtonSizes.Small)
			.Add(p => p.Items, [
				new ToolbarButton { Key = "a", Text = "A" },
				new ToolbarButton { Key = "b", Text = "B", Size = ButtonSizes.Large }
			]));

		var sizes = component.FindComponents<PDToolbarButton>().Select(b => b.Instance.Size);
		sizes.Should().Equal(ButtonSizes.Small, ButtonSizes.Large);
	}

	/// <summary>
	/// Verifies that clicking an item button raises <see cref="PDToolbar.ButtonClick"/> with that button's key.
	/// </summary>
	[Fact]
	public async Task ClickingAButton_RaisesButtonClickWithItsKey()
	{
		var keys = new List<string>();
		var component = Render<PDToolbar>(parameters => parameters
			.Add(p => p.Items, [new ToolbarButton { Key = "save", Text = "Save" }])
			.Add(p => p.ButtonClick, args => keys.Add(args.Key)));

		await component.Find("button").ClickAsync(new MouseEventArgs());

		keys.Should().Equal("save");
	}

	/// <summary>
	/// Verifies that a textbox item's value, key press and clear notifications reach the item's delegates.
	/// </summary>
	[Fact]
	public async Task TextBoxItem_ForwardsItsNotificationsToTheItemDelegates()
	{
		var values = new List<string>();
		var keys = new List<string>();
		var cleared = 0;
		var textbox = new ToolbarTextBox
		{
			Key = "search",
			ValueChanged = values.Add,
			Keypress = args => keys.Add(args.Key),
			Cleared = () => cleared++
		};
		var component = Render<PDToolbar>(parameters => parameters.Add(p => p.Items, [textbox]));
		var rendered = component.FindComponent<PDToolbarTextbox>().Instance;

		await component.InvokeAsync(() => rendered.ValueChanged.InvokeAsync("abc"));
		await component.InvokeAsync(() => rendered.Keypress.InvokeAsync(new KeyboardEventArgs { Key = "Enter" }));
		await component.InvokeAsync(() => rendered.Cleared.InvokeAsync());

		values.Should().Equal("abc");
		keys.Should().Equal("Enter");
		cleared.Should().Be(1);
	}

	/// <summary>
	/// Verifies that a textbox item without delegates tolerates the notifications rather than throwing.
	/// </summary>
	[Fact]
	public async Task TextBoxItem_WithoutDelegates_IgnoresTheNotifications()
	{
		var component = Render<PDToolbar>(parameters => parameters.Add(p => p.Items, [new ToolbarTextBox { Key = "search" }]));
		var rendered = component.FindComponent<PDToolbarTextbox>().Instance;

		var act = async () =>
		{
			await component.InvokeAsync(() => rendered.ValueChanged.InvokeAsync("abc"));
			await component.InvokeAsync(() => rendered.Keypress.InvokeAsync(new KeyboardEventArgs()));
			await component.InvokeAsync(() => rendered.Cleared.InvokeAsync());
		};

		await act.Should().NotThrowAsync();
	}
}
