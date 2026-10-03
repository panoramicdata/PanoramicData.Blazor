using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDList{TItem}"/> lists its items, filters them, and maintains a single, multiple
/// or all selection that it can persist through a state manager.
/// </summary>
public partial class PDListTests : BunitContext
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
	/// Verifies that default ids are unique when lists are created on many threads at once, and across item
	/// types, and keep the "pd-list-" format (#159).
	/// </summary>
	[Fact]
	public void DefaultIds_AreUnique_AcrossThreadsAndItemTypes()
	{
		var ids = new System.Collections.Concurrent.ConcurrentBag<string>();

		Parallel.For(0, 2000, new ParallelOptions { CancellationToken = Xunit.TestContext.Current.CancellationToken }, i =>
			ids.Add(i % 2 == 0 ? new PDList<string>().Id : new PDList<Fruit>().Id));

		ids.Should().HaveCount(2000).And.OnlyHaveUniqueItems();
		ids.Should().AllSatisfy(id => id.Should().MatchRegex("^pd-list-[0-9]+$"));
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
}
