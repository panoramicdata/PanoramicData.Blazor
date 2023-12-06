namespace PanoramicData.Blazor.Exceptions;

/// <summary>
/// The PDContextMenuException encapsulates exceptions arising from the PDSplitter component.
/// </summary>
public class PDContextMenuException : PDBlazorException
{
	/// <summary>
	/// Initializes a new instance of the PDContextMenuException class.
	/// </summary>
	public PDContextMenuException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the PDContextMenuException class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public PDContextMenuException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the PDContextMenuException class with a specified error
	/// message and a reference to the inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference
	//  if no inner exception is specified.</param>
	public PDContextMenuException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
