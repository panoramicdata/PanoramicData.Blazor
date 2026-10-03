using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDList{TItem}"/> saves and restores its selection through a state manager.
/// </summary>
public partial class PDListTests
{
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

	/// <summary>
	/// Verifies that without an ItemKeyFunction a saved multiple selection is restored in full, not just its
	/// first item (#181). The selection is saved as <see cref="Selection{TItem}.ToString"/>, which separates
	/// the items with a comma and a space.
	/// </summary>
	[Fact]
	public void StateManager_WithoutKeyFunction_RestoresEverySavedItem()
	{
		var state = new StateManager();
		state.Saved["l"] = new Selection<string> { Items = ["Apple", "Cherry"] }.ToString();

		var list = Render<PDList<string>>(p => p
			.Add(x => x.Id, "l")
			.Add(x => x.DataProvider, new ListDataProviderService<string>(["Apple", "Banana", "Cherry"]))
			.Add(x => x.SelectionMode, TableSelectionMode.Multiple)
			.AddCascadingValue<IAsyncStateManager>(state));

		list.Instance.Selection.Items.Should().Equal("Apple", "Cherry");
	}

	/// <summary>
	/// Verifies that without an ItemKeyFunction a selection saved by one list is restored by the next (#181).
	/// </summary>
	[Fact]
	public async Task StateManager_WithoutKeyFunction_RoundTripsTheSelection()
	{
		var state = new StateManager();
		var items = new ListDataProviderService<string>(["Apple", "Banana", "Cherry"]);
		var first = Render<PDList<string>>(p => p
			.Add(x => x.Id, "l")
			.Add(x => x.DataProvider, items)
			.Add(x => x.SelectionMode, TableSelectionMode.Multiple)
			.AddCascadingValue<IAsyncStateManager>(state));
		await first.InvokeAsync(() => first.FindAll("li.list-item")[0].ClickAsync(new MouseEventArgs()));
		await first.InvokeAsync(() => first.FindAll("li.list-item")[2].ClickAsync(new MouseEventArgs { CtrlKey = true }));

		var second = Render<PDList<string>>(p => p
			.Add(x => x.Id, "l")
			.Add(x => x.DataProvider, items)
			.Add(x => x.SelectionMode, TableSelectionMode.Multiple)
			.AddCascadingValue<IAsyncStateManager>(state));

		second.Instance.Selection.Items.Should().Equal("Apple", "Cherry");
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
