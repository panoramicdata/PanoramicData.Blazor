using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Validators;

namespace PanoramicData.Blazor.Test.Validators;

/// <summary>Tests for <see cref="NumericRangeValidator"/>.</summary>
public class NumericRangeValidatorTests
{
	/// <summary>A range whose start is at or before its end is valid.</summary>
	[Theory]
	[InlineData(0, 10)]
	[InlineData(5, 5)]
	[InlineData(-10, -1)]
	public void Validate_StartNotAfterEnd_IsValid(double start, double end)
	{
		var result = new NumericRangeValidator().Validate(new NumericRange(start, end));

		result.IsValid.Should().BeTrue();
	}

	/// <summary>A range whose start is after its end is invalid, and the error is reported against the start.</summary>
	[Fact]
	public void Validate_StartAfterEnd_IsInvalid()
	{
		var result = new NumericRangeValidator().Validate(new NumericRange(10, 1));

		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(NumericRange.Start));
	}
}
