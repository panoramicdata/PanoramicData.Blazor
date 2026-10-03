using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDDateTimeOffset"/> always selects an option for the zone it computes with, including a
/// local zone whose id is not listed (#201), and that its inputs work in any culture.
/// </summary>
public partial class PDDateTimeOffsetTests
{
	/// <summary>
	/// With no time zone given, exactly one option is selected, and it is the zone the value is computed in. On
	/// Linux with TZ=Etc/UTC the local id is not among the system zones, and before #201 nothing was selected.
	/// </summary>
	[Theory]
	[InlineData(null)]
	[InlineData("Not/A_Zone")]
	public void NoOrUnknownTimeZone_SelectsTheLocalZonesOption(string? timeZoneId)
	{
		var component = RenderEditor(showTimeZones: true, timeZoneId: timeZoneId);

		var selected = component.FindAll("select.timezone option").Where(o => o.HasAttribute("selected")).ToList();

		selected.Should().ContainSingle($"the value is computed in the local zone, {TimeZoneInfo.Local.Id}");
		var selectedZone = TimeZoneInfo.FindSystemTimeZoneById(selected[0].GetAttribute("value")!);
		selectedZone.HasSameRules(TimeZoneInfo.Local).Should().BeTrue($"'{selectedZone.Id}' should be equivalent to '{TimeZoneInfo.Local.Id}'");
	}

	/// <summary>A zone that is listed resolves to its own option.</summary>
	[Fact]
	public void ResolveListedTimeZoneId_ForAListedZone_IsItsOwnId()
	{
		var listed = TimeZoneInfo.GetSystemTimeZones();

		PDDateTimeOffset.ResolveListedTimeZoneId(listed[0], listed).Should().Be(listed[0].Id);
	}

	/// <summary>
	/// An unlisted alias of a listed zone, as Etc/UTC is of UTC on Linux, resolves to the listed zone. Built as a
	/// custom zone so the test runs the same on every platform.
	/// </summary>
	[Fact]
	public void ResolveListedTimeZoneId_ForAnUnlistedAlias_IsTheListedZone()
	{
		var utc = TimeZoneInfo.FindSystemTimeZoneById("UTC");
		var etcUtc = TimeZoneInfo.CreateCustomTimeZone("Etc/UTC", TimeSpan.Zero, "Coordinated Universal Time", "Coordinated Universal Time");
		TimeZoneInfo[] listed = [FixedZone(9), utc];

		PDDateTimeOffset.ResolveListedTimeZoneId(etcUtc, listed).Should().Be(utc.Id);
	}

	/// <summary>A zone with no listed equivalent resolves to nothing, so the component adds an option for it.</summary>
	[Fact]
	public void ResolveListedTimeZoneId_ForAZoneWithNoEquivalent_IsNull()
	{
		var odd = TimeZoneInfo.CreateCustomTimeZone("Nowhere/Odd", TimeSpan.FromMinutes(17), "Odd", "Odd");

		PDDateTimeOffset.ResolveListedTimeZoneId(odd, TimeZoneInfo.GetSystemTimeZones()).Should().BeNull();
	}

	/// <summary>A zone that is listed is offered among the listed zones, and selected by its own id.</summary>
	[Fact]
	public void GetTimeZoneOptions_ForAListedZone_OffersTheListedZones()
	{
		var utc = TimeZoneInfo.FindSystemTimeZoneById("UTC");
		TimeZoneInfo[] listed = [FixedZone(9), utc];

		var (options, selectedId) = PDDateTimeOffset.GetTimeZoneOptions(utc, listed);

		options.Should().Equal(listed);
		selectedId.Should().Be(utc.Id);
	}

	/// <summary>A zone with no listed equivalent is offered first, ahead of the listed zones, and selected.</summary>
	[Fact]
	public void GetTimeZoneOptions_ForAZoneWithNoEquivalent_OffersItFirst()
	{
		var odd = TimeZoneInfo.CreateCustomTimeZone("Nowhere/Odd", TimeSpan.FromMinutes(17), "Odd", "Odd");
		TimeZoneInfo[] listed = [FixedZone(9), TimeZoneInfo.FindSystemTimeZoneById("UTC")];

		var (options, selectedId) = PDDateTimeOffset.GetTimeZoneOptions(odd, listed);

		options.Should().Equal(odd, listed[0], listed[1]);
		selectedId.Should().Be("Nowhere/Odd");
	}

	/// <summary>The native date and time inputs get ISO values in a culture with another calendar or time separator.</summary>
	[Theory]
	[InlineData("th-TH")]
	[InlineData("fi-FI")]
	[InlineData("de-DE")]
	public void NativeInputs_UseIsoValues_InAnyCulture(string culture)
	{
		using var scope = new CultureScope(culture);

		var component = RenderEditor();

		component.Find("input.date").GetAttribute("value").Should().Be("2026-03-14");
		component.Find("input.time").GetAttribute("value").Should().Be("15:09:26");
	}

	/// <summary>A half-hour offset option's value is read back as that offset in a culture with a decimal comma.</summary>
	[Theory]
	[InlineData("de-DE")]
	[InlineData("en-GB")]
	public async Task OffsetOption_ReadsBackItsOwnValue_InAnyCulture(string culture)
	{
		using var scope = new CultureScope(culture);
		var component = RenderEditor(showOffset: true);
		var value = component.FindAll("select.offset option").Single(o => o.TextContent.StartsWith("+05:30", StringComparison.Ordinal)).GetAttribute("value");

		await component.Find("select.offset").InputAsync(new ChangeEventArgs { Value = value });

		_changes.Should().Equal(new DateTimeOffset(2026, 3, 14, 15, 9, 26, new TimeSpan(5, 30, 0)));
	}
}
