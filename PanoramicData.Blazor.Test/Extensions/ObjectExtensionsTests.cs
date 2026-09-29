using AwesomeAssertions;
using PanoramicData.Blazor.Extensions;
using System.Globalization;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="ObjectExtensions"/>.</summary>
public class ObjectExtensionsTests
{
	/// <summary>Null casts to null whatever the target type.</summary>
	[Fact]
	public void Cast_Null_IsNull()
	{
		((object?)null).Cast(typeof(int)).Should().BeNull();
	}

	/// <summary>Enums, including nullable enums, are parsed from their member names.</summary>
	[Fact]
	public void Cast_Enum_ParsesName()
	{
		"Friday".Cast(typeof(DayOfWeek)).Should().Be(DayOfWeek.Friday);
		"Monday".Cast(typeof(DayOfWeek?)).Should().Be(DayOfWeek.Monday);
	}

	/// <summary>Guids, including nullable guids, are parsed from strings.</summary>
	[Fact]
	public void Cast_Guid_Parses()
	{
		var id = Guid.NewGuid();

		id.ToString().Cast(typeof(Guid)).Should().Be(id);
		id.ToString().Cast(typeof(Guid?)).Should().Be(id);
	}

	/// <summary>A nullable integer is parsed from a string.</summary>
	[Fact]
	public void Cast_NullableInt_Parses()
	{
		"42".Cast(typeof(int?)).Should().Be(42);
	}

	/// <summary>Dates, including nullable dates, are parsed in the current culture.</summary>
	[Fact]
	public void Cast_DateTime_Parses()
	{
		var when = new DateTime(2024, 3, 4, 5, 6, 7, DateTimeKind.Unspecified);
		var text = when.ToString(CultureInfo.CurrentCulture);

		text.Cast(typeof(DateTime)).Should().Be(when);
		text.Cast(typeof(DateTime?)).Should().Be(when);
	}

	/// <summary>Date/time offsets, including nullable ones, are parsed in the current culture.</summary>
	[Fact]
	public void Cast_DateTimeOffset_Parses()
	{
		var when = new DateTimeOffset(2024, 3, 4, 5, 6, 7, TimeSpan.FromHours(1));
		var text = when.ToString(CultureInfo.CurrentCulture);

		text.Cast(typeof(DateTimeOffset)).Should().Be(when);
		text.Cast(typeof(DateTimeOffset?)).Should().Be(when);
	}

	/// <summary>Other types are converted with the framework's type conversion.</summary>
	[Fact]
	public void Cast_OtherTypes_UseChangeType()
	{
		"12".Cast(typeof(int)).Should().Be(12);
		5.Cast(typeof(string)).Should().Be("5");
		"1.5".Cast(typeof(decimal?)).Should().Be(decimal.Parse("1.5", CultureInfo.CurrentCulture));
	}

	/// <summary>A value that cannot be converted throws.</summary>
	[Fact]
	public void Cast_Unconvertible_Throws()
	{
		var act = () => "abc".Cast(typeof(int));

		act.Should().Throw<FormatException>();
	}
}
