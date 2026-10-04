using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that the date, time, offset and time zone inputs of <see cref="PDDateTimeOffset"/> edit the value.
/// </summary>
public partial class PDDateTimeOffsetTests
{
	/// <summary>A valid date keeps the time and offset and raises ValueChanged.</summary>
	[Fact]
	public void DateInput_KeepsTheTimeAndOffset()
	{
		var component = RenderEditor();

		component.Find("input.date").Input("2027-01-02");

		_changes.Should().Equal(new DateTimeOffset(2027, 1, 2, 15, 9, 26, TimeSpan.FromHours(2)));
		component.Find("input.date").ClassList.Should().NotContain("invalid");
	}

	/// <summary>An invalid or null date marks the date input invalid and raises nothing.</summary>
	[Theory]
	[InlineData("tomorrow")]
	[InlineData(null)]
	public void InvalidDateInput_IsMarkedInvalid(string? text)
	{
		var component = RenderEditor();

		component.Find("input.date").Input(text);

		component.Find("input.date").ClassList.Should().Contain("invalid");
		_changes.Should().BeEmpty();
	}

	/// <summary>A valid time keeps the date and offset and raises ValueChanged.</summary>
	[Fact]
	public void TimeInput_KeepsTheDateAndOffset()
	{
		var component = RenderEditor();

		component.Find("input.time").Input("08:00:00");

		_changes.Should().Equal(new DateTimeOffset(2026, 3, 14, 8, 0, 0, TimeSpan.FromHours(2)));
	}

	/// <summary>An invalid or null time marks the time input invalid and raises nothing.</summary>
	[Theory]
	[InlineData("8 o'clock")]
	[InlineData(null)]
	public void InvalidTimeInput_IsMarkedInvalid(string? text)
	{
		var component = RenderEditor();

		component.Find("input.time").Input(text);

		component.Find("input.time").ClassList.Should().Contain("invalid");
		_changes.Should().BeEmpty();
	}

	/// <summary>Choosing an offset keeps the clock time and applies the new offset.</summary>
	[Fact]
	public void OffsetInput_KeepsTheClockTime_WithTheNewOffset()
	{
		var component = RenderEditor(showOffset: true);

		component.Find("select.offset").Input("5.5");

		_changes.Should().Equal(new DateTimeOffset(2026, 3, 14, 15, 9, 26, new TimeSpan(5, 30, 0)));
	}

	/// <summary>An offset that cannot be read is ignored.</summary>
	[Fact]
	public void OffsetInput_Unreadable_IsIgnored()
	{
		var component = RenderEditor(showOffset: true);

		component.Find("select.offset").Input("not a number");

		_changes.Should().BeEmpty();
		component.Instance.Value.Should().Be(_value);
	}

	/// <summary>Choosing a time zone raises TimeZoneIdChanged and reinterprets the clock time in that zone.</summary>
	[Fact]
	public void TimeZoneChange_ReinterpretsTheClockTimeInTheNewZone()
	{
		var zone = FixedZone(9);
		var zoneChanges = new List<string?>();
		var component = Render<PDDateTimeOffset>(parameters => parameters
			.Add(p => p.Value, _value)
			.Add(p => p.ShowTime, true)
			.Add(p => p.ShowTimeZones, true)
			.Add(p => p.TimeZoneIdChanged, (string? id) => zoneChanges.Add(id))
			.Add(p => p.ValueChanged, (DateTimeOffset v) => _changes.Add(v)));

		component.Find("select.timezone").Change(zone.Id);

		zoneChanges.Should().Equal(zone.Id);
		_changes.Should().Equal(new DateTimeOffset(2026, 3, 14, 15, 9, 26, TimeSpan.FromHours(9)));
	}

	/// <summary>With a time zone selected, typing a date takes that zone's offset for the new date.</summary>
	[Fact]
	public void DateInput_WithTimeZones_UsesTheSelectedZonesOffset()
	{
		var zone = FixedZone(-3);
		var component = RenderEditor(showTimeZones: true, timeZoneId: zone.Id);

		component.Find("input.date").Input("2027-01-02");

		_changes.Should().Equal(new DateTimeOffset(2027, 1, 2, 15, 9, 26, TimeSpan.FromHours(-3)));
	}

	/// <summary>Clearing the time zone selection resets the id to null and uses the local zone.</summary>
	[Fact]
	public void TimeZoneChange_ToEmpty_ResetsToLocal()
	{
		var zoneChanges = new List<string?>();
		var component = Render<PDDateTimeOffset>(parameters => parameters
			.Add(p => p.Value, _value)
			.Add(p => p.ShowTime, true)
			.Add(p => p.ShowTimeZones, true)
			.Add(p => p.TimeZoneId, TimeZoneInfo.Utc.Id)
			.Add(p => p.TimeZoneIdChanged, (string? id) => zoneChanges.Add(id))
			.Add(p => p.ValueChanged, (DateTimeOffset v) => _changes.Add(v)));

		component.Find("select.timezone").Change(string.Empty);

		zoneChanges.Should().Equal((string?)null);
		var local = new DateTime(2026, 3, 14, 15, 9, 26);
		_changes.Should().Equal(new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)));
	}
}
