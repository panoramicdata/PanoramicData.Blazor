using FluentValidation;

namespace PanoramicData.Blazor.Validators;

/// <summary>
/// Fluent validator for <see cref="PDRange"/> that enforces that the range bounds are within the component's min/max limits and that the track height is a valid fraction.
/// </summary>
public class PDRangeValidator : AbstractValidator<PDRange>
{
	/// <summary>
	/// Initializes a new instance of <see cref="PDRangeValidator"/> and configures validation rules.
	/// </summary>
	public PDRangeValidator()
	{
		RuleFor(x => x.TrackHeight).InclusiveBetween(0, 1);
		RuleFor(x => x.Min).LessThanOrEqualTo(x => x.Max);
		RuleFor(x => x.Range).SetValidator(new NumericRangeValidator());
		RuleFor(x => x.Range.Start).GreaterThanOrEqualTo(x => x.Min);
		RuleFor(x => x.Range.End).LessThanOrEqualTo(x => x.Max);
	}
}
