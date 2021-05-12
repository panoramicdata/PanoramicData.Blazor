﻿using System.Collections.Generic;
using System.ComponentModel;

namespace PanoramicData.Blazor
{
	/// <summary>
	/// The MenuItemsEventArgs class provides information about an event related to a set of MenuItem instances.
	/// </summary>
	public class MenuItemsEventArgs : CancelEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the MenuItemsEventArgs class.
		/// </summary>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="items">The MenuItems the event relates to.</param>
		public MenuItemsEventArgs(object sender, List<MenuItem> items)
		{
			MenuItems = items;
			Sender = sender;
		}

		/// <summary>
		/// Gets the MenuItem the event relates to.
		/// </summary>
		public List<MenuItem> MenuItems { get; }

		/// <summary>
		/// Gets the object that raised the event.
		/// </summary>
		public object Sender { get; }

		/// <summary>
		/// Gets or sets an application specific context.
		/// </summary>
		public object? Context { get; set; }
	}
}
