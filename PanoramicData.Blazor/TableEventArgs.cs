using System.Collections.Generic;

namespace PanoramicData.Blazor
{
	/// <summary>
	/// The TableEventArgs class holds details on a node event.
	/// </summary>
	public class TableEventArgs<TItem> where TItem : class
	{
		/// <summary>
		/// Initializes a new instance of the TableEventArgs class.
		/// </summary>
		/// <param name="item">The item the event relates to.</param>
		public TableEventArgs(TItem item)
		{
			Item = item;
		}

		/// <summary>
		/// Gets the item the event relates to.
		/// </summary>
		public TItem Item { get; }
	}

	/// <summary>
	/// The TableCancelEventArgs class allows an event to be canceled.
	/// </summary>
	public class TableCancelEventArgs<TItem> : TableEventArgs<TItem> where TItem : class
	{
		/// <summary>
		/// Initializes a new instance of the TableCancelEventArgs class.
		/// </summary>
		/// <param name="node">The node the event relates to.</param>
		public TableCancelEventArgs(TItem item)
			: base(item)
		{
		}

		/// <summary>
		/// Gets or sets whether the operation attempted should be canceled.
		/// </summary>
		public bool Cancel { get; set; }
	}

	/// <summary>
	/// The TableBeforeEditEventArgs class allows a pending edit to be canceled.
	/// </summary>
	public class TableBeforeEditEventArgs<TItem> : TableCancelEventArgs<TItem> where TItem : class
	{
		/// <summary>
		/// Initializes a new instance of the TableBeforeEditEventArgs class.
		/// </summary>
		/// <param item="">The item the event relates to.</param>
		public TableBeforeEditEventArgs(TItem item)
			: base(item)
		{
		}

		/// <summary>
		/// Gets or sets the start of the selected text to be edited.
		/// </summary>
		public int SelectionStart { get; set; }

		/// <summary>
		/// Gets or sets the end of the selected text to be edited.
		/// </summary>
		public int SelectionEnd { get; set; }
	}

	/// <summary>
	/// The TableAfterEditEventArgs class provides details of a completed edit and allows for cancellation.
	/// </summary>
	public class TableAfterEditEventArgs<TItem> : TableCancelEventArgs<TItem> where TItem : class
	{
		/// <summary>
		/// Initializes a new instance of the TableBeforeEditEventArgs class.
		/// </summary>
		/// <param name="item">The original item values the event relates to.</param>
		public TableAfterEditEventArgs(TItem item)
			: base(item)
		{
		}

		/// <summary>
		/// Gets or sets the new values.
		/// </summary>
		public Dictionary<string, object?> NewValues { get; set; } = new Dictionary<string, object?>();
	}

	/// <summary>
	/// The TableSelectionEventArgs class holds data for an action that is performed on a selection of items.
	/// </summary>
	public class TableSelectionEventArgs<TItem> where TItem : class
	{
		/// <summary>
		/// Initializes a new instance of the TableSelectionEventArgs class.
		/// </summary>
		/// <param name="items">The items the event relates to.</param>
		public TableSelectionEventArgs(TItem[] items)
		{
			Items = items;
		}

		/// <summary>
		/// Gets the items the event relates to.
		/// </summary>
		public TItem[] Items { get; }
	}
}
