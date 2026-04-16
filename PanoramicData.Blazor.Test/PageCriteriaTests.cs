using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

public class PageCriteriaTests
{
    [Fact]
    public void WhenConstructedWithDefaultsThenPageIsOne()
    {
        var pc = new PageCriteria();

        pc.Page.ShouldBe(1u);
        pc.PageSize.ShouldBe(10u);
        pc.TotalCount.ShouldBe(0u);
    }

    [Fact]
    public void WhenConstructedWithValuesThenPropertiesAreSet()
    {
        var pc = new PageCriteria(3, 25, 100);

        pc.Page.ShouldBe(3u);
        pc.PageSize.ShouldBe(25u);
        pc.TotalCount.ShouldBe(100u);
    }

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

    [Fact]
    public void WhenOnFirstPageThenIsFirstPageIsTrue()
    {
        var pc = new PageCriteria(1, 10, 50);

        pc.IsFirstPage.ShouldBeTrue();
        pc.IsLastPage.ShouldBeFalse();
    }

    [Fact]
    public void WhenOnLastPageThenIsLastPageIsTrue()
    {
        var pc = new PageCriteria(5, 10, 50);

        pc.IsLastPage.ShouldBeTrue();
        pc.IsFirstPage.ShouldBeFalse();
    }

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

    [Fact]
    public void WhenSettingPageBeyondPageCountThenPageIsNotChanged()
    {
		var pc = new PageCriteria(1, 10, 20)
		{
			Page = 5 // only 2 pages exist
		};

		pc.Page.ShouldBe(1u);
    }

    [Fact]
    public void WhenSettingPageToZeroThenPageIsNotChanged()
    {
		var pc = new PageCriteria(2, 10, 50)
		{
			Page = 0
		};

		pc.Page.ShouldBe(2u);
    }

    [Fact]
    public void WhenSettingPageSizeToZeroThenThrows()
    {
        var pc = new PageCriteria(1, 10, 50);

        Should.Throw<ArgumentOutOfRangeException>(() => pc.PageSize = 0);
    }

    [Fact]
    public void WhenPageSizeIncreasesAndPageExceedsNewPageCountThenPageIsAdjusted()
    {
		var pc = new PageCriteria(5, 10, 50)
		{
			PageSize = 25 // now only 2 pages
		}; // page 5 of 5

		pc.Page.ShouldBe(2u);
    }

    [Fact]
    public void WhenTotalCountDecreasesAndPageExceedsNewPageCountThenPageIsAdjusted()
    {
		var pc = new PageCriteria(5, 10, 50)
		{
			TotalCount = 15 // now only 2 pages
		};

		pc.Page.ShouldBe(2u);
    }

    [Fact]
    public void WhenTotalCountSetToZeroThenPageResetsToOne()
    {
		var pc = new PageCriteria(3, 10, 50)
		{
			TotalCount = 0
		};

		pc.Page.ShouldBe(1u);
    }

    [Fact]
    public void WhenPageChangedThenEventIsFired()
    {
        var pc = new PageCriteria(1, 10, 50);
        var fired = false;
        pc.PageChanged += (_, _) => fired = true;

        pc.Page = 2;

        fired.ShouldBeTrue();
    }

    [Fact]
    public void WhenPageSizeChangedThenEventIsFired()
    {
        var pc = new PageCriteria(1, 10, 50);
        var fired = false;
        pc.PageSizeChanged += (_, _) => fired = true;

        pc.PageSize = 25;

        fired.ShouldBeTrue();
    }

    [Fact]
    public void WhenTotalCountChangedThenEventIsFired()
    {
        var pc = new PageCriteria(1, 10, 50);
        var fired = false;
        pc.TotalCountChanged += (_, _) => fired = true;

        pc.TotalCount = 100;

        fired.ShouldBeTrue();
    }

    [Fact]
    public void WhenPreviousItemsThenReturnsCorrectCount()
    {
        var pc = new PageCriteria(3, 10, 50);

        pc.PreviousItems.ShouldBe(20u);
    }
}
