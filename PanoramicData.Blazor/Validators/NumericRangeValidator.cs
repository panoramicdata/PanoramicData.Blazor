using FluentValidation;

namespace PanoramicData.Blazor.Validators;

/// <summary>
/// Fluent validator for <see cref="NumericRange"/> that ensures the start value does not exceed the end value.
/// </summary>
public class NumericRangeValidator : AbstractValidator<NumericRange>
{
	/// <summary>
	/// Initializes a new instance of <see cref="NumericRangeValidator"/> and configures validation rules.
	/// </summary>
	public NumericRangeValidator()
	{
		RuleFor(x => x.Start).LessThanOrEqualTo(x => x.End);
	}
}
