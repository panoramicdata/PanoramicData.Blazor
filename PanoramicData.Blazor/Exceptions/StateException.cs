namespace PanoramicData.Blazor.Exceptions;

public class StateException : Exception
{
	public StateException(string? message)
		: base(message)
	{
	}

	public StateException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
