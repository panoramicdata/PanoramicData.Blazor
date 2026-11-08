namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The DropEventArgs class provides information for PDDragContext events.
/// </summary>
public class DropEventArgs
{
	/// <summary>
	/// Initializes a new instance of the DropEventArgs class.
	/// </summary>
	/// <param name="target">Details on where the drop occurred.</param>
	/// <param name="payload">Payload of the drop.</param>
	/// <param name="ctrl">Was the control key pressed during the drop?</param>
	public DropEventArgs(object? target, object? payload, bool ctrl)
	{
		Target = target;
		Payload = payload;
		Ctrl = ctrl;
		Before = null;
		Position = DropPosition.Into;
	}

	/// <summary>
	/// Initializes a new instance of the DropEventArgs class with ordering information.
	/// </summary>
	/// <param name="target">Details on where the drop occurred.</param>
	/// <param name="payload">Payload of the drop.</param>
	/// <param name="ctrl">Was the control key pressed during the drop?</param>
	/// <param name="before">Whether the item was dropped before (<c>true</c>), after (<c>false</c>), or in an unordered position (<c>null</c>) relative to the target.</param>
	public DropEventArgs(object? target, object? payload, bool ctrl, bool? before)
	{
		Target = target;
		Payload = payload;
		Ctrl = ctrl;
		Before = before;
		Position = before switch
		{
			true => DropPosition.Before,
			false => DropPosition.After,
			null => DropPosition.Into
		};
	}

	/// <summary>
	/// Initializes a new instance of the DropEventArgs class with the drop position.
	/// </summary>
	/// <param name="target">Details on where the drop occurred.</param>
	/// <param name="payload">Payload of the drop.</param>
	/// <param name="ctrl">Was the control key pressed during the drop?</param>
	/// <param name="position">Whether the item was dropped before, after or into the target; <see cref="Before"/> is set to match.</param>
	public DropEventArgs(object? target, object? payload, bool ctrl, DropPosition position)
	{
		Target = target;
		Payload = payload;
		Ctrl = ctrl;
		Position = position;
		Before = position switch
		{
			DropPosition.Before => true,
			DropPosition.After => false,
			_ => null
		};
	}

	/// <summary>
	/// Gets or sets details on where the drop occurred.
	/// </summary>
	public object? Target { get; set; }

	/// <summary>
	/// Gets the payload associated with the drop.
	/// </summary>
	public object? Payload { get; }

	/// <summary>
	/// Gets whether the control key was held down on drop.
	/// </summary>
	public bool Ctrl { get; }

	/// <summary>
	/// Gets whether the drop operation is before (true), after (false) or not relevant (null) to the Node specified.
	/// </summary>
	public bool? Before { get; }

	/// <summary>
	/// Gets whether the drop was before, after or into the target. It agrees with <see cref="Before"/>:
	/// <see cref="DropPosition.Into"/> is a <c>null</c> <see cref="Before"/>.
	/// </summary>
	public DropPosition Position { get; }
}
