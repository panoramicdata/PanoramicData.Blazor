using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

public class FilterTests
{
	[Fact]
	public void DateTimeWithTimeZoneSucceeds()
	{
		var filter = Filter.ParseMany("claimedAt:>\"15/08/2023 21:26:07 +01:00\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("\"2023-08-15T20:26:07Z\"");
		DateTimeOffset.TryParse(firstFilter.Value.Replace("\"", string.Empty), out DateTimeOffset _).ShouldBeTrue();
	}

	[Fact]
	public void DateTimeWithoutTimeZoneSucceeds()
	{
		var filter = Filter.ParseMany("claimedAt:>\"15/08/2023 21:26:07.000\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("\"2023-08-15T21:26:07Z\"");
		DateTimeOffset.TryParse(firstFilter.Value.Replace("\"", string.Empty), out DateTimeOffset _).ShouldBeTrue();
	}

	[Fact]
	public void DateTimeWithDateOnlySucceeds()
	{
		var filter = Filter.ParseMany("claimedAt:\"15/08/2023\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"2023-08-15T00:00:00Z\"");
		DateTimeOffset.TryParse(firstFilter.Value.Replace("\"", string.Empty), out DateTimeOffset _).ShouldBeTrue();
	}

	[Fact]
	public void DateTimeWithDateOnlyAlternativeFormatSucceeds()
	{
		var filter = Filter.ParseMany("claimedAt:\"15-08-2023\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"2023-08-15T00:00:00Z\"");
		DateTimeOffset.TryParse(firstFilter.Value.Replace("\"", string.Empty), out DateTimeOffset _).ShouldBeTrue();
	}

	[Fact]
	public void DateTimeWithDateAndTimeOnlySucceeds()
	{
		var filter = Filter.ParseMany("claimedAt:\"15/08/2023 21:00:00\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"2023-08-15T21:00:00Z\"");
		DateTimeOffset.TryParse(firstFilter.Value.Replace("\"", string.Empty), out DateTimeOffset _).ShouldBeTrue();
	}

	[Fact]
	public void DoubleSucceeds()
	{
		var filter = Filter.ParseMany("claimedAt:>\"2.4\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("\"2.4\"");
		double.TryParse(firstFilter.Value.Replace("\"", string.Empty), out double _).ShouldBeTrue();
	}

	[Fact]
	public void IntegerSucceeds()
	{
		var filter = Filter.ParseMany("claimedAt:>\"2\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("\"2\"");
		int.TryParse(firstFilter.Value.Replace("\"", string.Empty), out int _).ShouldBeTrue();
	}

	[Fact]
	public void StringSucceeds()
	{
		var filter = Filter.ParseMany("claimedAt:\"A string\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"A string\"");
	}

	[Fact]
	public void BooleanSucceeds()
	{
		var filter = Filter.ParseMany("claimedAt:\"True\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"True\"");
		bool.TryParse(firstFilter.Value.Replace("\"", string.Empty), out bool _).ShouldBeTrue();
	}
}