using AwesomeAssertions;
using PanoramicData.Blazor.Attributes;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test.Attributes;

/// <summary>Tests for <see cref="RequiredGuidAttribute"/>.</summary>
public class RequiredGuidAttributeTests
{
	/// <summary>A non-empty guid is valid.</summary>
	[Fact]
	public void IsValid_NonEmptyGuid_IsTrue()
	{
		new RequiredGuidAttribute().IsValid(Guid.NewGuid()).Should().BeTrue();
	}

	/// <summary>Null, the empty guid and values that are not guids at all are invalid.</summary>
	[Fact]
	public void IsValid_NullEmptyOrNonGuid_IsFalse()
	{
		var attribute = new RequiredGuidAttribute();

		attribute.IsValid(null).Should().BeFalse();
		attribute.IsValid(Guid.Empty).Should().BeFalse();
		attribute.IsValid(Guid.NewGuid().ToString()).Should().BeFalse();
		attribute.IsValid(42).Should().BeFalse();
	}

	/// <summary>The error message names the field that is required.</summary>
	[Fact]
	public void ErrorMessage_NamesTheField()
	{
		var attribute = new RequiredGuidAttribute();

		attribute.FormatErrorMessage("Owner").Should().Be("Owner is required.");
	}

	/// <summary>Validating a model through the data annotations validator reports an empty guid property.</summary>
	[Fact]
	public void Validator_ReportsEmptyGuidProperty()
	{
		var model = new Model();
		var results = new List<ValidationResult>();

		var valid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);

		valid.Should().BeFalse();
		results.Should().ContainSingle().Which.ErrorMessage.Should().Be("OwnerId is required.");
	}

	private sealed class Model
	{
		[RequiredGuid]
		public Guid OwnerId { get; set; }
	}
}
