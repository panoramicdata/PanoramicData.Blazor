namespace PanoramicData.Blazor.Attributes;

/// <summary>
/// Validation attribute that requires the annotated property to be a non-null, non-empty <see cref="Guid"/>.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public class RequiredGuidAttribute : ValidationAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RequiredGuidAttribute"/> class with a default error message template.
	/// </summary>
	public RequiredGuidAttribute() => ErrorMessage = "{0} is required.";

	/// <inheritdoc />
	public override bool IsValid(object? value)
		=> value != null && value is Guid && !Guid.Empty.Equals(value);
}
