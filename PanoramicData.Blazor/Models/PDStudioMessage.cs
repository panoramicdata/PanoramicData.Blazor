namespace PanoramicData.Blazor.Models;

/// <summary>
/// A message shown on the <see cref="PDStudio"/> Messages tab.
/// </summary>
/// <param name="Timestamp">When the message was recorded, in UTC.</param>
/// <param name="Level">The message's severity.</param>
/// <param name="Text">The message, shown as plain text.</param>
public record PDStudioMessage(DateTime Timestamp, LogLevel Level, string Text);
