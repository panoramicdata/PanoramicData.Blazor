namespace PanoramicData.Blazor.Models;

/// <summary>
/// One unit of model reasoning, shown collapsed against an in-progress message (issue #98).
/// </summary>
/// <param name="Title">
/// A short summary, shown on the collapsed row. Written by the service rather than derived here:
/// only the service knows what its own reasoning was about, and a title guessed from the first few
/// words of the text is usually misleading.
/// </param>
/// <param name="Text">
/// The reasoning itself, shown when the reader expands the row.
/// </param>
/// <remarks>
/// Deliberately collapsed by default. Model reasoning is long, repetitive and sometimes wrong;
/// rendering it inline makes a chat panel unreadable and, worse, invites a reader to treat a thought
/// the model later discarded as though it were the conclusion. Collapsed-with-a-title keeps the
/// honest signal - something is happening, and this is roughly what - while leaving the detail one
/// click away for anyone who wants to audit it.
/// </remarks>
public sealed record ChatThought(string Title, string Text);
