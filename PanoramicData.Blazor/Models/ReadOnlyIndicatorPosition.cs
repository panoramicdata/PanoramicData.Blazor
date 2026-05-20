namespace PanoramicData.Blazor.Models;

/// <summary>
/// Determines where the read-only indicator appears relative to the file/folder name.
/// </summary>
public enum ReadOnlyIndicatorPosition
{
    /// <summary>
    /// Indicator appears after the name (e.g., "filename.txt (ro)").
    /// Maintains backward compatibility with text postfix behavior.
    /// </summary>
    After = 0,

    /// <summary>
    /// Indicator appears before the name (e.g., "🔒 filename.txt").
    /// Provides better visual alignment when multiple items have indicators.
    /// </summary>
    Before = 1
}
