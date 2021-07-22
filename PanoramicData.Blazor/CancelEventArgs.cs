﻿namespace PanoramicData.Blazor
{
	/// <summary>
	/// The CancelEventArgs class models event arguments for an action that can be canceled.
	/// </summary>
	public class CancelEventArgs
	{
		/// <summary>
		/// Gets or sets whether the navigation should be canceled.
		/// </summary>
		public bool Cancel { get; set; }
	}
}
