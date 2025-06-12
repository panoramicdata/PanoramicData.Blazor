using FluentValidation;

namespace PanoramicData.Blazor.Validators;

public class NumericRangeValidator : AbstractValidator<NumericRange>
{
	public NumericRangeValidator()
	{
		RuleFor(x => x.Start).LessThanOrEqualTo(x => x.End);
	}
}
