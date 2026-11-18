namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Handles a component's events as <see cref="ComponentBase"/> does, rendering after each one, but passes a failure to
/// the component's <c>ExceptionHandler</c>, when the application has given one, rather than letting it end the Blazor
/// circuit. Without one, a failure is thrown exactly as before.
/// </summary>
internal static class ComponentExceptionGuard
{
	/// <summary>
	/// Runs an event handler, rendering straight after it and again once an asynchronous one finishes.
	/// </summary>
	/// <param name="callback">The event handler.</param>
	/// <param name="arg">The event's argument.</param>
	/// <param name="exceptionHandler">Where a failure is passed; without a delegate a failure is thrown.</param>
	/// <param name="stateHasChanged">The component's <see cref="ComponentBase.StateHasChanged"/>.</param>
	public static async Task HandleEventAsync(
		EventCallbackWorkItem callback,
		object? arg,
		EventCallback<Exception> exceptionHandler,
		Action stateHasChanged)
	{
		ArgumentNullException.ThrowIfNull(stateHasChanged);

		Task task;
		try
		{
			task = callback.InvokeAsync(arg);
		}
		catch (Exception ex) when (exceptionHandler.HasDelegate)
		{
			await ReportAsync(exceptionHandler, ex).ConfigureAwait(true);
			stateHasChanged();
			return;
		}

		var isPending = task.Status is not (TaskStatus.RanToCompletion or TaskStatus.Canceled);
		stateHasChanged();
		if (!isPending)
		{
			return;
		}

		try
		{
			await task.ConfigureAwait(true);
		}
		catch when (task.IsCanceled)
		{
			return;
		}
		catch (Exception ex) when (exceptionHandler.HasDelegate)
		{
			await ReportAsync(exceptionHandler, ex).ConfigureAwait(true);
		}

		stateHasChanged();
	}

	/// <summary>
	/// Passes a failure to the handler. A failure of the handler itself is dropped: rethrowing it would end the circuit.
	/// </summary>
	/// <param name="exceptionHandler">Where the failure is passed.</param>
	/// <param name="exception">The failure.</param>
	public static async Task ReportAsync(EventCallback<Exception> exceptionHandler, Exception exception)
	{
		try
		{
			await exceptionHandler.InvokeAsync(exception).ConfigureAwait(true);
		}
		catch
		{
			// Nothing safer is left to do with it
		}
	}
}
