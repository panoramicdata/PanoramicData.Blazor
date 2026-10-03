using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests of the list <see cref="PDComboBox{TItem}"/> shows as the user types: filtering, ordering, templates and
/// selection.
/// </summary>
public partial class PDComboBoxTests
{
	/// <summary>
	/// Verifies that focusing with nothing selected opens the drop-down with the first items alphabetically,
	/// limited to <see cref="PDComboBox{TItem}.MaxResults"/>, with nothing active.
	/// </summary>
	[Fact]
	public void Focus_OpensAlphabeticalLimitedList()
	{
		var combo = RenderCombo();

		combo.Find("input").Focus();

		Options(combo).Should().Equal("Apple", "Banana", "Cherry", "Grape", "Kiwi");
		ActiveOption(combo).Should().BeNull();
		_module.VerifyNotInvoke("selectInputText");
	}

	/// <summary>
	/// Verifies that typing filters the list case-insensitively and shows the clear button.
	/// </summary>
	[Fact]
	public void Typing_FiltersAndShowsClearButton()
	{
		var combo = RenderCombo(p => p.Add(x => x.MaxResults, 10));

		combo.Find("input").Input("AN");

		Options(combo).Should().Equal("Banana", "Mango", "Orange");
		combo.FindAll("button.combo-clear").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that a custom order and filter are used in place of the defaults.
	/// </summary>
	[Fact]
	public void CustomOrderAndFilter_AreUsed()
	{
		var combo = RenderCombo(p => p
			.Add(x => x.OrderBy, item => item.Length)
			.Add(x => x.Filter, (item, text) => item.StartsWith(text, StringComparison.OrdinalIgnoreCase)));

		combo.Find("input").Input("");

		Options(combo).Should().Equal("Kiwi", "Apple", "Mango", "Grape", "Cherry");
	}

	/// <summary>
	/// Verifies that an item template and item-to-string function control how items are shown.
	/// </summary>
	[Fact]
	public void ItemTemplate_IsUsedForEachItem()
	{
		RenderFragment<string> template = item => builder => builder.AddMarkupContent(0, $"<b class=\"tpl\">{item}!</b>");
		var combo = RenderCombo(p => p.Add(x => x.ItemTemplate, template));

		combo.Find("input").Input("ki");

		combo.Find("ul.combo-dropdown li b.tpl").TextContent.Should().Be("Kiwi!");
	}

	/// <summary>
	/// Verifies that clicking an item selects it, raises the change, closes the drop-down and shows it in the input.
	/// </summary>
	[Fact]
	public void ClickingItem_SelectsIt()
	{
		var combo = RenderCombo();
		combo.Find("input").Input("an");

		combo.FindAll("ul.combo-dropdown li")[1].Click();

		_selected.Should().Equal("Mango");
		combo.Instance.SelectedItem.Should().Be("Mango");
		combo.FindAll("ul.combo-dropdown").Should().BeEmpty();
		combo.Find("input").GetAttribute("value").Should().Be("Mango");
	}

	/// <summary>
	/// Verifies that when nothing matches the no-results text is shown, or the no-results template when given.
	/// </summary>
	[Fact]
	public void NoMatches_ShowsNoResults()
	{
		var combo = RenderCombo(p => p.Add(x => x.NoResultsText, "Nothing"));
		combo.Find("input").Input("zzz");
		combo.Find(".combo-no-results").TextContent.Should().Be("Nothing");
		combo.FindAll("ul.combo-dropdown").Should().BeEmpty();

		RenderFragment<string> template = text => builder => builder.AddMarkupContent(0, $"<i class=\"none\">No {text}</i>");
		var templated = RenderCombo(p => p.Add(x => x.NoResultsTemplate, template));
		templated.Find("input").Input("zzz");
		templated.Find("i.none").TextContent.Should().Be("No zzz");
	}

	/// <summary>
	/// Verifies that the selected item is kept at the top of the list when requested, and is the active option.
	/// </summary>
	[Fact]
	public void ShowSelectedItemOnTop_InsertsSelectedItem()
	{
		var combo = RenderCombo(p => p
			.Add(x => x.SelectedItem, "Kiwi")
			.Add(x => x.ShowSelectedItemOnTop, true));

		combo.Find("input").Input("an");

		Options(combo).Should().Equal("Kiwi", "Banana", "Mango", "Orange");
		ActiveOption(combo).Should().Be("Kiwi");
		combo.FindAll(".combo-no-results").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that when only the pinned selected item remains the no-results message is shown beneath it, in
	/// both its plain and templated forms.
	/// </summary>
	[Fact]
	public void ShowSelectedItemOnTop_WithNoMatches_ShowsNoResultsBelowSelected()
	{
		var combo = RenderCombo(p => p
			.Add(x => x.SelectedItem, "Kiwi")
			.Add(x => x.ShowSelectedItemOnTop, true));
		combo.Find("input").Input("zzz");
		Options(combo).Should().Equal("Kiwi");
		combo.Find(".combo-no-results").TextContent.Should().Be("No results found");

		RenderFragment<string> template = text => builder => builder.AddMarkupContent(0, $"<i class=\"none\">No {text}</i>");
		var templated = RenderCombo(p => p
			.Add(x => x.SelectedItem, "Kiwi")
			.Add(x => x.ShowSelectedItemOnTop, true)
			.Add(x => x.NoResultsTemplate, template));
		templated.Find("input").Input("zzz");
		templated.Find("i.none").TextContent.Should().Be("No zzz");
	}
}
