using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the PageCriteria class.</summary>
public class PageCriteriaTests
{
    /// <summary>Verifies that default construction sets Page to 1, PageSize to 10, and TotalCount to 0.</summary>
    [Fact]
    public void WhenConstructedWithDefaultsThenPageIsOne()
    {
        var pc = new PageCriteria();

        pc.Page.ShouldBe(1u);
        pc.PageSize.ShouldBe(10u);
        pc.TotalCount.ShouldBe(0u);
    }

    /// <summary>Verifies that constructing with explicit values sets all Page, PageSize, and TotalCount properties correctly.</summary>
    [Fact]
    public void WhenConstructedWithValuesThenPropertiesAreSet()
    {
        var pc = new PageCriteria(3, 25, 100);

        pc.Page.ShouldBe(3u);
        pc.PageSize.ShouldBe(25u);
        pc.TotalCount.ShouldBe(100u);
    }

    /// <summary>Verifies that PageCount is calculated correctly for various total counts and page sizes.</summary>
    [Theory]
    [InlineData(100u, 10u, 10u)]
    [InlineData(101u, 10u, 11u)]
    [InlineData(0u, 10u, 0u)]
    [InlineData(1u, 10u, 1u)]
    [InlineData(50u, 25u, 2u)]
    public void WhenCalculatingPageCountThenReturnsCorrectValue(uint totalCount, uint pageSize, uint expectedPageCount)
    {
        var pc = new PageCriteria(1, pageSize, totalCount);

        pc.PageCount.ShouldBe(expectedPageCount);
    }

    /// <summary>Verifies that IsFirstPage is true and IsLastPage is false on the first page.</summary>
    [Fact]
    public void WhenOnFirstPageThenIsFirstPageIsTrue()
    {
        var pc = new PageCriteria(1, 10, 50);

        pc.IsFirstPage.ShouldBeTrue();
        pc.IsLastPage.ShouldBeFalse();
    }

    /// <summary>Verifies that IsLastPage is true and IsFirstPage is false on the last page.</summary>
    [Fact]
    public void WhenOnLastPageThenIsLastPageIsTrue()
    {
        var pc = new PageCriteria(5, 10, 50);

        pc.IsLastPage.ShouldBeTrue();
        pc.IsFirstPage.ShouldBeFalse();
    }

    /// <summary>Verifies that PageRangeStart and PageRangeEnd return the correct item index bounds for each page.</summary>
    [Theory]
    [InlineData(1u, 10u, 50u, 1u, 10u)]
    [InlineData(2u, 10u, 50u, 11u, 20u)]
    [InlineData(5u, 10u, 50u, 41u, 50u)]
    [InlineData(3u, 10u, 25u, 21u, 25u)]
    public void WhenCalculatingPageRangeThenReturnsCorrectValues(
        uint page, uint pageSize, uint totalCount, uint expectedStart, uint expectedEnd)
    {
        var pc = new PageCriteria(page, pageSize, totalCount);

        pc.PageRangeStart.ShouldBe(expectedStart);
        pc.PageRangeEnd.ShouldBe(expectedEnd);
    }

    /// <summary>Verifies that setting Page to a value beyond the available page count leaves the page unchanged.</summary>
    [Fact]
    public void WhenSettingPageBeyondPageCountThenPageIsNotChanged()
    {
		var pc = new PageCriteria(1, 10, 20)
		{
			Page = 5 // only 2 pages exist
		};

		pc.Page.ShouldBe(1u);
    }

    /// <summary>Verifies that setting Page to zero leaves the page unchanged.</summary>
    [Fact]
    public void WhenSettingPageToZeroThenPageIsNotChanged()
    {
		var pc = new PageCriteria(2, 10, 50)
		{
			Page = 0
		};

		pc.Page.ShouldBe(2u);
    }

    /// <summary>Verifies that setting PageSize to zero throws an ArgumentOutOfRangeException.</summary>
    [Fact]
    public void WhenSettingPageSizeToZeroThenThrows()
    {
        var pc = new PageCriteria(1, 10, 50);

        Should.Throw<ArgumentOutOfRangeException>(() => pc.PageSize = 0);
    }

    /// <summary>Verifies that when page size increases and the current page exceeds the new page count, the page is adjusted to the last valid page.</summary>
    [Fact]
    public void WhenPageSizeIncreasesAndPageExceedsNewPageCountThenPageIsAdjusted()
    {
		var pc = new PageCriteria(5, 10, 50)
		{
			PageSize = 25 // now only 2 pages
		}; // page 5 of 5

		pc.Page.ShouldBe(2u);
    }

    /// <summary>Verifies that when total count decreases and the current page exceeds the new page count, the page is adjusted.</summary>
    [Fact]
    public void WhenTotalCountDecreasesAndPageExceedsNewPageCountThenPageIsAdjusted()
    {
		var pc = new PageCriteria(5, 10, 50)
		{
			TotalCount = 15 // now only 2 pages
		};

		pc.Page.ShouldBe(2u);
    }

    /// <summary>Verifies that setting total count to zero resets the page to 1.</summary>
    [Fact]
    public void WhenTotalCountSetToZeroThenPageResetsToOne()
    {
		var pc = new PageCriteria(3, 10, 50)
		{
			TotalCount = 0
		};

		pc.Page.ShouldBe(1u);
    }

    /// <summary>Verifies that the PageChanged event is raised when the page number changes.</summary>
    [Fact]
    public void WhenPageChangedThenEventIsFired()
    {
        var pc = new PageCriteria(1, 10, 50);
        var fired = false;
        pc.PageChanged += (_, _) => fired = true;

        pc.Page = 2;

        fired.ShouldBeTrue();
    }

    /// <summary>Verifies that the PageSizeChanged event is raised when the page size changes.</summary>
    [Fact]
    public void WhenPageSizeChangedThenEventIsFired()
    {
        var pc = new PageCriteria(1, 10, 50);
        var fired = false;
        pc.PageSizeChanged += (_, _) => fired = true;

        pc.PageSize = 25;

        fired.ShouldBeTrue();
    }

    /// <summary>Verifies that the TotalCountChanged event is raised when the total count changes.</summary>
    [Fact]
    public void WhenTotalCountChangedThenEventIsFired()
    {
        var pc = new PageCriteria(1, 10, 50);
        var fired = false;
        pc.TotalCountChanged += (_, _) => fired = true;

        pc.TotalCount = 100;

        fired.ShouldBeTrue();
    }

    /// <summary>Verifies that PreviousItems returns the correct count of items before the current page.</summary>
    [Fact]
    public void WhenPreviousItemsThenReturnsCorrectCount()
    {
        var pc = new PageCriteria(3, 10, 50);

        pc.PreviousItems.ShouldBe(20u);
    }
}
