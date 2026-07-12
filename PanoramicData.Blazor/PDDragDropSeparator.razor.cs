namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that renders a visual separator that accepts drag-and-drop operations.
/// </summary>
public partial class PDDragDropSeparator
{
	/// <summary>
	/// Gets or sets the cascading drag context.
	/// </summary>
	[CascadingParameter] public PDDragContext? DragContext { get; set; }

	/// <summary>
	/// Gets or sets the height of the separator.
	/// </summary>
	[Parameter] public int Height { get; set; } = 3;

	/// <summary>
	/// Gets or sets whether the separator is before or after the item.
	/// </summary>
	[Parameter] public bool? Before { get; set; }

	/// <summary>
	/// Gets or sets the CSS class for the separator.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// An event callback that is invoked when an item is dropped on the separator.
	/// </summary>
	[Parameter] public EventCallback<DropEventArgs> Drop { get; set; }

	/// <summary>
	/// Gets or sets whether the separator is currently in a drag-over state.
	/// </summary>
	public bool DragOver { get; set; }

	private void OnDragOver() => DragOver = true;

	private void OnDragLeave() => DragOver = false;

	private async Task OnDropAsync(MouseEventArgs args)
	{
		DragOver = false;
		var dropArgs = new DropEventArgs(this, DragContext?.Payload, args.CtrlKey, Before);
		await Drop.InvokeAsync(dropArgs).ConfigureAwait(true);
	}
}
