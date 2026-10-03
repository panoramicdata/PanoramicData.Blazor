using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Value assignment and computed field tests for <see cref="PDColumn{TItem}"/>.
/// </summary>
public partial class PDColumnTests
{
	/// <summary>SetValue assigns a value of the property's type directly.</summary>
	[Fact]
	public void SetValue_AssignsMatchingType()
	{
		var row = new Row();

		Column(r => r.Name).SetValue(row, "new");

		row.Name.Should().Be("new");
	}

	/// <summary>SetValue converts from a string for a boxed value-type field.</summary>
	[Fact]
	public void SetValue_ConvertsFromString_ForBoxedValueTypes()
	{
		var row = new Row();

		Column(r => r.Count).SetValue(row, "42");

		row.Count.Should().Be(42);
	}

	/// <summary>SetValue with a null value on a nullable field clears it.</summary>
	[Fact]
	public void SetValue_Null_ClearsANullableField()
	{
		var row = new Row { MaybeCount = 3 };

		Column(r => r.MaybeCount!).SetValue(row, null);

		row.MaybeCount.Should().BeNull();
	}

	/// <summary>SetValue on a calculated column (no field) changes nothing.</summary>
	[Fact]
	public void SetValue_NoField_DoesNothing()
	{
		var row = new Row { Name = "same" };

		Column(null).SetValue(row, "other");

		row.Name.Should().Be("same");
	}

	/// <summary>A runtime title set with SetTitle overrides the Title parameter and TitleFunc (#172).</summary>
	[Fact]
	public async Task SetTitle_OverridesTheDeclaredTitle()
	{
		var table = RenderTable(c => c.Add(x => x.Field, r => r.Name).Add(x => x.Title, "Declared").Add(x => x.TitleFunc, _ => "Func"));
		var column = table.Instance.Columns[0];

		await table.InvokeAsync(() => column.SetTitle(string.Empty));

		column.GetTitle().Should().BeEmpty();
	}

	/// <summary>
	/// A computed field with no member registers without error: it has no derived type, property or
	/// member-based title, and filters as numeric (#171).
	/// </summary>
	[Fact]
	public void ComputedField_HasNoMemberMetadata()
	{
		var column = Column(r => r.Name + "!");

		column.Type.Should().BeNull();
		column.PropertyInfo.Should().BeNull();
		column.GetTitle().Should().BeEmpty();
		column.GetFilterDataType().Should().Be(FilterDataTypes.Numeric);
		column.GetFilterIsNullable().Should().BeFalse();
		column.GetRenderValue(new Row { Name = "a" }).Should().Be("a!");
	}

	/// <summary>SetValue on a computed field reports that it cannot be written rather than recursing (#171).</summary>
	[Fact]
	public void SetValue_ComputedField_Throws()
	{
		var boxed = Column(r => r.GetHashCode());
		var unboxed = Column(r => r.Name.Trim());

		var actBoxed = () => boxed.SetValue(new Row(), 1);
		var actUnboxed = () => unboxed.SetValue(new Row(), "x");

		actBoxed.Should().Throw<PDTableException>();
		actUnboxed.Should().Throw<PDTableException>();
	}

	/// <summary>SetValue rejects a null item.</summary>
	[Fact]
	public void SetValue_NullItem_Throws()
	{
		var act = () => Column(r => r.Name).SetValue(null, "x");

		act.Should().Throw<ArgumentNullException>();
	}
}
