using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDDateTimeOffset"/> edits the date, time, offset and time zone of a value, and
/// follows the live clock while "Now" is ticked.
/// </summary>
public partial class PDDateTimeOffsetTests : BunitContext
{
	private static readonly DateTimeOffset _value = new(2026, 3, 14, 15, 9, 26, TimeSpan.FromHours(2));
	private readonly List<DateTimeOffset> _changes = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDDateTimeOffsetTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<PDDateTimeOffset> RenderEditor(bool showTime = true, bool showOffset = false, bool showTimeZones = false, string? timeZoneId = null)
		=> Render<PDDateTimeOffset>(parameters => parameters
			.Add(p => p.Value, _value)
			.Add(p => p.ShowTime, showTime)
			.Add(p => p.ShowOffset, showOffset)
			.Add(p => p.ShowTimeZones, showTimeZones)
			.Add(p => p.TimeZoneId, timeZoneId)
			.Add(p => p.ValueChanged, (DateTimeOffset v) => _changes.Add(v)));

	/// <summary>
	/// A system zone whose offset is the given one on every date these tests use. Not filtered on
	/// <see cref="TimeZoneInfo.SupportsDaylightSavingTime"/>: on Linux almost every IANA zone reports true because
	/// of historical rules (Asia/Tokyo included), so that filter finds nothing there.
	/// </summary>
	private static TimeZoneInfo FixedZone(double hours)
	{
		var offset = TimeSpan.FromHours(hours);
		DateTime[] dates = [new(2026, 3, 14, 15, 9, 26), new(2026, 7, 1, 12, 0, 0), new(2027, 1, 2, 15, 9, 26)];
		return TimeZoneInfo.GetSystemTimeZones()
			.First(z => z.BaseUtcOffset == offset && dates.All(d => z.GetUtcOffset(d) == offset));
	}

	/// <summary>By default only the date is shown.</summary>
	[Fact]
	public void Default_ShowsOnlyTheDate()
	{
		var component = RenderEditor(showTime: false);

		component.Find("input.date").GetAttribute("value").Should().Be("2026-03-14");
		component.FindAll("input.time").Should().BeEmpty();
		component.FindAll("select").Should().BeEmpty();
		component.FindAll("input[type=checkbox]").Should().BeEmpty();
	}

	/// <summary>ShowTime shows the time with its step, and ShowOffset adds an offset selector with the value's offset selected.</summary>
	[Fact]
	public void ShowOffset_ListsHalfHourOffsets_WithTheValuesSelected()
	{
		var component = Render<PDDateTimeOffset>(parameters => parameters
			.Add(p => p.Value, _value)
			.Add(p => p.ShowTime, true)
			.Add(p => p.ShowOffset, true)
			.Add(p => p.TimeStepSecs, 30));

		component.Find("input.time").GetAttribute("value").Should().Be("15:09:26");
		component.Find("input.time").GetAttribute("step").Should().Be("30");
		var options = component.FindAll("select.offset option");
		options.Should().HaveCount(51, "offsets run from -11 to +14 in half hours");
		options[0].TextContent.Should().StartWith("-11:00");
		options.Single(o => o.HasAttribute("selected")).TextContent.Should().StartWith("+02:00");
		options.Single(o => o.GetAttribute("value") == "0").TextContent.Should().StartWith(" 00:00");
		options.Single(o => o.GetAttribute("value") == "5.5").TextContent.Should().StartWith("+05:30");
	}

	/// <summary>An offset that a system time zone uses also shows that zone's name, without its UTC prefix.</summary>
	[Fact]
	public void OffsetOption_ShowsARepresentativeZoneName()
	{
		var zone = TimeZoneInfo.GetSystemTimeZones().First(z => z.BaseUtcOffset == TimeSpan.FromHours(9));
		var component = RenderEditor(showOffset: true);

		var option = component.FindAll("select.offset option").Single(o => o.GetAttribute("value") == "9");

		option.TextContent.Should().StartWith("+09:00  ");
		option.TextContent.Should().NotContain("(UTC");
		option.TextContent.Length.Should().BeGreaterThan("+09:00  ".Length, $"a zone such as '{zone.DisplayName}' uses +9");
	}

	/// <summary>ShowTimeZones replaces the offset selector with the system time zones, selecting the given one.</summary>
	[Fact]
	public void ShowTimeZones_ListsTheSystemZones_AndTakesPrecedence()
	{
		var utc = TimeZoneInfo.Utc.Id;

		var component = RenderEditor(showOffset: true, showTimeZones: true, timeZoneId: utc);

		component.FindAll("select.offset").Should().BeEmpty();
		var options = component.FindAll("select.timezone option");
		options.Should().HaveCount(TimeZoneInfo.GetSystemTimeZones().Count);
		options.Single(o => o.HasAttribute("selected")).GetAttribute("value").Should().Be(utc);
	}

	/// <summary>An unknown or missing time zone id falls back to the local zone for the value it produces.</summary>
	/// <remarks>
	/// Asserted through the offset the component applies; which option is selected is covered by
	/// <see cref="NoOrUnknownTimeZone_SelectsTheLocalZonesOption"/>.
	/// </remarks>
	[Theory]
	[InlineData("Not/A_Zone")]
	[InlineData(null)]
	public void UnknownTimeZone_FallsBackToLocal(string? timeZoneId)
	{
		var component = RenderEditor(showTimeZones: true, timeZoneId: timeZoneId);

		component.Find("input.date").Input("2027-01-02");

		var local = new DateTime(2027, 1, 2, 15, 9, 26);
		_changes.Should().Equal(new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)));
	}

	/// <summary>A disabled editor disables every input.</summary>
	[Fact]
	public void Disabled_DisablesEveryInput()
	{
		var component = Render<PDDateTimeOffset>(parameters => parameters
			.Add(p => p.ShowTime, true)
			.Add(p => p.ShowOffset, true)
			.Add(p => p.ShowNow, true)
			.Add(p => p.IsEnabled, false)
			.Add(p => p.IsVisible, false));

		component.FindAll("input, select").Should().AllSatisfy(e => e.HasAttribute("disabled").Should().BeTrue());
		component.Find("div.pddatetimeoffset").ClassList.Should().Contain("d-none");
	}

	/// <summary>Leaving any input raises Blur.</summary>
	[Fact]
	public void Blur_OnEachInput_RaisesBlur()
	{
		var blurs = 0;
		var component = Render<PDDateTimeOffset>(parameters => parameters
			.Add(p => p.ShowTime, true)
			.Add(p => p.ShowOffset, true)
			.Add(p => p.Blur, () => blurs++));

		component.Find("input.date").Blur();
		component.Find("input.time").Blur();
		component.Find("select.offset").Blur();

		blurs.Should().Be(3);
	}

	/// <summary>Leaving the time zone selector raises Blur.</summary>
	[Fact]
	public void Blur_OnTimeZoneSelector_RaisesBlur()
	{
		var blurs = 0;
		var component = Render<PDDateTimeOffset>(parameters => parameters
			.Add(p => p.ShowTime, true)
			.Add(p => p.ShowTimeZones, true)
			.Add(p => p.Blur, () => blurs++));

		component.Find("select.timezone").Blur();

		blurs.Should().Be(1);
	}
}
