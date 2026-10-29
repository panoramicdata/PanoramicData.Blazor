namespace PanoramicData.Blazor.Models;

/// <summary>
/// A column of a <see cref="PDStudioResultSet"/>.
/// </summary>
/// <param name="Name">The column's name, shown as its heading.</param>
/// <param name="TypeName">The column's data type as the source names it, such as "text" or "timestamp".</param>
public record PDStudioResultColumn(string Name, string TypeName);
