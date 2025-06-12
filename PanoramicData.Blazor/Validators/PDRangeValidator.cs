using FluentValidation;

namespace PanoramicData.Blazor.Validators;

public class PDRangeValidator : AbstractValidator<PDRange>
{
	public PDRangeValidator()
	{
		RuleFor(x => x.TrackHeight).InclusiveBetween(0, 1);
		RuleFor(x => x.Min).LessThanOrEqualTo(x => x.Max);
		RuleFor(x => x.Range).SetValidator(new NumericRangeValidator());
		RuleFor(x => x.Range.Start)
			.GreaterThanOrEqualTo(x => x.Min);
		//.WithMessage(x => $"Must be greater than or equal to 'Min' ({x.Min})");
		RuleFor(x => x.Range.End)
			.LessThanOrEqualTo(x => x.Max);
		//.WithMessage(x => $"Must be less than or equal to 'Max' ({x.Max})");
	}
}
