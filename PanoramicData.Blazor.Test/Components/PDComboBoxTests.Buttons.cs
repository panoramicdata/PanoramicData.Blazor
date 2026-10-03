using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Dropdown and clear button tests for <see cref="PDComboBox{TItem}"/>.
/// </summary>
public partial class PDComboBoxTests
{
	/// <summary>
	/// Verifies that the drop-down button opens the list and selects the input text, and a second press closes
	/// it and restores the selected item's text.
	/// </summary>
	[Fact]
	public void DropdownButton_TogglesList()
	{
		var combo = RenderCombo(p => p.Add(x => x.SelectedItem, "Cherry"));

		combo.Find("button.combo-dropdown-icon").Click();
		Options(combo).Should().HaveCount(5);
		ActiveOption(combo).Should().Be("Cherry");
		_module.VerifyInvoke("selectInputText");

		combo.Find("button.combo-dropdown-icon").Click();
		combo.FindAll("ul.combo-dropdown").Should().BeEmpty();
		combo.Find("input").GetAttribute("value").Should().Be("Cherry");
	}

	/// <summary>
	/// Verifies that the drop-down button closes the list without a selection too.
	/// </summary>
	[Fact]
	public void DropdownButton_ClosesWithoutSelection()
	{
		var combo = RenderCombo();

		combo.Find("button.combo-dropdown-icon").Click();
		combo.Find("button.combo-dropdown-icon").Click();

		combo.FindAll("ul.combo-dropdown").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the clear button empties the search, shows every item again and refocuses the input.
	/// </summary>
	[Fact]
	public void ClearButton_ResetsSearch()
	{
		var combo = RenderCombo();
		combo.Find("input").Input("an");

		combo.Find("button.combo-clear").Click();

		Options(combo).Should().HaveCount(5);
		combo.FindAll("button.combo-clear").Should().BeEmpty();
		_module.VerifyInvoke("selectInputText");
	}

	/// <summary>
	/// Verifies that the clear button with the list closed empties the search without reopening it.
	/// </summary>
	[Fact]
	public void ClearButton_WithListClosed_DoesNotReopen()
	{
		var combo = RenderCombo();
		combo.Find("input").Input("an");
		combo.Find("button.combo-dropdown-icon").Click();
		combo.FindAll("ul.combo-dropdown").Should().BeEmpty();

		combo.Find("button.combo-clear").Click();

		combo.FindAll("ul.combo-dropdown").Should().BeEmpty();
		combo.Find("input").GetAttribute("value").Should().BeEmpty();
	}
}
