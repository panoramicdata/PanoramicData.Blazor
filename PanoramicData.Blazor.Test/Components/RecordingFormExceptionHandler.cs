using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>An application's form exception handler that records what it is given, or throws when asked to.</summary>
internal sealed class RecordingFormExceptionHandler : IPDFormExceptionHandler
{
	public bool Throws { get; init; }

	public List<Exception> Handled { get; } = [];

	public Task HandleExceptionAsync(Exception exception)
	{
		if (Throws)
		{
			throw new InvalidOperationException("The application's handler failed");
		}

		Handled.Add(exception);
		return Task.CompletedTask;
	}
}
