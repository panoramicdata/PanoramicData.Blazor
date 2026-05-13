using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using Shouldly;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test;

public class IQueryableExtensionsTests
{
    #region Test model

    private sealed class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Rating Rating { get; set; }
        public Rating? NullableRating { get; set; }
        public string? Description { get; set; }
        public int Score { get; set; }
    }

    private enum Rating
    {
        [Display(Name = "Needs Improvement")]
        NeedsImprovement,

        [Display(Name = "In Progress")]
        InProgress,

        Good,
    }

    private static IQueryable<Item> GetItems() => new List<Item>
    {
        new() { Id = 1, Name = "Alpha",   Rating = Rating.NeedsImprovement, NullableRating = Rating.NeedsImprovement, Description = "first",  Score = 10 },
        new() { Id = 2, Name = "Beta",    Rating = Rating.InProgress,        NullableRating = Rating.InProgress,       Description = "second", Score = 20 },
        new() { Id = 3, Name = "Gamma",   Rating = Rating.Good,              NullableRating = null,                    Description = null,      Score = 30 },
        new() { Id = 4, Name = "Delta",   Rating = Rating.NeedsImprovement, NullableRating = Rating.Good,             Description = "",        Score = 40 },
        new() { Id = 5, Name = "Epsilon", Rating = Rating.Good,              NullableRating = null,                    Description = "fifth",  Score = 50 },
    }.AsQueryable();

    #endregion

    #region Enum Equals - display name round-trip (the core bug fix)

    [Fact]
    public void ApplyFilter_EnumEquals_DisplayName_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.Equals, "Rating", "Needs Improvement") { PropertyName = "Rating" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(2);
        result.ShouldAllBe(x => x.Rating == Rating.NeedsImprovement);
    }

    [Fact]
    public void ApplyFilter_EnumEquals_RawMemberName_FiltersCorrectly()
    {
        // Raw member name should still work (passthrough in GetMemberName)
        var filter = new Filter(FilterTypes.Equals, "Rating", "NeedsImprovement") { PropertyName = "Rating" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(2);
        result.ShouldAllBe(x => x.Rating == Rating.NeedsImprovement);
    }

    [Fact]
    public void ApplyFilter_EnumEquals_NoDisplayAttribute_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.Equals, "Rating", "Good") { PropertyName = "Rating" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(2);
        result.ShouldAllBe(x => x.Rating == Rating.Good);
    }

    #endregion

    #region Enum In - display name round-trip

    [Fact]
    public void ApplyFilter_EnumIn_DisplayNames_FiltersCorrectly()
    {
        // Pipe-separated display names as Filter.Format() would produce them
        var filter = new Filter(FilterTypes.In, "Rating", "Needs Improvement|In Progress") { PropertyName = "Rating" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(3);
        result.ShouldAllBe(x => x.Rating == Rating.NeedsImprovement || x.Rating == Rating.InProgress);
    }

    [Fact]
    public void ApplyFilter_EnumNotIn_DisplayName_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.NotIn, "Rating", "Needs Improvement") { PropertyName = "Rating" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(3);
        result.ShouldAllBe(x => x.Rating != Rating.NeedsImprovement);
    }

    #endregion

    #region Enum DoesNotEqual - display name round-trip

    [Fact]
    public void ApplyFilter_EnumDoesNotEqual_DisplayName_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.DoesNotEqual, "Rating", "Needs Improvement") { PropertyName = "Rating" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(3);
        result.ShouldAllBe(x => x.Rating != Rating.NeedsImprovement);
    }

    #endregion

    #region Nullable enum

    [Fact]
    public void ApplyFilter_NullableEnumEquals_DisplayName_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.Equals, "NullableRating", "Needs Improvement") { PropertyName = "NullableRating" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(1);
    }

    [Fact]
    public void ApplyFilter_NullableEnum_IsNull_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.IsNull, "NullableRating", string.Empty) { PropertyName = "NullableRating" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(2);
        result.ShouldAllBe(x => x.NullableRating == null);
    }

    #endregion

    #region String filters (regression - non-enum paths still work)

    [Fact]
    public void ApplyFilter_StringEquals_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.Equals, "Name", "Alpha") { PropertyName = "Name" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Alpha");
    }

    [Fact]
    public void ApplyFilter_StringContains_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.Contains, "Name", "a") { PropertyName = "Name" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(4); // Alpha, Beta, Gamma, Delta
    }

    [Fact]
    public void ApplyFilter_StringIsNull_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.IsNull, "Description", string.Empty) { PropertyName = "Description" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(3);
    }

    [Fact]
    public void ApplyFilter_StringIsEmpty_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.IsEmpty, "Description", string.Empty) { PropertyName = "Description" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(4);
    }

    #endregion

    #region Numeric filters (regression)

    [Fact]
    public void ApplyFilter_NumericGreaterThan_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.GreaterThan, "Score", "25") { PropertyName = "Score" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(3); // 30, 40, 50
    }

    [Fact]
    public void ApplyFilter_NumericRange_FiltersCorrectly()
    {
        var filter = new Filter(FilterTypes.Range, "Score", "20", "40") { PropertyName = "Score" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(3); // 20, 30, 40
    }

    #endregion

    #region Edge cases

    [Fact]
    public void ApplyFilter_EmptyKey_ReturnsAllItems()
    {
        var filter = new Filter(FilterTypes.Equals, string.Empty, "Alpha") { PropertyName = "Name" };

        var result = GetItems().ApplyFilter(filter).ToList();

        result.Count.ShouldBe(5);
    }

    [Fact]
    public void ApplyFilters_MultipleFilters_CombinesCorrectly()
    {
        var filters = new[]
        {
            new Filter(FilterTypes.Equals, "Rating", "Needs Improvement") { PropertyName = "Rating" },
            new Filter(FilterTypes.GreaterThan, "Score", "15") { PropertyName = "Score" },
        };

        var result = GetItems().ApplyFilters(filters).ToList();

        // NeedsImprovement items with Score > 15: Id=4 (Score=40)
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(4);
    }

    #endregion
}
