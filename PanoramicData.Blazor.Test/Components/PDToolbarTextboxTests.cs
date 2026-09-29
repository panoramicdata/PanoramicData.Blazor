using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDToolbarTextbox"/>: layout parameters, value change and clear notifications, key presses
/// and the enable state methods.
/// </summary>
public class PDToolbarTextboxTests : BunitContext
{
	private readonly List<string> _values = [];
	private int _clearedCount;

	/// <summary>Sets up the rendering context.</summary>
	public PDToolbarTextboxTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDToolbarTextbox> RenderTextbox(Action<ComponentParameterCollectionBuilder<PDToolbarTextbox>>? configure = null)
		=> Render<PDToolbarTextbox>(p =>
		{
			p.Add(x => x.ValueChanged, (string v) => _values.Add(v));
			p.Add(x => x.Cleared, () => _clearedCount++);
			configure?.Invoke(p);
		});

	/// <summary>Defaults render a visible item with automatic width and no label.</summary>
	[Fact]
	public void Defaults_RenderWithAutoWidth_AndNoLabel()
	{
		var cut = RenderTextbox();

		var item = cut.Find("div.pdtoolbaritem");
		item.GetAttribute("style").Should().Be("width: Auto");
		item.ClassList.Should().NotContain("d-none").And.NotContain("align-right");
		cut.FindAll("label").Should().BeEmpty();
		cut.Instance.ItemStyle.Should().Be("width: Auto");
	}

	/// <summary>Label, width, visibility, shift and CSS parameters reach the markup.</summary>
	[Fact]
	public void LayoutParameters_ReachTheMarkup()
	{
		var cut = RenderTextbox(p => p
			.Add(x => x.Label, "Search")
			.Add(x => x.Width, "200px")
			.Add(x => x.IsVisible, false)
			.Add(x => x.ShiftRight, true)
			.Add(x => x.ItemCssClass, "item-x")
			.Add(x => x.CssClass, "box-x")
			.Add(x => x.Placeholder, "type here"));

		var item = cut.Find("div.pdtoolbaritem");
		item.GetAttribute("style").Should().Be("width: 200px");
		item.ClassList.Should().Contain("d-none").And.Contain("align-right").And.Contain("item-x");
		cut.Find("div.pdtoolbartextbox").ClassList.Should().Contain("box-x");
		cut.Find("label").TextContent.Should().Be("Search");
		cut.Find("input").GetAttribute("placeholder").Should().Be("type here");
	}

	/// <summary>Changing the text raises ValueChanged with the new text.</summary>
	[Fact]
	public void Change_RaisesValueChanged()
	{
		var cut = RenderTextbox();

		cut.Find("input").Change("hello");

		_values.Should().Equal("hello");
		cut.Instance.Value.Should().Be("hello");
	}

	/// <summary>A change to the text it already holds is not reported again.</summary>
	[Fact]
	public void Change_ToTheSameValue_IsNotReported()
	{
		var cut = RenderTextbox(p => p.Add(x => x.Value, "same"));

		cut.Find("input").Change("same");

		_values.Should().BeEmpty();
	}

	/// <summary>The clear button empties the value, raising ValueChanged and Cleared.</summary>
	[Fact]
	public void Clear_EmptiesTheValue_AndRaisesBothEvents()
	{
		var cut = RenderTextbox(p => p.Add(x => x.Value, "abc"));

		cut.Find("div.input-group-text button").Click();

		cut.Instance.Value.Should().BeEmpty();
		_values.Should().Contain(string.Empty);
		_clearedCount.Should().Be(1);
	}

	/// <summary>With the clear button turned off, none is rendered.</summary>
	[Fact]
	public void ShowClearButtonFalse_RendersNoClearButton()
	{
		var cut = RenderTextbox(p => p.Add(x => x.ShowClearButton, false));

		cut.FindAll("div.input-group-text button").Should().BeEmpty();
	}

	/// <summary>With key press events enabled, a key up is forwarded to Keypress.</summary>
	[Fact]
	public void KeypressEvent_ForwardsKeyPresses()
	{
		KeyboardEventArgs? pressed = null;
		var cut = RenderTextbox(p => p
			.Add(x => x.KeypressEvent, true)
			.Add(x => x.Keypress, (KeyboardEventArgs a) => pressed = a));

		cut.Find("input").KeyUp(new KeyboardEventArgs { Key = "Enter", Code = "Enter" });

		pressed.Should().NotBeNull();
		pressed!.Code.Should().Be("Enter");
	}

	/// <summary>Disable, Enable and SetEnabled change the input's disabled attribute.</summary>
	[Fact]
	public async Task EnableMethods_ToggleTheInput()
	{
		var cut = RenderTextbox(p => p.Add(x => x.Size, ButtonSizes.Small).Add(x => x.Key, "k").Add(x => x.ToolTip, "tip"));

		await cut.InvokeAsync(cut.Instance.Disable);
		cut.Find("input").HasAttribute("disabled").Should().BeTrue();

		await cut.InvokeAsync(cut.Instance.Enable);
		cut.Find("input").HasAttribute("disabled").Should().BeFalse();

		await cut.InvokeAsync(() => cut.Instance.SetEnabled(false));
		cut.Instance.IsEnabled.Should().BeFalse();
		cut.Instance.Key.Should().Be("k");
		cut.Instance.ToolTip.Should().Be("tip");
	}
}
