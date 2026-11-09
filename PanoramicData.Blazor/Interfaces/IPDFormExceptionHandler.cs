namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Handles an exception in any <see cref="PDForm{TItem}"/> that sets neither its ExceptionHandler nor its Error
/// parameter, so that an application can report every form's failures in one place. Nothing is registered by
/// default: register an implementation in the service collection to use one.
/// </summary>
public interface IPDFormExceptionHandler
{
	/// <summary>
	/// Handles an exception raised in a form.
	/// </summary>
	/// <param name="exception">The exception.</param>
	Task HandleExceptionAsync(Exception exception);
}
