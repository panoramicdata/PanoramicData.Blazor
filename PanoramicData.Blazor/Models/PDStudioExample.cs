namespace PanoramicData.Blazor.Models;

/// <summary>
/// A piece of example code a host offers on the <see cref="PDStudio"/> Examples menu.
/// </summary>
/// <param name="Name">The menu text.</param>
/// <param name="Code">The code loaded into the editor.</param>
/// <param name="Language">The language the example is for; null offers it whatever the studio's language.</param>
public record PDStudioExample(string Name, string Code, string? Language = null)
{
	/// <summary>
	/// Determines whether the example is offered while the studio uses the given language.
	/// </summary>
	public bool IsFor(string language)
		=> Language is null || string.Equals(Language, language, StringComparison.OrdinalIgnoreCase);
}
