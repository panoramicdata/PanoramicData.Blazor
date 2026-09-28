using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDList{TItem}"/> lists its items, filters them, and maintains a single, multiple
/// or all selection that it can persist through a state manager.
/// </summary>
public class PDListTests : BunitContext
{
	private static readonly Fruit Apple = new(1, "Apple");
	private static readonly Fruit Banana = new(2, "Banana");
	private static readonly Fruit Cherry = new(3, "Cherry");

	private readonly ListDataProviderService<Fruit> _provider = new([Apple, Banana, Cherry]);
	private readonly List<string> _selections = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDListTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDList<Fruit>> RenderList(TableSelectionMode mode, Action<ComponentParameterCollectionBuilder<PDList<Fruit>>>? configure = null)
		=> Render<PDList<Fruit>>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, _provider)
				.Add(p => p.SelectionMode, mode)
				.Add(p => p.TextExpression, f => f.Name)
				.Add(p => p.SelectionChanged, s => _selections.Add(s.ToString()));
			configure?.Invoke(parameters);
		});

	private static IElement Item(IRenderedComponent<PDList<Fruit>> list, string text)
		=> list.FindAll("li.list-item").Single(li => li.TextContent.Trim() == text);

	private static List<string> CheckedItems(IRenderedComponent<PDList<Fruit>> list)
		=> [.. list.FindAll("li.list-item").Where(li => li.QuerySelector("i.fa-check-square") is not null).Select(li => li.TextContent.Trim())];

	/// <summary>
	/// Verifies that each item is listed by its text expression inside a container with the list's id and classes.
	/// </summary>
	[Fact]
	public void Items_AreListedByTheirText()
	{
		var list = RenderList(TableSelectionMode.None, p => p.Add(x => x.CssClass, "fruit").Add(x => x.ToolTip, "Pick"));

		list.FindAll("li.list-item").Select(li => li.TextContent.Trim()).Should().Equal("Apple", "Banana", "Cherry");
		var container = list.Find("div.pd-list");
		container.ClassList.Should().Contain("fruit");
		container.Id.Should().Be(list.Instance.Id);
		container.GetAttribute("title").Should().Be("Pick");
		list.FindAll("li.cursor-pointer").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that an item template replaces the default text, and that without a text expression the item's
	/// own text is shown.
	/// </summary>
	[Fact]
	public void ItemTemplateOrToString_RendersTheItem()
	{
		var templated = RenderList(TableSelectionMode.None, p => p.Add(x => x.ItemTemplate, f => $"<b>{f.Name.ToUpperInvariant()}</b>"));
		templated.FindAll("li b").Select(b => b.TextContent).Should().Equal("APPLE", "BANANA", "CHERRY");

		var plain = Render<PDList<Fruit>>(parameters => parameters.Add(p => p.DataProvider, _provider));

		plain.FindAll("li").First().TextContent.Trim().Should().Be(Apple.ToString());
	}

	/// <summary>
	/// Verifies that in single selection a click selects one item, another click moves the selection, and
	/// clicking the selected item again changes nothing.
	/// </summary>
	[Fact]
	public void SingleSelection_SelectsOneItem()
	{
		var list = RenderList(TableSelectionMode.Single);

		Item(list, "Apple").Click();
		Item(list, "Banana").Click();
		Item(list, "Banana").Click();

		list.Instance.Selection.Items.Should().Equal(Banana);
		Item(list, "Banana").ClassList.Should().Contain("selected");
		_selections.Should().HaveCount(2);
	}

	/// <summary>
	/// Verifies that in single selection with checkboxes, clicking the checked item unchecks it.
	/// </summary>
	[Fact]
	public void SingleSelectionWithCheckBoxes_TogglesTheItem()
	{
		var list = RenderList(TableSelectionMode.Single, p => p.Add(x => x.ShowCheckBoxes, true));

		Item(list, "Apple").Click();
		CheckedItems(list).Should().Equal("Apple");
		Item(list, "Apple").Click();

		CheckedItems(list).Should().BeEmpty();
		list.Instance.Selection.Items.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that in multiple selection a plain click replaces the selection, a control click adds or
	/// removes an item, and a shift click selects the range from the last item clicked.
	/// </summary>
	[Fact]
	public void MultipleSelection_SupportsControlAndShiftClicks()
	{
		var list = RenderList(TableSelectionMode.Multiple);

		Item(list, "Apple").Click();
		Item(list, "Apple").Click();
		Item(list, "Cherry").Click(new MouseEventArgs { CtrlKey = true });
		list.Instance.Selection.Items.Should().Equal(Apple, Cherry);

		Item(list, "Apple").Click(new MouseEventArgs { CtrlKey = true });
		list.Instance.Selection.Items.Should().Equal(Cherry);

		Item(list, "Banana").Click(new MouseEventArgs { ShiftKey = true });
		list.Instance.Selection.Items.Should().Equal(Apple, Banana);

		Item(list, "Apple").Click();
		list.Instance.Selection.Items.Should().Equal(Apple);
	}

	/// <summary>
	/// Verifies that a shift range covering every item becomes an all selection.
	/// </summary>
	[Fact]
	public void MultipleSelection_ShiftRangeOfEverything_SelectsAll()
	{
		var list = RenderList(TableSelectionMode.Multiple);

		Item(list, "Cherry").Click();
		Item(list, "Apple").Click(new MouseEventArgs { ShiftKey = true });

		list.Instance.Selection.AllSelected.Should().BeTrue();
		list.Instance.Selection.Items.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that with checkboxes, checking every item becomes an all selection, and unchecking one from
	/// an all selection leaves the others checked.
	/// </summary>
	[Fact]
	public void MultipleCheckBoxes_ToggleToAndFromAll()
	{
		var list = RenderList(TableSelectionMode.Multiple, p => p.Add(x => x.ShowCheckBoxes, true));

		Item(list, "Apple").Click();
		Item(list, "Banana").Click();
		Item(list, "Cherry").Click();
		list.Instance.Selection.AllSelected.Should().BeTrue();
		CheckedItems(list).Should().Equal("Apple", "Banana", "Cherry");

		Item(list, "Banana").Click();

		list.Instance.Selection.AllSelected.Should().BeFalse();
		list.Instance.Selection.Items.Should().Equal(Apple, Cherry);
	}

	/// <summary>
	/// Verifies that the all checkbox selects everything from none, clears an all selection, and treats a
	/// partial selection according to <see cref="PDList{TItem}.AllCheckBoxWhenPartial"/>.
	/// </summary>
	[Theory]
	[InlineData(SelectionBehaviours.SelectAll, true)]
	[InlineData(SelectionBehaviours.ClearAll, false)]
	public void AllCheckBox_CyclesTheSelection(SelectionBehaviours whenPartial, bool partialSelectsAll)
	{
		var list = RenderList(TableSelectionMode.Multiple, p => p
			.Add(x => x.ShowCheckBoxes, true)
			.Add(x => x.ShowAllCheckBox, true)
			.Add(x => x.AllCheckBoxWhenPartial, whenPartial));
		IElement All() => list.FindAll("li.list-item")[0];

		All().TextContent.Trim().Should().Be("(All)");
		All().Click();
		list.Instance.Selection.AllSelected.Should().BeTrue();
		All().QuerySelector("i")!.ClassList.Should().Contain("fa-check-square");
		All().Click();
		list.Instance.Selection.AllSelected.Should().BeFalse();

		Item(list, "Apple").Click();
		All().QuerySelector("i")!.ClassList.Should().Contain("fa-minus-square");
		All().Click();

		list.Instance.Selection.AllSelected.Should().Be(partialSelectsAll);
		list.Instance.Selection.Items.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a disabled list ignores clicks and is styled as disabled.
	/// </summary>
	[Fact]
	public void Disabled_IgnoresClicks()
	{
		var list = RenderList(TableSelectionMode.Single, p => p.Add(x => x.IsEnabled, false).Add(x => x.IsVisible, false));

		Item(list, "Apple").Click();

		list.Instance.Selection.Items.Should().BeEmpty();
		list.Find("div.pd-list").ClassList.Should().Contain(["disabled", "d-none"]);
		_selections.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that clicking an item in a list without selection clears any selection and raises nothing.
	/// </summary>
	[Fact]
	public void NoSelectionMode_ClickClearsAndRaisesNothing()
	{
		var list = RenderList(TableSelectionMode.None, p => p.Add(x => x.Selection, new Selection<Fruit> { Items = [Apple] }));

		Item(list, "Banana").Click();

		list.Instance.Selection.Items.Should().BeEmpty();
		_selections.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that filter text hides non-matching items, hides the all checkbox, and clears the selection.
	/// </summary>
	[Fact]
	public async Task Filter_HidesNonMatchingItems_AndClearsTheSelection()
	{
		var list = RenderList(TableSelectionMode.Multiple, p => p
			.Add(x => x.ShowFilter, true)
			.Add(x => x.ShowCheckBoxes, true)
			.Add(x => x.ShowAllCheckBox, true));
		Item(list, "Apple").Click();

		await list.InvokeAsync(() => list.FindComponent<PDTextBox>().Instance.ValueChanged.InvokeAsync("an"));

		list.FindAll("li.list-item").Select(li => li.TextContent.Trim()).Should().Equal("Banana");
		list.Instance.Selection.Items.Should().BeEmpty();
		list.Instance.ItemVisible(Apple).Should().BeFalse();
	}

	/// <summary>
	/// Verifies that a custom filter function decides visibility, and the selection can be kept across a filter.
	/// </summary>
	[Fact]
	public async Task Filter_WithACustomFunction_AndSelectionKept()
	{
		var list = RenderList(TableSelectionMode.Single, p => p
			.Add(x => x.ShowFilter, true)
			.Add(x => x.ClearSelectionOnFilter, false)
			.Add(x => x.FilterIncludeFunction, (f, text) => f.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) == text));
		Item(list, "Apple").Click();

		await list.InvokeAsync(() => list.FindComponent<PDTextBox>().Instance.ValueChanged.InvokeAsync("3"));

		list.FindAll("li.list-item").Select(li => li.TextContent.Trim()).Should().Equal("Cherry");
		list.Instance.Selection.Items.Should().Equal(Apple);
	}

	/// <summary>
	/// Verifies that Apply raises the current selection and Cancel raises cancel, and that without selection
	/// only Cancel is offered.
	/// </summary>
	[Fact]
	public void ApplyAndCancel_RaiseTheirCallbacks()
	{
		Selection<Fruit>? applied = null;
		var cancelled = 0;
		var list = RenderList(TableSelectionMode.Single, p => p
			.Add(x => x.ShowApplyCancelButtons, true)
			.Add(x => x.Apply, s => applied = s)
			.Add(x => x.Cancel, () => cancelled++));
		Item(list, "Banana").Click();

		list.Find(".footer button.btn-primary").Click();
		list.Find(".footer button.btn-secondary").Click();

		applied!.Items.Should().Equal(Banana);
		cancelled.Should().Be(1);
		RenderList(TableSelectionMode.None, p => p.Add(x => x.ShowApplyCancelButtons, true))
			.FindAll(".footer button").Select(b => b.TextContent.Trim()).Should().Equal("Cancel");
	}

	/// <summary>
	/// Verifies that the list starts with everything selected when asked, and that the public select and
	/// clear methods change the selection and announce it.
	/// </summary>
	[Fact]
	public async Task DefaultToSelectAll_AndPublicSelectionMethods()
	{
		var list = RenderList(TableSelectionMode.Multiple, p => p.Add(x => x.DefaultToSelectAll, true));
		list.Instance.Selection.AllSelected.Should().BeTrue();

		await list.InvokeAsync(() => list.Instance.ClearAllAsync());
		list.Instance.Selection.AllSelected.Should().BeFalse();
		await list.InvokeAsync(() => list.Instance.SelectAllAsync());

		list.Instance.Selection.AllSelected.Should().BeTrue();
		_selections.Should().Equal("(None)", "(All)");
	}

	/// <summary>
	/// Verifies that a sort expression is sent to the provider with the sort direction.
	/// </summary>
	[Fact]
	public void SortExpression_IsSentToTheProvider()
	{
		var provider = new RecordingProvider();
		Render<PDList<Fruit>>(parameters => parameters
			.Add(p => p.DataProvider, provider)
			.Add(p => p.SortExpression, f => f.Name)
			.Add(p => p.SortDirection, SortDirection.Descending));

		provider.LastRequest!.SortDirection.Should().Be(SortDirection.Descending);
		provider.LastRequest.SortFieldExpression.Should().NotBeNull();
	}

	/// <summary>
	/// Verifies that selections are saved to the state manager by key or as the all token.
	/// </summary>
	[Fact]
	public async Task StateManager_SavesTheSelection()
	{
		var state = new StateManager();
		var list = RenderList(TableSelectionMode.Multiple, p => p
			.Add(x => x.Id, "fruit-list")
			.Add(x => x.ItemKeyFunction, f => f.Id)
			.AddCascadingValue<IAsyncStateManager>(state));

		Item(list, "Apple").Click();
		Item(list, "Cherry").Click(new MouseEventArgs { CtrlKey = true });
		state.Saved["fruit-list"].Should().Be("1,3");

		await list.InvokeAsync(() => list.Instance.SelectAllAsync());
		state.Saved["fruit-list"].Should().Be("(All)");
	}

	/// <summary>
	/// Verifies that a saved selection of keys, or the all token, is restored on first render.
	/// </summary>
	[Theory]
	[InlineData("1,3", false, new[] { 1, 3 })]
	[InlineData("(All)", true, new int[0])]
	[InlineData("(None)", false, new int[0])]
	public void StateManager_RestoresTheSelection(string saved, bool allSelected, int[] expectedIds)
	{
		var state = new StateManager();
		state.Saved["fruit-list"] = saved;

		var list = RenderList(TableSelectionMode.Multiple, p => p
			.Add(x => x.Id, "fruit-list")
			.Add(x => x.ItemKeyFunction, f => f.Id)
			.AddCascadingValue<IAsyncStateManager>(state));

		list.Instance.Selection.AllSelected.Should().Be(allSelected);
		list.Instance.Selection.Items.Select(f => f.Id).Should().Equal(expectedIds);
	}

	/// <summary>A listed item.</summary>
	/// <param name="Id">Key.</param>
	/// <param name="Name">Display name.</param>
	public sealed record Fruit(int Id, string Name);

	/// <summary>A provider that remembers the last request.</summary>
	private sealed class RecordingProvider : DataProviderBase<Fruit>
	{
		public DataRequest<Fruit>? LastRequest { get; private set; }

		public override Task<DataResponse<Fruit>> GetDataAsync(DataRequest<Fruit> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			LastRequest = request;
			return Task.FromResult(new DataResponse<Fruit>([Apple], 1));
		}
	}

	/// <summary>An in-memory state manager.</summary>
	private sealed class StateManager : IAsyncStateManager
	{
		public Dictionary<string, object> Saved { get; } = [];

		public Task InitializeAsync() => Task.CompletedTask;

		public Task<T?> LoadStateAsync<T>(string key)
			=> Task.FromResult(Saved.TryGetValue(key, out var value) ? (T?)value : default);

		public Task RemoveStateAsync(string key)
		{
			Saved.Remove(key);
			return Task.CompletedTask;
		}

		public Task SaveStateAsync(string key, object state)
		{
			Saved[key] = state;
			return Task.CompletedTask;
		}
	}
}
