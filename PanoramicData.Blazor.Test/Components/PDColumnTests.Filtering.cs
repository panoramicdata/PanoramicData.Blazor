using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Filter metadata tests for <see cref="PDColumn{TItem}"/>.
/// </summary>
public partial class PDColumnTests
{
	/// <summary>The filter data type is inferred from the field's property type.</summary>
	/// <param name="property">The Row property to bind.</param>
	/// <param name="expected">The expected filter data type.</param>
	[Theory]
	[InlineData(nameof(Row.Colour), FilterDataTypes.Enum)]
	[InlineData(nameof(Row.MaybeColour), FilterDataTypes.Enum)]
	[InlineData(nameof(Row.Flag), FilterDataTypes.Bool)]
	[InlineData(nameof(Row.MaybeFlag), FilterDataTypes.Bool)]
	[InlineData(nameof(Row.Name), FilterDataTypes.Text)]
	[InlineData(nameof(Row.When), FilterDataTypes.Date)]
	[InlineData(nameof(Row.MaybeWhen), FilterDataTypes.Date)]
	[InlineData(nameof(Row.Stamp), FilterDataTypes.Date)]
	[InlineData(nameof(Row.MaybeStamp), FilterDataTypes.Date)]
	[InlineData(nameof(Row.Count), FilterDataTypes.Numeric)]
	public void GetFilterDataType_IsInferredFromTheProperty(string property, FilterDataTypes expected)
		=> Column(FieldFor(property)).GetFilterDataType().Should().Be(expected);

	/// <summary>A column with no field is treated as numeric.</summary>
	[Fact]
	public void GetFilterDataType_NoField_IsNumeric()
		=> Column(null).GetFilterDataType().Should().Be(FilterDataTypes.Numeric);

	/// <summary>Strings and nullable value types are nullable for filtering; plain value types and no field are not.</summary>
	[Fact]
	public void GetFilterIsNullable_ReflectsThePropertyType()
	{
		Column(r => r.Name).GetFilterIsNullable().Should().BeTrue();
		Column(r => r.MaybeCount!).GetFilterIsNullable().Should().BeTrue();
		Column(r => r.Count).GetFilterIsNullable().Should().BeFalse();
		Column(null).GetFilterIsNullable().Should().BeFalse();
	}

	/// <summary>The filter key honours FilterKey and Display short names, lower-cases the first character otherwise, and chains nested members.</summary>
	[Fact]
	public void GetFilterKey_UsesAttributesAndChains()
	{
		Column(r => r.Keyed).GetFilterKey().Should().Be("custom");
		Column(r => r.Shorted).GetFilterKey().Should().Be("sn");
		Column(r => r.Name).GetFilterKey().Should().Be("name");
		Column(r => r.Child!.Name).GetFilterKey().Should().Be("child.name");
	}

	/// <summary>With no field the filter key is the column id.</summary>
	[Fact]
	public void GetFilterKey_NoField_IsTheId()
	{
		Column(null, c => c.Add(x => x.Id, "custom-id")).GetFilterKey().Should().Be("custom-id");
	}
}
