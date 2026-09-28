using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDComboBox{TItem}"/> filters, orders and limits its items as the user types, supports
/// mouse and keyboard selection, and closes its drop-down when it should.
/// </summary>
public class PDComboBoxTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDComboBox.razor.js";
	private static readonly TimeSpan _blurTimeout = TimeSpan.FromSeconds(5);

	private readonly BunitJSModuleInterop _module;
	private readonly List<string> _selected = [];

	/// <summary>Sets up the rendering context and the combo box module.</summary>
	public PDComboBoxTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
	}

	private static List<string> Fruit() => ["Cherry", "Apple", "Banana", "Mango", "Orange", "Grape", "Kiwi"];

	private IRenderedComponent<PDComboBox<string>> RenderCombo(Action<ComponentParameterCollectionBuilder<PDComboBox<string>>>? configure = null)
		=> Render<PDComboBox<string>>(parameters =>
		{
			parameters
				.Add(p => p.Items, Fruit())
				.Add(p => p.SelectedItemChanged, (string item) => _selected.Add(item));
			configure?.Invoke(parameters);
		});

	private static List<string> Options(IRenderedComponent<PDComboBox<string>> combo)
		=> [.. combo.FindAll("ul.combo-dropdown li").Select(li => li.TextContent.Replace("✓", string.Empty, StringComparison.Ordinal).Trim())];

	private static string? ActiveOption(IRenderedComponent<PDComboBox<string>> combo)
		=> combo.FindAll("ul.combo-dropdown li.active").Select(li => li.TextContent.Replace("✓", string.Empty, StringComparison.Ordinal).Trim()).SingleOrDefault();

	/// <summary>
	/// Verifies the initial markup: the placeholder, no drop-down, no clear button, and an input showing nothing.
	/// </summary>
	[Fact]
	public void Initial_ShowsPlaceholderAndNoDropdown()
	{
		var combo = RenderCombo(p => p.Add(x => x.Placeholder, "Pick fruit"));

		var input = combo.Find("input.combo-input");
		input.GetAttribute("placeholder").Should().Be("Pick fruit");
		input.GetAttribute("value").Should().BeEmpty();
		input.HasAttribute("disabled").Should().BeFalse();
		input.HasAttribute("readonly").Should().BeFalse();
		combo.Find("div.pd-combobox").ClassList.Should().Contain("default-combobox");
		combo.FindAll("ul.combo-dropdown, .combo-no-results, button.combo-clear").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a selected item is shown in the input, and that disabled and read-only reach the input.
	/// </summary>
	[Fact]
	public void SelectedItem_DisabledAndReadOnly_AreRendered()
	{
		var combo = RenderCombo(p => p
			.Add(x => x.SelectedItem, "Mango")
			.Add(x => x.IsDisabled, true)
			.Add(x => x.IsReadOnly, true));

		var input = combo.Find("input");
		input.GetAttribute("value").Should().Be("Mango");
		input.HasAttribute("disabled").Should().BeTrue();
		input.HasAttribute("readonly").Should().BeTrue();
		combo.Find("button.combo-dropdown-icon").HasAttribute("disabled").Should().BeTrue();
	}

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

	/// <summary>
	/// Verifies that refocusing after typing, with an item selected, restores the typed search and selects the
	/// input text.
	/// </summary>
	[Fact]
	public void Refocus_WithSelection_RestoresTypedSearch()
	{
		var combo = RenderCombo(p => p.Add(x => x.SelectedItem, "Kiwi"));
		combo.Find("input").Input("ki");

		combo.Find("input").Focus();

		Options(combo).Should().Equal("Kiwi");
		ActiveOption(combo).Should().Be("Kiwi");
		combo.Find("input").GetAttribute("value").Should().Be("ki");
		_module.VerifyInvoke("selectInputText");
	}

	/// <summary>
	/// Verifies that losing focus closes the list after its short delay and restores the selected item's text.
	/// </summary>
	[Fact]
	public void Blur_ClosesListAfterDelay()
	{
		var combo = RenderCombo(p => p.Add(x => x.SelectedItem, "Apple"));
		combo.Find("input").Input("an");

		combo.Find("input").Blur();

		combo.WaitForAssertion(() => combo.FindAll("ul.combo-dropdown").Should().BeEmpty(), _blurTimeout);
		combo.Find("input").GetAttribute("value").Should().Be("Apple");
	}

	/// <summary>
	/// Verifies that losing focus with nothing selected also closes the list.
	/// </summary>
	[Fact]
	public void Blur_WithoutSelection_ClosesList()
	{
		var combo = RenderCombo();
		combo.Find("input").Focus();

		combo.Find("input").Blur();

		combo.WaitForAssertion(() => combo.FindAll("ul.combo-dropdown").Should().BeEmpty(), _blurTimeout);
	}

	/// <summary>
	/// Verifies that disposing the component, including a second time, is safe while a close is pending.
	/// </summary>
	[Fact]
	public async Task Dispose_IsSafeWhileCloseIsPending()
	{
		var combo = RenderCombo();
		combo.Find("input").Focus();
		combo.Find("input").Blur();
		var instance = combo.Instance;

		await DisposeComponentsAsync();
		await instance.DisposeAsync();

		combo.IsDisposed.Should().BeTrue();
	}
}
