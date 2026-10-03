using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Data loading, sorting and paging tests for <see cref="PDTable{TItem}"/>.
/// </summary>
public partial class PDTableTests
{
	/// <summary>BeforeFetch, AfterFetch, Ready and ItemsLoaded are all called around the first load.</summary>
	[Fact]
	public void The_first_load_raises_its_callbacks_and_lets_items_be_modified()
	{
		var events = new List<string>();
		var table = RenderTable(p => p
			.Add(x => x.BeforeFetch, () => events.Add("before"))
			.Add(x => x.AfterFetch, () => events.Add("after"))
			.Add(x => x.Ready, () => events.Add("ready"))
			.Add(x => x.ItemsLoaded, items => items.RemoveAt(0)));

		events.Should().Equal("before", "after", "ready");
		table.Instance.ItemsToDisplay.Select(i => i.Name).Should().Equal("Beta", "Gamma");
		table.Instance.IsBusy.Should().BeFalse();
		table.Instance.IsCancelled.Should().BeFalse();
	}

	/// <summary>With AutoLoad off nothing is fetched until a refresh is asked for.</summary>
	[Fact]
	public async Task AutoLoad_off_waits_for_a_refresh()
	{
		var ready = 0;
		var table = RenderTable(p => p
			.Add(x => x.AutoLoad, false)
			.Add(x => x.Ready, () => ready++), waitForRows: false);

		ready.Should().Be(1);
		_provider.Requests.Should().BeEmpty();

		await table.InvokeAsync(table.Instance.RefreshAsync);
		table.Instance.ItemsToDisplay.Should().HaveCount(3);
	}

	/// <summary>A provider failure is passed to the exception handler rather than thrown.</summary>
	[Fact]
	public void A_provider_failure_reaches_the_exception_handler()
	{
		_provider.FailWith(new InvalidOperationException("boom"));
		Exception? caught = null;
		RenderTable(p => p.Add(x => x.ExceptionHandler, ex => caught = ex), waitForRows: false);

		caught.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("boom");
	}

	/// <summary>A table with no data provider reports the problem through the exception handler.</summary>
	[Fact]
	public void A_missing_data_provider_reaches_the_exception_handler()
	{
		var caught = new List<Exception>();
		Render<PDTable<Item>>(parameters => parameters
			.Add(p => p.ExceptionHandler, ex => caught.Add(ex)));

		caught.Should().NotBeEmpty();
	}

	/// <summary>HandleExceptionAsync logs and forwards an exception to the handler.</summary>
	[Fact]
	public async Task HandleExceptionAsync_forwards_to_the_handler()
	{
		Exception? caught = null;
		var table = RenderTable(p => p.Add(x => x.ExceptionHandler, ex => caught = ex));
		var error = new InvalidOperationException("forwarded");

		await table.InvokeAsync(() => table.Instance.HandleExceptionAsync(error));

		caught.Should().BeSameAs(error);
	}

	/// <summary>The search text parameter is passed to the provider.</summary>
	[Fact]
	public void SearchText_is_sent_to_the_provider()
	{
		RenderTable(p => p.Add(x => x.SearchText, "Be"));

		_provider.Requests.Last().SearchText.Should().Be("Be");
	}

	/// <summary>Enable, Disable, SetEnabled and SetStateHasChanged update the rendered state.</summary>
	[Fact]
	public async Task Enable_and_disable_update_the_markup()
	{
		var table = RenderTable();

		await table.InvokeAsync(table.Instance.Disable);
		table.Find("div.pdtable").ClassList.Should().Contain("disabled");
		await table.InvokeAsync(table.Instance.Enable);
		table.Find("div.pdtable").ClassList.Should().NotContain("disabled");
		await table.InvokeAsync(() => table.Instance.SetEnabled(false));
		await table.InvokeAsync(table.Instance.SetStateHasChanged);
		table.Find("div.pdtable").ClassList.Should().Contain("disabled");
	}

	/// <summary>CancelAsync cancels a fetch in progress, which the provider sees through its token.</summary>
	[Fact]
	public async Task CancelAsync_cancels_the_fetch_in_progress()
	{
		var table = RenderTable();
		var gate = _provider.HoldFetches();

		var refresh = table.InvokeAsync(table.Instance.RefreshAsync);
		table.WaitForAssertion(() => table.Instance.IsBusy.Should().BeTrue());
		await table.InvokeAsync(table.Instance.CancelAsync);
		table.Instance.IsCancelled.Should().BeTrue();

		gate.SetResult();
		await refresh;
		_provider.LastToken.IsCancellationRequested.Should().BeTrue();
		table.Instance.IsBusy.Should().BeFalse();
	}

	/// <summary>CancelAsync with nothing in progress does nothing.</summary>
	[Fact]
	public async Task CancelAsync_when_idle_does_nothing()
	{
		var table = RenderTable();

		await table.InvokeAsync(table.Instance.CancelAsync);

		table.Instance.IsCancelled.Should().BeFalse();
	}

	/// <summary>Changing the page on the page criteria refetches that page and raises PageChanged.</summary>
	[Fact]
	public async Task Changing_the_page_refetches_and_raises_PageChanged()
	{
		var criteria = new PageCriteria(1, 2);
		var pages = new List<uint>();
		var table = RenderTable(p => p
			.Add(x => x.PageCriteria, criteria)
			.Add(x => x.PageChanged, c => pages.Add(c.Page)));
		criteria.TotalCount.Should().Be(3);
		Names(table).Should().Equal("Alpha", "Beta");

		await table.InvokeAsync(() => criteria.Page = 2);

		table.WaitForAssertion(() => Names(table).Should().Equal("Gamma"));
		pages.Should().Equal(2u);
	}

	/// <summary>Changing the page size refetches and raises PageSizeChanged.</summary>
	[Fact]
	public async Task Changing_the_page_size_refetches_and_raises_PageSizeChanged()
	{
		var criteria = new PageCriteria(1, 2);
		var sizes = new List<uint>();
		var table = RenderTable(p => p
			.Add(x => x.PageCriteria, criteria)
			.Add(x => x.PageSizeChanged, c => sizes.Add(c.PageSize)));

		await table.InvokeAsync(() => criteria.PageSize = 10);

		table.WaitForAssertion(() => Names(table).Should().HaveCount(3));
		sizes.Should().Equal(10u);
	}

	/// <summary>After disposal the table no longer follows its page criteria.</summary>
	[Fact]
	public async Task Disposal_unsubscribes_from_the_page_criteria()
	{
		var criteria = new PageCriteria(1, 2);
		var table = RenderTable(p => p.Add(x => x.PageCriteria, criteria));
		var requests = _provider.Requests.Count;

		await table.Instance.DisposeAsync();
		criteria.Page = 2;

		_provider.Requests.Should().HaveCount(requests);
	}
}
