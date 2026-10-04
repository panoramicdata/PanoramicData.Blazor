using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Keyboard navigation tests for <see cref="PDComboBox{TItem}"/>.
/// </summary>
public partial class PDComboBoxTests
{
	/// <summary>
	/// Verifies that arrow keys open the list and move the active option within its bounds, and Enter selects the
	/// active option and blurs the input.
	/// </summary>
	[Fact]
	public void ArrowKeysAndEnter_SelectActiveOption()
	{
		var combo = RenderCombo(p => p.Add(x => x.MaxResults, 3));
		var input = combo.Find("input");

		input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
		Options(combo).Should().Equal("Apple", "Banana", "Cherry");
		ActiveOption(combo).Should().Be("Apple");

		foreach (var key in new[] { "ArrowDown", "ArrowDown", "ArrowDown", "ArrowUp" })
		{
			combo.Find("input").KeyDown(new KeyboardEventArgs { Key = key });
		}

		ActiveOption(combo).Should().Be("Banana");

		combo.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
		combo.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
		ActiveOption(combo).Should().Be("Apple");

		combo.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

		_selected.Should().Equal("Apple");
		combo.FindAll("ul.combo-dropdown").Should().BeEmpty();
		_module.VerifyInvoke("blurInput");
	}

	/// <summary>
	/// Verifies that ArrowUp also opens a closed list, and that other keys do nothing while it is closed.
	/// </summary>
	[Fact]
	public void ArrowUpOpens_OtherKeysIgnoredWhenClosed()
	{
		var combo = RenderCombo();

		combo.Find("input").KeyDown(new KeyboardEventArgs { Key = "a" });
		combo.FindAll("ul.combo-dropdown").Should().BeEmpty();

		combo.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
		ActiveOption(combo).Should().Be("Apple");

		combo.Find("input").KeyDown(new KeyboardEventArgs { Key = "Tab" });
		ActiveOption(combo).Should().Be("Apple");
	}

	/// <summary>
	/// Verifies that Enter with no active option selects nothing.
	/// </summary>
	[Fact]
	public void Enter_WithNoActiveOption_SelectsNothing()
	{
		var combo = RenderCombo();
		combo.Find("input").Focus();

		combo.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

		_selected.Should().BeEmpty();
		combo.FindAll("ul.combo-dropdown").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that Escape closes the list, restores the selected item's text, and blurs the input.
	/// </summary>
	[Fact]
	public void Escape_ClosesAndRevertsToSelectedItem()
	{
		var combo = RenderCombo(p => p.Add(x => x.SelectedItem, "Grape"));
		combo.Find("input").Input("an");

		combo.Find("input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

		combo.FindAll("ul.combo-dropdown").Should().BeEmpty();
		combo.Find("input").GetAttribute("value").Should().Be("Grape");
		_module.VerifyInvoke("blurInput");
		_selected.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that Escape with nothing selected empties the input.
	/// </summary>
	[Fact]
	public void Escape_WithNoSelection_EmptiesInput()
	{
		var combo = RenderCombo();
		combo.Find("input").Input("an");

		combo.Find("input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

		combo.Find("input").GetAttribute("value").Should().BeEmpty();
	}
}
