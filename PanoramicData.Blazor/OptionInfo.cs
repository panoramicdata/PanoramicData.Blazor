﻿namespace PanoramicData.Blazor
{
	/// <summary>
	/// The OptionInfo class provides details on a single Option element.
	/// </summary>
	public class OptionInfo
	{
		/// <summary>
		/// Gets or sets the text displayed for the option.
		/// </summary>
		public string Text { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the value for the option.
		/// </summary>
		public string Value { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets whether the option is displayed.
		/// </summary>
		public bool IsSelected { get; set; }
	}
}