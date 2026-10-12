namespace PanoramicData.Blazor.Models;

/// <summary>
/// An action offered as the last entry of a <see cref="PanoramicData.Blazor.PDChat"/> picker, such as "New Assistant".
/// Choosing it invokes <paramref name="OnChosen"/> and never changes the picker's selection.
/// </summary>
/// <param name="Label">The text shown in the picker.</param>
/// <param name="OnChosen">Invoked when the user chooses the action, for example to navigate elsewhere.</param>
/// <param name="Icon">Optional text shown before the label, such as "[+]"; a native option cannot show an image.</param>
/// <param name="Description">An optional description, shown as the option's tooltip.</param>
public sealed record PDChatPickerAction(string Label, Func<Task> OnChosen, string? Icon = null, string? Description = null)
{
	/// <summary>Gets the text the picker shows: the icon, if any, then the label.</summary>
	public string DisplayText => string.IsNullOrWhiteSpace(Icon) ? Label : $"{Icon} {Label}";
}
