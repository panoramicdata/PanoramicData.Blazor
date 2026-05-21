using FluentValidation;

namespace PanoramicData.Blazor;

/// <summary>
/// Base class for PanoramicData Blazor components with common rendering, enablement, and validation behavior.
/// </summary>
public class PDComponentBase : ComponentBase, IEnablable
{
	/// <summary>
	/// Shared sequence used when generating default component identifiers.
	/// </summary>
	protected static int Sequence { get; set; }

	/// <summary>
	/// Gets or sets CSS classes for the component.
	/// </summary>
	[Parameter]
	public string? CssClass { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier for this component instance.
	/// </summary>
	[Parameter]
	public virtual string Id { get; set; } = $"pd-component-{++Sequence}";

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

	/// <summary>
	/// Gets the current validation errors keyed by property name.
	/// </summary>
	protected Dictionary<string, string> ValidationErrors { get; } = [];

	/// <summary>
	/// Gets a value indicating whether this component currently has no validation errors.
	/// </summary>
	protected bool IsValid => ValidationErrors.Count == 0;

	/// <summary>
	/// Merges data annotation validation results into the validation error dictionary.
	/// </summary>
	/// <param name="results">Validation results to apply.</param>
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

	/// <summary>
	/// Merges FluentValidation results into the validation error dictionary.
	/// </summary>
	/// <param name="result">Validation result to apply.</param>
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

	/// <summary>
	/// Performs data annotation validation for this component instance.
	/// </summary>
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

	/// <summary>
	/// Performs FluentValidation validation using the supplied validator and object.
	/// </summary>
	/// <typeparam name="T">Validated model type.</typeparam>
	/// <param name="validator">Validator instance.</param>
	/// <param name="obj">Model instance to validate.</param>
	protected virtual void FluentValidate<T>(IValidator<T> validator, T obj)
	{
		var result = validator.Validate(obj);
		if (!result.IsValid)
		{
			SetValidationErrors(result);
		}
	}

	#endregion

	/// <summary>
	/// Revalidates component state whenever parameters are updated.
	/// </summary>
	protected override void OnParametersSet()
	{
		Validate();
	}

	/// <summary>
	/// Enables the component and requests a re-render.
	/// </summary>
	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	/// <summary>
	/// Disables the component and requests a re-render.
	/// </summary>
	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	/// <summary>
	/// Sets component enabled state and requests a re-render.
	/// </summary>
	/// <param name="isEnabled">True to enable; false to disable.</param>
	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}
}
