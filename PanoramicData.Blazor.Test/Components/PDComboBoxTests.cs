using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDComboBox{TItem}"/> filters, orders and limits its items as the user types, supports
/// mouse and keyboard selection, and closes its drop-down when it should.
/// </summary>
public partial class PDComboBoxTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDComboBox.razor.js";
	// The drop-down closes after a real 400 ms delay; the wait is generous so it holds under full-suite load.
	private static readonly TimeSpan _blurTimeout = TimeSpan.FromSeconds(30);

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
