using System;

namespace PanoramicData.Blazor.Arguments
{
	/// <summary>
	/// The KeyedEventArgs class wraps another EventArgs class and adds a unique key.
	/// </summary>
	/// <typeparam name="T">DataType of the wrapped EventArgs.</typeparam>
	public class KeyedEventArgs<T>
	{
		/// <summary>
		/// Initializes a new instance of the KeyedEventArgs class.
		/// </summary>
		/// <param name="key">Unique key identifier.</param>
		/// <param name="args">Wrapped EventArgs.</param>
		public KeyedEventArgs(string key, T args)
		{
			Key = key;
			Args = args;
		}

		/// <summary>
		/// Initializes a new instance of the KeyedEventArgs class.
		/// </summary>
		/// <param name="key">Unique key identifier.</param>
		public KeyedEventArgs(string key)
		{
			Key = key;
			Args = Activator.CreateInstance<T>();
		}

		/// <summary>
		/// Gets the unique key.
		/// </summary>
		public string Key { get; }

		/// <summary>
		/// Gets the wrapped event arguments.
		/// </summary>
		public T Args { get; }
	}
}
