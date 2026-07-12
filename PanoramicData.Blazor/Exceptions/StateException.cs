namespace PanoramicData.Blazor.Exceptions;

/// <summary>
/// Represents an error caused by an unexpected or invalid component state.
/// </summary>
public class StateException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="StateException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public StateException(string? message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StateException"/> class with a specified error
	/// message and a reference to the inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
	public StateException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
