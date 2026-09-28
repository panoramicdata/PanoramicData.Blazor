using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDPager"/> shows where the user is in a paged set and moves between pages.
/// </summary>
public class PDPagerTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDPagerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDPager> RenderPager(PageCriteria criteria, Action<ComponentParameterCollectionBuilder<PDPager>>? configure = null)
		=> Render<PDPager>(parameters =>
		{
			parameters.Add(p => p.PageCriteria, criteria);
			configure?.Invoke(parameters);
		});

	private static IElement Button(IRenderedComponent<PDPager> pager, string icon)
		=> pager.FindAll("button.pd-button").Single(b => b.InnerHtml.Contains(icon, StringComparison.Ordinal));

	private static bool IsDisabled(IElement element) => element.HasAttribute("disabled");

	/// <summary>
	/// Verifies that the first page of a multi-page set describes its range, and that only the forward
	/// buttons are enabled.
	/// </summary>
	[Fact]
	public void FirstPage_DescribesTheRange_AndEnablesOnlyForwardButtons()
	{
		var pager = RenderPager(new PageCriteria(1, 10, 95));

		pager.Find(".direction-buttons").TextContent.Should().Contain("1").And.Contain("of").And.Contain("10");
		pager.Find(".pdpager-page-description").TextContent.Should().Contain("1 - 10").And.Contain("95");
		IsDisabled(Button(pager, "fa-fast-backward")).Should().BeTrue();
		IsDisabled(Button(pager, "fa-backward\"")).Should().BeTrue();
		IsDisabled(Button(pager, "fa-forward\"")).Should().BeFalse();
		IsDisabled(Button(pager, "fa-fast-forward")).Should().BeFalse();
	}

	/// <summary>
	/// Verifies that each navigation button moves to the page it names.
	/// </summary>
	[Fact]
	public void NavigationButtons_MoveBetweenPages()
	{
		var criteria = new PageCriteria(1, 10, 95);
		var pager = RenderPager(criteria);

		Button(pager, "fa-forward\"").Click();
		criteria.Page.Should().Be(2);

		Button(pager, "fa-fast-forward").Click();
		criteria.Page.Should().Be(10);
		pager.Find(".pdpager-page-description").TextContent.Should().Contain("91 - 95");
		IsDisabled(Button(pager, "fa-fast-forward")).Should().BeTrue();

		Button(pager, "fa-backward\"").Click();
		criteria.Page.Should().Be(9);

		Button(pager, "fa-fast-backward").Click();
		criteria.Page.Should().Be(1);
	}

	/// <summary>
	/// Verifies that an empty set shows the no-items text and reports one page rather than zero.
	/// </summary>
	[Fact]
	public void EmptySet_ShowsNoItemsText_AndPageOneOfOne()
	{
		var pager = RenderPager(new PageCriteria(1, 10, 0), p => p.Add(x => x.NoItemsText, "Nothing here"));

		pager.Find(".pdpager-page-description").TextContent.Trim().Should().Be("Nothing here");
		var counts = pager.FindAll(".direction-buttons span.ms-1").Select(s => s.TextContent).ToList();
		counts.Should().Equal("1", "of", "1");
	}

	/// <summary>
	/// Verifies that choosing a page size updates the criteria, and that the choices offered are the ones configured.
	/// </summary>
	[Fact]
	public void PageSizeSelection_UpdatesTheCriteria()
	{
		var criteria = new PageCriteria(1, 10, 95);
		var pager = RenderPager(criteria, p => p.Add(x => x.PageSizeChoices, new uint[] { 10, 20 }));

		pager.FindAll("option").Select(o => o.TextContent).Should().Equal("10", "20");
		pager.Find("select").Change("20");

		criteria.PageSize.Should().Be(20);
		pager.Find(".direction-buttons").TextContent.Should().Contain("5");
	}

	/// <summary>
	/// Verifies that each area of the pager can be hidden independently.
	/// </summary>
	[Fact]
	public void ShowFlags_HideTheirAreas()
	{
		var pager = RenderPager(new PageCriteria(1, 10, 95), p => p
			.Add(x => x.ShowPageChangeButtons, false)
			.Add(x => x.ShowPageDescription, false)
			.Add(x => x.ShowPageSizeChoices, false)
			.Add(x => x.CssClass, "my-pager"));

		pager.Find("nav").ClassList.Should().Contain("my-pager");
		pager.FindAll(".direction-buttons").Should().BeEmpty();
		pager.FindAll("select").Should().BeEmpty();
		pager.Find(".pdpager-page-description").TextContent.Trim().Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the button size maps to the matching select size class.
	/// </summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "form-select-sm")]
	[InlineData(ButtonSizes.Large, "form-select-lg")]
	public void Size_SetsTheSelectSizeClass(ButtonSizes size, string expected)
	{
		var pager = RenderPager(new PageCriteria(1, 10, 95), p => p.Add(x => x.Size, size));

		pager.Find("select").ClassList.Should().Contain(expected);
	}

	/// <summary>
	/// Verifies that a medium or unset size adds no select size class.
	/// </summary>
	[Fact]
	public void Size_Medium_AddsNoSelectSizeClass()
	{
		var pager = RenderPager(new PageCriteria(1, 10, 95), p => p.Add(x => x.Size, ButtonSizes.Medium));

		pager.Find("select").ClassList.Should().NotContain(c => c.StartsWith("form-select-", StringComparison.Ordinal));
	}

	/// <summary>
	/// Verifies that disabling the pager disables every button and the page size choice, and that
	/// enabling it again restores them.
	/// </summary>
	[Fact]
	public async Task DisableAndEnable_ToggleEveryControl()
	{
		var pager = RenderPager(new PageCriteria(2, 10, 95));

		await pager.InvokeAsync(() => pager.Instance.Disable());
		pager.FindAll("button.pd-button").Should().OnlyContain(b => IsDisabled(b));
		IsDisabled(pager.Find("select")).Should().BeTrue();

		await pager.InvokeAsync(() => pager.Instance.Enable());
		pager.FindAll("button.pd-button").Should().OnlyContain(b => !IsDisabled(b));
		IsDisabled(pager.Find("select")).Should().BeFalse();

		await pager.InvokeAsync(() => pager.Instance.SetEnabled(false));
		pager.Instance.IsEnabled.Should().BeFalse();
		IsDisabled(pager.Find("select")).Should().BeTrue();
	}

	/// <summary>
	/// Verifies that a change to the total count made elsewhere is reflected, and that after disposal
	/// the pager no longer listens.
	/// </summary>
	[Fact]
	public async Task TotalCountChange_Rerenders_UntilDisposed()
	{
		var criteria = new PageCriteria(1, 10, 95);
		var pager = RenderPager(criteria);

		await pager.InvokeAsync(() => criteria.TotalCount = 250);
		pager.Find(".pdpager-page-description").TextContent.Should().Contain("250");

		pager.Instance.Dispose();
		var renders = pager.RenderCount;
		await pager.InvokeAsync(() => criteria.TotalCount = 300);

		pager.RenderCount.Should().Be(renders);
	}
}
