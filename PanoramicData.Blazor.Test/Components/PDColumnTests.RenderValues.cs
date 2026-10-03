using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Value rendering, title and sort icon tests for <see cref="PDColumn{TItem}"/>.
/// </summary>
public partial class PDColumnTests
{
	/// <summary>Rendering a null item gives an empty string.</summary>
	[Fact]
	public void GetRenderValue_NullItem_IsEmpty()
		=> Column(r => r.Name).GetRenderValue(null!).Should().BeEmpty();

	/// <summary>A null field value renders empty; a column with no field renders empty.</summary>
	[Fact]
	public void GetRenderValue_NullValueOrNoField_IsEmpty()
	{
		Column(r => r.MaybeCount!).GetRenderValue(new Row()).Should().BeEmpty();
		Column(null).GetRenderValue(new Row()).Should().BeEmpty();
	}

	/// <summary>A plain value is rendered with ToString, a formatted value with the format string.</summary>
	[Fact]
	public void GetRenderValue_AppliesFormat()
	{
		var row = new Row { Count = 1234 };

		Column(r => r.Count).GetRenderValue(row).Should().Be("1234");
		Column(r => r.Count, c => c.Add(x => x.Format, "D6")).GetRenderValue(row).Should().Be("001234");
	}

	/// <summary>Password columns and sensitive items are masked with one star per character.</summary>
	[Fact]
	public void GetRenderValue_MasksPasswordsAndSensitiveValues()
	{
		var row = new Row { Name = "secret" };
		var password = Column(r => r.Name, c => c.Add(x => x.IsPassword, true));
		var sensitive = Column(r => r.Name, c => c.Add(x => x.IsSensitive, (item, _) => item?.Name == "secret"));

		password.GetRenderValue(row).Should().Be("******");
		sensitive.GetRenderValue(row).Should().Be("******");
	}

	/// <summary>Enum values render their Display name when they have one, otherwise their member name.</summary>
	[Fact]
	public void GetRenderValue_UsesEnumDisplayNames()
	{
		Column(r => r.Colour).GetRenderValue(new Row { Colour = Colour.DarkRed }).Should().Be("Dark red");
		Column(r => r.Colour).GetRenderValue(new Row { Colour = Colour.Blue }).Should().Be("Blue");
	}

	/// <summary>A nested field whose parent is null renders empty rather than throwing.</summary>
	[Fact]
	public void GetRenderValue_NestedNullParent_IsEmpty()
		=> Column(r => r.Child!.Name).GetRenderValue(new Row()).Should().BeEmpty();

	/// <summary>GetValue returns the raw field value, or null when there is no field.</summary>
	[Fact]
	public void GetValue_ReturnsRawValue()
	{
		Column(r => r.Count).GetValue(new Row { Count = 5 }).Should().Be(5);
		Column(null).GetValue(new Row()).Should().BeNull();
	}

	/// <summary>The property name comes from the field, or is empty with no field.</summary>
	[Fact]
	public void GetPropertyName_ComesFromTheField()
	{
		Column(r => r.Name).GetPropertyName().Should().Be(nameof(Row.Name));
		Column(null).GetPropertyName().Should().BeEmpty();
	}

	/// <summary>The sort icon reflects the sort direction.</summary>
	[Fact]
	public void SortIcon_ReflectsDirection()
	{
		var column = Column(r => r.Name);
		column.SortIcon.Should().Contain("fa-sort").And.NotContain("fa-sort-up").And.NotContain("fa-sort-down");

		column.SortDirection = SortDirection.Ascending;
		column.SortIcon.Should().Contain("fa-sort-up");

		column.SortDirection = SortDirection.Descending;
		column.SortIcon.Should().Contain("fa-sort-down");
	}

	/// <summary>The title comes from TitleFunc, then Title, then Display name, then property name.</summary>
	[Fact]
	public void GetTitle_FollowsItsPrecedence()
	{
		var withFunc = Column(r => r.Name, c => c.Add(x => x.TitleFunc, item => $"T:{item?.Name}").Add(x => x.Title, "ignored"));
		withFunc.GetTitle(new Row { Name = "x" }).Should().Be("T:x");

		Column(r => r.Name, c => c.Add(x => x.Title, "Explicit")).GetTitle().Should().Be("Explicit");

		Column(r => r.Shorted).GetTitle().Should().Be("Pretty");
		Column(r => r.Name).GetTitle().Should().Be(nameof(Row.Name));
		Column(null).GetTitle().Should().BeEmpty();
	}
}
