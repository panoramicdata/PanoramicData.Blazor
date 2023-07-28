using FluentValidation;

namespace PanoramicData.Blazor;

public class PDComponentBase : ComponentBase
{
	/// <summary>
	/// Gets or sets CSS classes for the component.
	/// </summary>
	[Parameter]
	public string? CssClass { get; set; }

	/// <summary>
	/// Gets or sets whether the component is enabled.
	/// </summary>
	[Parameter]
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the component size.
	/// </summary>
	[Parameter]
	public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Gets or sets the tooltip for the component.
	/// </summary>
	[Parameter]
	public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the component is visible.
	/// </summary>
	[Parameter]
	public bool IsVisible { get; set; } = true;

	#region Validation

	protected Dictionary<string, string> ValidationErrors { get; } = new();

	protected bool IsValid => ValidationErrors.Count == 0;

	protected void SetValidationErrors(IEnumerable<ValidationResult> results)
	{
		foreach (var result in results)
		{
			var propertyNames = string.Join(", ", result.MemberNames.ToArray());
			if (!ValidationErrors.ContainsKey(propertyNames))
			{
				ValidationErrors.Add(propertyNames, result.ErrorMessage ?? string.Empty);
			}
		}
	}

	protected void SetValidationErrors(FluentValidation.Results.ValidationResult result)
	{
		foreach (var failure in result.Errors)
		{
			if (!ValidationErrors.ContainsKey(failure.PropertyName))
			{
				ValidationErrors.Add(failure.PropertyName, failure.ErrorMessage);
			}
		}
	}

	protected virtual void Validate()
	{
		ValidationErrors.Clear();

		// default using DataAnnotations validation
		var validationContext = new ValidationContext(this, null, null);
		var validationResults = new List<ValidationResult>();
		if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
		{
			SetValidationErrors(validationResults);
		}
	}

	protected virtual void FluentValidate<T>(IValidator<T> validator, T obj)
	{
		var result = validator.Validate(obj);
		if (!result.IsValid)
		{
			SetValidationErrors(result);
		}
	}

	#endregion

	protected override void OnParametersSet()
	{
		Validate();
	}
}
