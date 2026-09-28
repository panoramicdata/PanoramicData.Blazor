using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDDateTimeOffset"/> edits the date, time, offset and time zone of a value, and
/// follows the live clock while "Now" is ticked.
/// </summary>
public class PDDateTimeOffsetTests : BunitContext
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

	private static TimeZoneInfo FixedZone(double hours)
		=> TimeZoneInfo.GetSystemTimeZones().First(z => !z.SupportsDaylightSavingTime && z.BaseUtcOffset == TimeSpan.FromHours(hours));

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

	/// <summary>An unknown or missing time zone id falls back to the local zone.</summary>
	[Theory]
	[InlineData("Not/A_Zone")]
	[InlineData(null)]
	public void UnknownTimeZone_FallsBackToLocal(string? timeZoneId)
	{
		var component = RenderEditor(showTimeZones: true, timeZoneId: timeZoneId);

		component.FindAll("select.timezone option").Single(o => o.HasAttribute("selected"))
			.GetAttribute("value").Should().Be(TimeZoneInfo.Local.Id);
	}

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

	private IRenderedComponent<PDDateTimeOffset> RenderNow(bool isNow, List<bool> nowChanges, int intervalMs = 20)
		=> Render<PDDateTimeOffset>(parameters => parameters
			.Add(p => p.Value, _value)
			.Add(p => p.ShowTime, true)
			.Add(p => p.ShowOffset, true)
			.Add(p => p.ShowNow, true)
			.Add(p => p.IsNow, isNow)
			.Add(p => p.LiveUpdateIntervalMs, intervalMs)
			.Add(p => p.IsNowChanged, (bool b) => nowChanges.Add(b))
			.Add(p => p.ValueChanged, (DateTimeOffset v) => _changes.Add(v)));

	/// <summary>Ticking Now raises IsNowChanged, jumps to the current instant, disables the inputs and keeps ticking.</summary>
	[Fact]
	public void TickingNow_FollowsTheLiveClock()
	{
		var nowChanges = new List<bool>();
		var component = RenderNow(false, nowChanges);
		var before = DateTimeOffset.UtcNow;

		component.Find("input[type=checkbox]").Change(true);

		nowChanges.Should().Equal(true);
		_changes.Should().NotBeEmpty();
		_changes[0].UtcDateTime.Should().BeOnOrAfter(before.UtcDateTime.AddSeconds(-1));
		component.Find("input.date").HasAttribute("disabled").Should().BeTrue();
		component.Find("select.offset").HasAttribute("disabled").Should().BeTrue();
		component.WaitForAssertion(() => _changes.Count.Should().BeGreaterThanOrEqualTo(3), TimeSpan.FromSeconds(5));
	}

	/// <summary>Unticking Now raises IsNowChanged and re-enables the inputs.</summary>
	[Fact]
	public void UntickingNow_ReenablesTheInputs()
	{
		var nowChanges = new List<bool>();
		var component = RenderNow(true, nowChanges);

		component.Find("input[type=checkbox]").Change("false");

		nowChanges.Should().Equal(false);
		component.Find("input.date").HasAttribute("disabled").Should().BeFalse();
		component.Find("input[type=checkbox]").HasAttribute("checked").Should().BeFalse();
	}

	/// <summary>A checkbox value that is neither a boolean nor readable as one counts as unticked.</summary>
	[Fact]
	public void UnreadableNowValue_CountsAsUnticked()
	{
		var nowChanges = new List<bool>();
		var component = RenderNow(false, nowChanges);

		component.Find("input[type=checkbox]").Change("on");

		nowChanges.Should().Equal(false);
		_changes.Should().BeEmpty();
	}

	/// <summary>Starting with Now ticked follows the clock from the first render, even with no interval given.</summary>
	[Theory]
	[InlineData(20)]
	[InlineData(0)]
	public void StartingWithNow_FollowsTheClock(int intervalMs)
	{
		var component = RenderNow(true, [], intervalMs);

		component.WaitForAssertion(() => _changes.Should().NotBeEmpty(), TimeSpan.FromSeconds(5));
		component.Find("input[type=checkbox]").HasAttribute("checked").Should().BeTrue();
	}

	/// <summary>Disposing stops the clock and may safely be repeated.</summary>
	[Fact]
	public async Task Dispose_StopsTheClock_AndIsRepeatable()
	{
		var component = RenderNow(true, []);
		component.WaitForAssertion(() => _changes.Should().NotBeEmpty(), TimeSpan.FromSeconds(5));

		await component.InvokeAsync(component.Instance.Dispose);
		var act = () => component.Instance.Dispose();

		act.Should().NotThrow();
	}}
