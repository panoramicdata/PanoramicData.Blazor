using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDLabel"/>: text and icon labels, labels bound to a data item, and their events.
/// </summary>
public class PDLabelTests : BunitContext
{
	/// <summary>
	/// Verifies that a plain label renders its icon, its text with a spacing class, its tooltip and CSS class.
	/// </summary>
	[Fact]
	public void TextAndIcon_AreRenderedWithSpacing()
	{
		var component = Render<PDLabel>(parameters => parameters
			.Add(p => p.Text, "Hello")
			.Add(p => p.IconCssClass, "fas fa-star")
			.Add(p => p.TextCssClass, "fw-bold")
			.Add(p => p.CssClass, "my-label")
			.Add(p => p.ToolTip, "Tip"));

		var label = component.Find("div.pd-label");
		label.ClassList.Should().Contain("my-label");
		label.GetAttribute("title").Should().Be("Tip");
		component.Find("i").ClassName.Should().Be("fas fa-star");
		var text = component.Find("span");
		text.TextContent.Should().Be("Hello");
		text.ClassList.Should().Contain("ms-1").And.Contain("fw-bold");
	}

	/// <summary>
	/// Verifies that text without an icon has no icon element and no spacing class.
	/// </summary>
	[Fact]
	public void TextWithoutIcon_HasNoIconOrSpacing()
	{
		var component = Render<PDLabel>(parameters => parameters.Add(p => p.Text, "Hello"));

		component.FindAll("i").Should().BeEmpty();
		component.Find("span").ClassList.Should().NotContain("ms-1");
	}

	/// <summary>
	/// Verifies that a label with neither text nor icon renders only its child content.
	/// </summary>
	[Fact]
	public void ChildContent_IsRenderedWhenThereIsNoDataItem()
	{
		var component = Render<PDLabel>(parameters => parameters.AddChildContent("<b class=\"child\">Child</b>"));

		component.FindAll("span").Should().BeEmpty();
		component.FindAll("i").Should().BeEmpty();
		component.Find(".child").TextContent.Should().Be("Child");
	}

	/// <summary>
	/// Verifies that a data item that is neither selectable nor displayable is shown by its string form.
	/// </summary>
	[Fact]
	public void PlainDataItem_IsShownByItsStringForm()
	{
		var component = Render<PDLabel>(parameters => parameters
			.Add(p => p.DataItem, 42)
			.Add(p => p.TextCssClass, "value")
			.Add(p => p.Text, "ignored"));

		component.Find("span.value").TextContent.Should().Be("42");
		component.FindAll("input").Should().BeEmpty();
		component.Markup.Should().NotContain("ignored");
	}

	/// <summary>
	/// Verifies that a displayable item shows its own icon and text, and a selectable one a checkbox reflecting its state.
	/// </summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(false, false)]
	public void SelectableDisplayItem_ShowsACheckboxIconAndText(bool isSelected, bool isEnabled)
	{
		var item = new Item { Text = "Item", IconCssClass = "fas fa-file", IsSelected = isSelected, IsEnabled = isEnabled };

		var component = Render<PDLabel>(parameters => parameters.Add(p => p.DataItem, item));

		var checkbox = component.Find("input[type=checkbox]");
		checkbox.HasAttribute("checked").Should().Be(isSelected);
		checkbox.HasAttribute("disabled").Should().Be(!isEnabled);
		component.Find("i").ClassList.Should().Contain("fa-file").And.Contain("me-1");
		component.Find("span.d-flex span").TextContent.Should().Be("Item");
	}

	/// <summary>
	/// Verifies that a displayable item with no icon renders no icon element.
	/// </summary>
	[Fact]
	public void DisplayItemWithoutIcon_HasNoIcon()
	{
		var component = Render<PDLabel>(parameters => parameters.Add(p => p.DataItem, new Item { Text = "Item" }));

		component.FindAll("i").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that ticking the checkbox selects the item and raises <see cref="PDLabel.SelectedChanged"/> with it.
	/// </summary>
	[Fact]
	public async Task TickingTheCheckbox_SelectsTheItemAndRaisesSelectedChanged()
	{
		var item = new Item { Text = "Item" };
		var raised = new List<ISelectable>();
		var component = Render<PDLabel>(parameters => parameters
			.Add(p => p.DataItem, item)
			.Add(p => p.SelectedChanged, selectable => raised.Add(selectable)));

		await component.Find("input").InputAsync(new ChangeEventArgs { Value = true });

		item.IsSelected.Should().BeTrue();
		raised.Should().ContainSingle().Which.Should().BeSameAs(item);
	}

	/// <summary>
	/// Verifies that reporting the state the item already has changes nothing and raises nothing.
	/// </summary>
	[Fact]
	public async Task ReportingTheCurrentState_RaisesNothing()
	{
		var item = new Item { Text = "Item", IsSelected = true };
		var raised = 0;
		var component = Render<PDLabel>(parameters => parameters
			.Add(p => p.DataItem, item)
			.Add(p => p.SelectedChanged, _ => raised++));

		await component.Find("input").InputAsync(new ChangeEventArgs { Value = true });

		item.IsSelected.Should().BeTrue();
		raised.Should().Be(0);
	}

	/// <summary>
	/// Verifies that click, mouse down and mouse enter are each raised from the label.
	/// </summary>
	[Fact]
	public async Task MouseEvents_AreRaised()
	{
		var events = new List<string>();
		var component = Render<PDLabel>(parameters => parameters
			.Add(p => p.Text, "Hello")
			.Add(p => p.Click, _ => events.Add("click"))
			.Add(p => p.MouseDown, _ => events.Add("down"))
			.Add(p => p.MouseEnter, _ => events.Add("enter")));

		var label = component.Find("div.pd-label");
		await label.MouseEnterAsync(new MouseEventArgs());
		await label.MouseDownAsync(new MouseEventArgs());
		await label.ClickAsync(new MouseEventArgs());

		events.Should().Equal("enter", "down", "click");
	}

	private sealed class Item : ISelectable, IDisplayItem
	{
		public bool IsSelected { get; set; }

		public bool IsEnabled { get; set; } = true;

		public string IconCssClass { get; set; } = string.Empty;

		public string Id { get; set; } = "1";

		public string Text { get; set; } = string.Empty;
	}
}
