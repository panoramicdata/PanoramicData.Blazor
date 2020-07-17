using System;
using System.Runtime.Serialization;

namespace PanoramicData.Blazor.Exceptions
{
	/// <summary>
	/// The PDSplitterException encapsulates exceptions arising from the PDSplitter component.
	/// </summary>
	public class PDSplitterException : PDBlazorException
	{
		/// <summary>
		/// Initializes a new instance of the PDSplitterException class.
		/// </summary>
		public PDSplitterException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the PDSplitterException class with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public PDSplitterException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the PDSplitterException class with a specified error
		/// message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference
		//  if no inner exception is specified.</param>
		public PDSplitterException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the PDSplitterException class with serialized data.
		/// </summary>
		/// <param name="info">The System.Runtime.Serialization.SerializationInfo that holds the serialized object
		/// data about the exception being thrown.</param>
		/// <param name="context"> The System.Runtime.Serialization.StreamingContext that contains contextual
		/// information about the source or destination.</param>
		protected PDSplitterException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
