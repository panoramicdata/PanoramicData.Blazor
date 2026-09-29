using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Validators;

namespace PanoramicData.Blazor.Test.Validators;

/// <summary>Tests for <see cref="PDRangeValidator"/>.</summary>
/// <remarks>
/// The validator works on a <see cref="PDRange"/> component, whose values are parameters, so each instance
/// is obtained by rendering it rather than by assigning parameters by hand.
/// </remarks>
public class PDRangeValidatorTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDRangeValidatorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private PDRange RenderRange(double min, double max, double start, double end, double trackHeight)
	{
		// The component constrains its range while rendering, so the values under test are applied to the
		// range object afterwards, which is the state the validator is there to catch.
		var range = new NumericRange();
		var component = Render<PDRange>(parameters => parameters
			.Add(p => p.Min, min)
			.Add(p => p.Max, max)
			.Add(p => p.Range, range)
			.Add(p => p.TrackHeight, trackHeight)).Instance;
		range.Start = start;
		range.End = end;
		return component;
	}

	/// <summary>A range inside its bounds, with a proportional track height, is valid.</summary>
	[Fact]
	public void Validate_WellFormedRange_IsValid()
	{
		var result = new PDRangeValidator().Validate(RenderRange(0, 100, 10, 90, 0.5));

		result.IsValid.Should().BeTrue();
	}

	/// <summary>A track height outside zero to one is reported against the track height.</summary>
	[Theory]
	[InlineData(-0.1)]
	[InlineData(1.1)]
	public void Validate_TrackHeightOutOfRange_IsInvalid(double trackHeight)
	{
		var result = new PDRangeValidator().Validate(RenderRange(0, 100, 10, 90, trackHeight));

		result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(PDRange.TrackHeight));
	}

	/// <summary>A minimum above the maximum is reported against the minimum.</summary>
	[Fact]
	public void Validate_MinAboveMax_IsInvalid()
	{
		var result = new PDRangeValidator().Validate(RenderRange(100, 50, 60, 70, 0.5));

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == nameof(PDRange.Min));
	}

	/// <summary>A range whose start is after its end is rejected by the nested range validator.</summary>
	[Fact]
	public void Validate_InvertedRange_IsInvalid()
	{
		var result = new PDRangeValidator().Validate(RenderRange(0, 100, 80, 20, 0.5));

		result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Range.Start");
	}

	/// <summary>A range that starts below the minimum or ends above the maximum is invalid.</summary>
	[Theory]
	[InlineData(-5, 50)]
	[InlineData(50, 105)]
	public void Validate_RangeOutsideBounds_IsInvalid(double start, double end)
	{
		var result = new PDRangeValidator().Validate(RenderRange(0, 100, start, end, 0.5));

		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle();
	}
}
