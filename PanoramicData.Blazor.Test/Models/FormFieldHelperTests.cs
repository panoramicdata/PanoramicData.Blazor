using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="FormFieldHelper{TItem}"/>.</summary>
public class FormFieldHelperTests
{
	/// <summary>A new helper has no click handlers, icon or tooltip.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var helper = new FormFieldHelper<Item>();

		helper.Click.Should().BeNull();
		helper.ClickAsync.Should().BeNull();
		helper.IconCssClass.Should().BeEmpty();
		helper.IconCssClass2.Should().BeNull();
		helper.ToolTip.Should().BeEmpty();
		helper.ToolTip2.Should().BeNull();
	}

	/// <summary>The handlers and item-dependent icon and tooltip functions are invoked as supplied.</summary>
	[Fact]
	public async Task Delegates_AreInvokable()
	{
		var result = new FormFieldResult();
		var field = new FormField<Item>();
		var helper = new FormFieldHelper<Item>
		{
			Click = _ => result,
			ClickAsync = _ => Task.FromResult(result),
			IconCssClass = "fa-search",
			IconCssClass2 = item => item.Name.Length > 0 ? "fa-check" : "fa-times",
			ToolTip = "Look up",
			ToolTip2 = item => $"Look up {item.Name}"
		};

		helper.Click(field).Should().BeSameAs(result);
		(await helper.ClickAsync(field)).Should().BeSameAs(result);
		helper.IconCssClass.Should().Be("fa-search");
		helper.IconCssClass2(new Item { Name = "x" }).Should().Be("fa-check");
		helper.ToolTip.Should().Be("Look up");
		helper.ToolTip2(new Item { Name = "Ann" }).Should().Be("Look up Ann");
	}

	private sealed class Item
	{
		public string Name { get; set; } = string.Empty;
	}
}
